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
};
