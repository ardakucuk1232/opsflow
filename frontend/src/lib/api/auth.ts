import { api } from "@/lib/api/client";
import { send } from "@/lib/api/http";
import type { AuthResponse, AuthUser } from "@/types/api";

export type LoginInput = {
  email: string;
  password: string;
};

export type RegisterInput = {
  companyName: string;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
};

export type ResetPasswordInput = {
  token: string;
  newPassword: string;
};

export const authApi = {
  login(input: LoginInput): Promise<AuthResponse> {
    return send<AuthResponse>("/api/auth/login", { method: "POST", body: input });
  },

  register(input: RegisterInput): Promise<AuthResponse> {
    return send<AuthResponse>("/api/auth/register", { method: "POST", body: input });
  },

  me(): Promise<AuthUser> {
    return api<AuthUser>("/api/auth/me");
  },

  verifyEmail(token: string): Promise<void> {
    return send<void>("/api/auth/verify-email", { method: "POST", body: { token } });
  },

  resendVerification(): Promise<void> {
    return api<void>("/api/auth/resend-verification", { method: "POST" });
  },

  forgotPassword(email: string): Promise<void> {
    return send<void>("/api/auth/forgot-password", { method: "POST", body: { email } });
  },

  resetPassword(input: ResetPasswordInput): Promise<void> {
    return send<void>("/api/auth/reset-password", { method: "POST", body: input });
  },
};
