import { env } from "@/config/env";
import { ApiError } from "@/lib/api/errors";

export type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

export type SendOptions = {
  method?: HttpMethod;
  body?: unknown;
  token?: string | null;
  signal?: AbortSignal;
  responseType?: "json" | "blob";
};

export async function send<T>(path: string, options: SendOptions = {}): Promise<T> {
  const isForm = options.body instanceof FormData;
  const headers = new Headers({ Accept: options.responseType === "blob" ? "*/*" : "application/json" });

  if (options.body !== undefined && !isForm) {
    headers.set("Content-Type", "application/json");
  }

  if (options.token) {
    headers.set("Authorization", `Bearer ${options.token}`);
  }

  let response: Response;

  try {
    response = await fetch(`${env.apiUrl}${path}`, {
      method: options.method ?? "GET",
      headers,
      body: serializeBody(options.body, isForm),
      signal: options.signal,
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") {
      throw error;
    }

    throw ApiError.network();
  }

  if (!response.ok) {
    throw await ApiError.fromResponse(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  if (options.responseType === "blob") {
    return (await response.blob()) as T;
  }

  return (await response.json()) as T;
}

function serializeBody(body: unknown, isForm: boolean): BodyInit | undefined {
  if (body === undefined) {
    return undefined;
  }

  return isForm ? (body as FormData) : JSON.stringify(body);
}
