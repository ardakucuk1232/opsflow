import { ApiError } from "@/lib/api/errors";
import { send, type SendOptions } from "@/lib/api/http";
import { session } from "@/lib/auth/session";

export type ApiOptions = Omit<SendOptions, "token">;

function unauthorized(): ApiError {
  return new ApiError({
    status: 401,
    code: "unauthorized",
    message: "Authentication is required.",
  });
}

export async function api<T>(path: string, options: ApiOptions = {}): Promise<T> {
  const token = await session.getAccessToken();

  if (!token) {
    throw unauthorized();
  }

  try {
    return await send<T>(path, { ...options, token });
  } catch (error) {
    if (!(error instanceof ApiError) || error.status !== 401) {
      throw error;
    }

    const refreshed = await session.refresh(token);
    const retryToken = refreshed ? await session.getAccessToken() : null;

    if (!retryToken) {
      throw error;
    }

    return send<T>(path, { ...options, token: retryToken });
  }
}
