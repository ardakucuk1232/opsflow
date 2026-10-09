import type { AuthResponse, AuthUser } from "@/types/api";

export const TEST_USER: AuthUser = {
  id: "0b0f6f0e-6f0a-4a57-9d0e-3f0f8f1f2a01",
  companyId: "5d0c1c52-7c64-4c4f-8a55-0a3a3c1d9b02",
  companyName: "ABC Yazılım A.Ş.",
  email: "arda@abc.com",
  isEmailVerified: true,
  firstName: "Arda",
  lastName: "Küçük",
  roles: ["Admin"],
  permissions: [
    "attachment.upload",
    "audit_log.view",
    "comment.create",
    "company.manage",
    "project.create",
    "project.manage",
    "project.view_all",
    "report.view",
    "role.manage",
    "task.assign",
    "task.create",
    "task.delete",
    "task.update",
    "user.invite",
    "user.manage",
    "user.view",
  ],
};

export function createAuthResponse(overrides: Partial<AuthResponse> = {}): AuthResponse {
  return {
    accessToken: "access-1",
    accessTokenExpiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
    refreshToken: "refresh-1",
    refreshTokenExpiresAt: new Date(Date.now() + 7 * 24 * 60 * 60_000).toISOString(),
    user: TEST_USER,
    ...overrides,
  };
}

export function jsonResponse(body: unknown, init: ResponseInit = {}): Response {
  return new Response(JSON.stringify(body), {
    status: 200,
    ...init,
    headers: { "Content-Type": "application/json", ...init.headers },
  });
}

export function problemResponse(
  status: number,
  code: string,
  headers: Record<string, string> = {},
): Response {
  return new Response(JSON.stringify({ status, code, title: code }), {
    status,
    headers: { "Content-Type": "application/problem+json", ...headers },
  });
}

export function requestBody(call: unknown[]): unknown {
  const init = call[1] as RequestInit;

  return JSON.parse(init.body as string);
}

export function requestHeader(call: unknown[], name: string): string | null {
  const init = call[1] as RequestInit;

  return new Headers(init.headers).get(name);
}
