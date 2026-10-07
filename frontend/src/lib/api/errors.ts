import type { ProblemDetails } from "@/types/api";

export const NETWORK_ERROR_CODE = "network_error";
export const UNKNOWN_ERROR_CODE = "unknown_error";

type ApiErrorInit = {
  status: number;
  code: string;
  message: string;
  fieldErrors?: Record<string, string[]>;
  retryAfterSeconds?: number | null;
};

export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly fieldErrors: Record<string, string[]>;
  readonly retryAfterSeconds: number | null;

  constructor(init: ApiErrorInit) {
    super(init.message);
    this.name = "ApiError";
    this.status = init.status;
    this.code = init.code;
    this.fieldErrors = init.fieldErrors ?? {};
    this.retryAfterSeconds = init.retryAfterSeconds ?? null;
  }

  get isNetworkError(): boolean {
    return this.code === NETWORK_ERROR_CODE;
  }

  get isTransient(): boolean {
    return this.isNetworkError || this.status === 429 || this.status >= 500;
  }

  static network(): ApiError {
    return new ApiError({
      status: 0,
      code: NETWORK_ERROR_CODE,
      message: "The server could not be reached.",
    });
  }

  static async fromResponse(response: Response): Promise<ApiError> {
    const problem = await readProblem(response);

    return new ApiError({
      status: response.status,
      code: problem?.code ?? UNKNOWN_ERROR_CODE,
      message: problem?.detail ?? problem?.title ?? response.statusText,
      fieldErrors: problem?.errors,
      retryAfterSeconds: parseRetryAfter(response.headers.get("Retry-After")),
    });
  }
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
  const contentType = response.headers.get("Content-Type") ?? "";

  if (!contentType.includes("json")) {
    return null;
  }

  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return null;
  }
}

function parseRetryAfter(value: string | null): number | null {
  if (!value) {
    return null;
  }

  const seconds = Number.parseInt(value, 10);

  return Number.isFinite(seconds) && seconds >= 0 ? seconds : null;
}
