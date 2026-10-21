import { act, render, screen } from "@testing-library/react";
import { useEffect } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider, useAuth } from "@/lib/auth/auth-provider";
import { createAuthResponse, TEST_USER } from "@/test/fixtures";

const mocks = vi.hoisted(() => {
  const listeners = new Set<() => void>();
  let state: { status: string; user: unknown } = { status: "loading", user: null };

  return {
    listeners,
    setState(next: typeof state) {
      state = next;
      listeners.forEach((listener) => listener());
    },
    session: {
      subscribe: (listener: () => void) => {
        listeners.add(listener);
        return () => listeners.delete(listener);
      },
      getState: () => state,
      getServerState: () => ({ status: "loading", user: null }),
      bootstrap: vi.fn(),
      signIn: vi.fn(),
      signOut: vi.fn(),
      reloadUser: vi.fn(),
    },
    authApi: { login: vi.fn(), register: vi.fn(), acceptInvitation: vi.fn() },
  };
});

vi.mock("@/lib/auth/session", () => ({ session: mocks.session }));
vi.mock("@/lib/api/auth", () => ({ authApi: mocks.authApi }));

let captured: ReturnType<typeof useAuth> | null = null;

function Probe() {
  const auth = useAuth();

  useEffect(() => {
    captured = auth;
  });

  return <p>durum: {auth.status}</p>;
}

beforeEach(() => {
  captured = null;
  mocks.setState({ status: "loading", user: null });
  mocks.session.bootstrap.mockReset().mockResolvedValue(undefined);
  mocks.session.signIn.mockReset();
  mocks.session.signOut.mockReset().mockResolvedValue(undefined);
  mocks.session.reloadUser.mockReset().mockResolvedValue(undefined);
  mocks.authApi.login.mockReset().mockResolvedValue(createAuthResponse());
  mocks.authApi.register.mockReset().mockResolvedValue(createAuthResponse());
  mocks.authApi.acceptInvitation.mockReset().mockResolvedValue(createAuthResponse({ accessToken: "invited" }));
});

describe("AuthProvider", () => {
  it("restores the session on mount and follows its state", () => {
    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );

    expect(mocks.session.bootstrap).toHaveBeenCalled();
    expect(screen.getByText("durum: loading")).toBeInTheDocument();

    act(() => mocks.setState({ status: "authenticated", user: TEST_USER }));

    expect(screen.getByText("durum: authenticated")).toBeInTheDocument();
  });

  it("signs in with the response of login and register", async () => {
    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );

    await act(() => captured!.login({ email: "a@b.c", password: "x" }));
    await act(() => captured!.register({ companyName: "C", firstName: "A", lastName: "B", email: "a@b.c", password: "x" }));

    expect(mocks.authApi.login).toHaveBeenCalledWith({ email: "a@b.c", password: "x" });
    expect(mocks.session.signIn).toHaveBeenCalledTimes(2);
  });

  it("ends the current session before accepting an invitation as someone else", async () => {
    mocks.setState({ status: "authenticated", user: TEST_USER });
    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );

    await act(() => captured!.acceptInvitation({ token: "tok", password: "x" }));

    expect(mocks.session.signOut).toHaveBeenCalled();
    expect(mocks.session.signIn).toHaveBeenCalledWith(expect.objectContaining({ accessToken: "invited" }));
  });

  it("passes logout, retry and reload through to the session", async () => {
    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );

    await act(() => captured!.logout());
    await act(() => captured!.retry());
    await act(() => captured!.reloadUser());

    expect(mocks.session.signOut).toHaveBeenCalled();
    expect(mocks.session.bootstrap).toHaveBeenCalledTimes(2);
    expect(mocks.session.reloadUser).toHaveBeenCalled();
  });

  it("must be used inside the provider", () => {
    vi.spyOn(console, "error").mockImplementation(() => undefined);

    expect(() => render(<Probe />)).toThrow("useAuth must be used within an AuthProvider.");
  });
});
