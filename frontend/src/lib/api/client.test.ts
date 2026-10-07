import { beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "@/lib/api/client";
import { ApiError } from "@/lib/api/errors";
import { REFRESH_TOKEN_KEY, session } from "@/lib/auth/session";
import {
  createAuthResponse,
  jsonResponse,
  problemResponse,
  requestHeader,
  TEST_USER,
} from "@/test/fixtures";

const fetchMock = vi.fn<typeof fetch>();

beforeEach(() => {
  session.resetForTests();
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
});

describe("api", () => {
  it("sends the access token as a bearer header", async () => {
    session.signIn(createAuthResponse({ accessToken: "access-1" }));
    fetchMock.mockResolvedValueOnce(jsonResponse(TEST_USER));

    await expect(api("/api/auth/me")).resolves.toEqual(TEST_USER);
    expect(requestHeader(fetchMock.mock.calls[0], "Authorization")).toBe("Bearer access-1");
  });

  it("refreshes the session once and retries after a 401", async () => {
    session.signIn(createAuthResponse({ accessToken: "access-1" }));
    fetchMock
      .mockResolvedValueOnce(problemResponse(401, "unauthorized"))
      .mockResolvedValueOnce(
        jsonResponse(createAuthResponse({ accessToken: "access-2", refreshToken: "refresh-2" })),
      )
      .mockResolvedValueOnce(jsonResponse(TEST_USER));

    await expect(api("/api/auth/me")).resolves.toEqual(TEST_USER);

    expect(fetchMock).toHaveBeenCalledTimes(3);
    expect(String(fetchMock.mock.calls[1][0])).toContain("/api/auth/refresh");
    expect(requestHeader(fetchMock.mock.calls[2], "Authorization")).toBe("Bearer access-2");
  });

  it("does not retry more than once", async () => {
    session.signIn(createAuthResponse({ accessToken: "access-1" }));
    fetchMock
      .mockResolvedValueOnce(problemResponse(401, "unauthorized"))
      .mockResolvedValueOnce(jsonResponse(createAuthResponse({ accessToken: "access-2" })))
      .mockResolvedValueOnce(problemResponse(401, "unauthorized"));

    await expect(api("/api/auth/me")).rejects.toMatchObject({ status: 401 });
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });

  it("ends the session when the refresh token is rejected", async () => {
    session.signIn(createAuthResponse({ accessToken: "access-1" }));
    fetchMock
      .mockResolvedValueOnce(problemResponse(401, "unauthorized"))
      .mockResolvedValueOnce(problemResponse(401, "auth.invalid_refresh_token"));

    await expect(api("/api/auth/me")).rejects.toBeInstanceOf(ApiError);

    expect(session.getState().status).toBe("unauthenticated");
    expect(window.localStorage.getItem(REFRESH_TOKEN_KEY)).toBeNull();
  });

  it("does not refresh for errors other than 401", async () => {
    session.signIn(createAuthResponse());
    fetchMock.mockResolvedValueOnce(problemResponse(403, "forbidden"));

    await expect(api("/api/projects")).rejects.toMatchObject({ status: 403, code: "forbidden" });
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("fails without calling the API when there is no session", async () => {
    await expect(api("/api/auth/me")).rejects.toMatchObject({ status: 401 });
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
