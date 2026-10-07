"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useSyncExternalStore,
  type ReactNode,
} from "react";
import { authApi, type LoginInput, type RegisterInput } from "@/lib/api/auth";
import { session, type SessionState } from "@/lib/auth/session";

type AuthContextValue = SessionState & {
  login: (input: LoginInput) => Promise<void>;
  register: (input: RegisterInput) => Promise<void>;
  logout: () => Promise<void>;
  retry: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const state = useSyncExternalStore(session.subscribe, session.getState, session.getServerState);

  useEffect(() => {
    void session.bootstrap();
  }, []);

  const login = useCallback(async (input: LoginInput) => {
    session.signIn(await authApi.login(input));
  }, []);

  const register = useCallback(async (input: RegisterInput) => {
    session.signIn(await authApi.register(input));
  }, []);

  const logout = useCallback(() => session.signOut(), []);

  const retry = useCallback(() => session.bootstrap(), []);

  const value = useMemo<AuthContextValue>(
    () => ({ ...state, login, register, logout, retry }),
    [state, login, register, logout, retry],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider.");
  }

  return context;
}
