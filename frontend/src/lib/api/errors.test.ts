import { describe, expect, it } from "vitest";
import { ApiError, UNKNOWN_ERROR_CODE } from "@/lib/api/errors";

describe("ApiError.fromResponse", () => {
  it("reads the code, field errors and retry delay from a problem details response", async () => {
    const response = new Response(
      JSON.stringify({
        status: 400,
        code: "validation_failed",
        detail: "One or more validation errors occurred.",
        errors: { email: ["Email is required."] },
      }),
      {
        status: 400,
        headers: { "Content-Type": "application/problem+json", "Retry-After": "12" },
      },
    );

    const error = await ApiError.fromResponse(response);

    expect(error.status).toBe(400);
    expect(error.code).toBe("validation_failed");
    expect(error.fieldErrors).toEqual({ email: ["Email is required."] });
    expect(error.retryAfterSeconds).toBe(12);
  });

  it("uses the unknown code when the body is not JSON", async () => {
    const response = new Response("<html>Bad Gateway</html>", {
      status: 502,
      headers: { "Content-Type": "text/html" },
    });

    const error = await ApiError.fromResponse(response);

    expect(error.code).toBe(UNKNOWN_ERROR_CODE);
    expect(error.fieldErrors).toEqual({});
    expect(error.retryAfterSeconds).toBeNull();
  });

  it.each([
    [0, "network_error", true],
    [429, "too_many_requests", true],
    [503, "internal_error", true],
    [401, "auth.invalid_refresh_token", false],
    [403, "forbidden", false],
  ])("treats status %i (%s) as transient: %s", (status, code, expected) => {
    expect(new ApiError({ status, code, message: "x" }).isTransient).toBe(expected);
  });
});
