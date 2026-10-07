import { ApiError } from "@/lib/api/errors";
import { send } from "@/lib/api/http";
import type { AuthResponse, AuthUser } from "@/types/api";

export type SessionState =
  | { status: "loading"; user: null }
  | { status: "authenticated"; user: AuthUser }
  | { status: "unauthenticated"; user: null }
  | { status: "unavailable"; user: null };

type AccessToken = { value: string; expiresAt: number };

export const REFRESH_TOKEN_KEY = "opsflow.refresh-token";

const REFRESH_LOCK_NAME = "opsflow.session.refresh";
const EXPIRY_SKEW_MS = 30_000;

const LOADING: SessionState = { status: "loading", user: null };
const UNAUTHENTICATED: SessionState = { status: "unauthenticated", user: null };
const UNAVAILABLE: SessionState = { status: "unavailable", user: null };

let state: SessionState = LOADING;
let accessToken: AccessToken | null = null;
let refreshInFlight: Promise<boolean> | null = null;
let bootstrapInFlight: Promise<void> | null = null;
let storageListenerAttached = false;

const listeners = new Set<() => void>();

function setState(next: SessionState): void {
  state = next;
  listeners.forEach((listener) => listener());
}

function readRefreshToken(): string | null {
  try {
    return window.localStorage.getItem(REFRESH_TOKEN_KEY);
  } catch {
    return null;
  }
}

function writeRefreshToken(token: string | null): void {
  try {
    if (token === null) {
      window.localStorage.removeItem(REFRESH_TOKEN_KEY);
    } else {
      window.localStorage.setItem(REFRESH_TOKEN_KEY, token);
    }
  } catch {
    return;
  }
}

function applyAuthResponse(response: AuthResponse): void {
  accessToken = {
    value: response.accessToken,
    expiresAt: Date.parse(response.accessTokenExpiresAt),
  };

  writeRefreshToken(response.refreshToken);
  setState({ status: "authenticated", user: response.user });
}

function clearSession(): void {
  accessToken = null;
  writeRefreshToken(null);
  setState(UNAUTHENTICATED);
}

function getUsableAccessToken(): string | null {
  if (accessToken === null || accessToken.expiresAt - EXPIRY_SKEW_MS <= Date.now()) {
    return null;
  }

  return accessToken.value;
}

async function withRefreshLock<T>(callback: () => Promise<T>): Promise<T> {
  if (typeof navigator !== "undefined" && navigator.locks) {
    return navigator.locks.request(REFRESH_LOCK_NAME, callback);
  }

  return callback();
}

async function performRefresh(): Promise<boolean> {
  return withRefreshLock(async () => {
    const refreshToken = readRefreshToken();

    if (!refreshToken) {
      clearSession();
      return false;
    }

    try {
      const response = await send<AuthResponse>("/api/auth/refresh", {
        method: "POST",
        body: { refreshToken },
      });

      applyAuthResponse(response);
      return true;
    } catch (error) {
      if (error instanceof ApiError && !error.isTransient) {
        clearSession();
        return false;
      }

      throw error;
    }
  });
}

function attachStorageListener(): void {
  if (storageListenerAttached || typeof window === "undefined") {
    return;
  }

  storageListenerAttached = true;

  window.addEventListener("storage", (event) => {
    if (event.key !== REFRESH_TOKEN_KEY) {
      return;
    }

    if (event.newValue === null) {
      accessToken = null;
      setState(UNAUTHENTICATED);
      return;
    }

    if (state.status !== "authenticated") {
      void session.refresh().catch(() => undefined);
    }
  });
}

export const session = {
  subscribe(listener: () => void): () => void {
    listeners.add(listener);

    return () => {
      listeners.delete(listener);
    };
  },

  getState(): SessionState {
    return state;
  },

  getServerState(): SessionState {
    return LOADING;
  },

  bootstrap(): Promise<void> {
    attachStorageListener();

    if (state.status === "authenticated") {
      return Promise.resolve();
    }

    bootstrapInFlight ??= (async () => {
      try {
        await session.refresh();
      } catch {
        setState(UNAVAILABLE);
      } finally {
        bootstrapInFlight = null;
      }
    })();

    return bootstrapInFlight;
  },

  refresh(staleToken?: string): Promise<boolean> {
    const current = getUsableAccessToken();

    if (staleToken && current && current !== staleToken) {
      return Promise.resolve(true);
    }

    refreshInFlight ??= performRefresh().finally(() => {
      refreshInFlight = null;
    });

    return refreshInFlight;
  },

  async getAccessToken(): Promise<string | null> {
    const current = getUsableAccessToken();

    if (current) {
      return current;
    }

    const refreshed = await session.refresh();

    return refreshed ? getUsableAccessToken() : null;
  },

  async reloadUser(): Promise<void> {
    try {
      const token = await session.getAccessToken();

      if (!token) {
        return;
      }

      const user = await send<AuthUser>("/api/auth/me", { token });

      if (state.status === "authenticated") {
        setState({ status: "authenticated", user });
      }
    } catch {
      return;
    }
  },

  signIn(response: AuthResponse): void {
    applyAuthResponse(response);
  },

  async signOut(): Promise<void> {
    const refreshToken = readRefreshToken();

    clearSession();

    if (!refreshToken) {
      return;
    }

    try {
      await send<void>("/api/auth/logout", { method: "POST", body: { refreshToken } });
    } catch {
      return;
    }
  },

  resetForTests(): void {
    state = LOADING;
    accessToken = null;
    refreshInFlight = null;
    bootstrapInFlight = null;
    listeners.clear();
  },
};
