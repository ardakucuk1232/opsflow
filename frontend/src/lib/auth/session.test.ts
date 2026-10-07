import { beforeEach, describe, expect, it, vi } from "vitest";
import { REFRESH_TOKEN_KEY, session } from "@/lib/auth/session";
import {
  createAuthResponse,
  jsonResponse,
  problemResponse,
  requestBody,
  TEST_USER,
} from "@/test/fixtures";

const fetchMock = vi.fn<typeof fetch>();

beforeEach(() => {
  session.resetForTests();
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
});

describe("session.bootstrap", () => {
  it("is unauthenticated without calling the API when no refresh token is stored", async () => {
    await session.bootstrap();

    expect(session.getState().status).toBe("unauthenticated");
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("restores the session and stores the rotated refresh token", async () => {
    window.localStorage.setItem(REFRESH_TOKEN_KEY, "refresh-0");
    fetchMock.mockResolvedValueOnce(jsonResponse(createAuthResponse({ refreshToken: "refresh-1" })));

    await session.bootstrap();

    expect(session.getState()).toEqual({ status: "authenticated", user: TEST_USER });
    expect(requestBody(fetchMock.mock.calls[0])).toEqual({ refreshToken: "refresh-0" });
    expect(window.localStorage.getItem(REFRESH_TOKEN_KEY)).toBe("refresh-1");
  });

  it("clears the stored token when the API rejects it", async () => {
    window.localStorage.setItem(REFRESH_TOKEN_KEY, "revoked");
    fetchMock.mockResolvedValueOnce(problemResponse(401, "auth.invalid_refresh_token"));

    await session.bootstrap();

    expect(session.getState().status).toBe("unauthenticated");
    expect(window.localStorage.getItem(REFRESH_TOKEN_KEY)).toBeNull();
  });

  it("keeps the stored token when the API cannot be reached", async () => {
    window.localStorage.setItem(REFRESH_TOKEN_KEY, "refresh-0");
    fetchMock.mockRejectedValueOnce(new TypeError("Failed to fetch"));

    await session.bootstrap();

    expect(session.getState().status).toBe("unavailable");
    expect(window.localStorage.getItem(REFRESH_TOKEN_KEY)).toBe("refresh-0");
  });

  it("keeps the stored token when the API is rate limited", async () => {
    window.localStorage.setItem(REFRESH_TOKEN_KEY, "refresh-0");
    fetchMock.mockResolvedValueOnce(problemResponse(429, "too_many_requests"));

    await session.bootstrap();

    expect(session.getState().status).toBe("unavailable");
    expect(window.localStorage.getItem(REFRESH_TOKEN_KEY)).toBe("refresh-0");
  });
});

describe("session.refresh", () => {
  it("sends a single request when called concurrently", async () => {
    window.localStorage.setItem(REFRESH_TOKEN_KEY, "refresh-0");
    fetchMock.mockResolvedValueOnce(jsonResponse(createAuthResponse()));

    const results = await Promise.all([session.refresh(), session.refresh(), session.refresh()]);

    expect(results).toEqual([true, true, true]);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("skips the request when another caller already replaced the stale token", async () => {
    session.signIn(createAuthResponse({ accessToken: "access-2" }));

    await expect(session.refresh("access-1")).resolves.toBe(true);
    expect(fetchMock).not.toHaveBeenCalled();
  });
});

describe("session.getAccessToken", () => {
  it("returns the in-memory token while it is still valid", async () => {
    session.signIn(createAuthResponse({ accessToken: "access-1" }));

    await expect(session.getAccessToken()).resolves.toBe("access-1");
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("refreshes first when the token is about to expire", async () => {
    session.signIn(
      createAuthResponse({
        accessToken: "expiring",
        accessTokenExpiresAt: new Date(Date.now() + 5_000).toISOString(),
      }),
    );
    fetchMock.mockResolvedValueOnce(
      jsonResponse(createAuthResponse({ accessToken: "access-2", refreshToken: "refresh-2" })),
    );

    await expect(session.getAccessToken()).resolves.toBe("access-2");
    expect(window.localStorage.getItem(REFRESH_TOKEN_KEY)).toBe("refresh-2");
  });
});

describe("session.signOut", () => {
  it("clears local state immediately and revokes the refresh token on the API", async () => {
    session.signIn(createAuthResponse({ refreshToken: "refresh-1" }));
    fetchMock.mockResolvedValueOnce(new Response(null, { status: 204 }));

    await session.signOut();

    expect(session.getState().status).toBe("unauthenticated");
    expect(window.localStorage.getItem(REFRESH_TOKEN_KEY)).toBeNull();
    expect(String(fetchMock.mock.calls[0][0])).toContain("/api/auth/logout");
    expect(requestBody(fetchMock.mock.calls[0])).toEqual({ refreshToken: "refresh-1" });
  });

  it("still signs out locally when the API call fails", async () => {
    session.signIn(createAuthResponse());
    fetchMock.mockRejectedValueOnce(new TypeError("Failed to fetch"));

    await expect(session.signOut()).resolves.toBeUndefined();
    expect(session.getState().status).toBe("unauthenticated");
  });
});

describe("session across tabs", () => {
  it("signs out when another tab removes the refresh token", async () => {
    await session.bootstrap();
    session.signIn(createAuthResponse());

    window.dispatchEvent(
      new StorageEvent("storage", { key: REFRESH_TOKEN_KEY, oldValue: "refresh-1", newValue: null }),
    );

    expect(session.getState().status).toBe("unauthenticated");
  });

  it("notifies subscribers when the state changes", () => {
    const listener = vi.fn();
    const unsubscribe = session.subscribe(listener);

    session.signIn(createAuthResponse());
    unsubscribe();
    session.signIn(createAuthResponse());

    expect(listener).toHaveBeenCalledTimes(1);
  });
});
