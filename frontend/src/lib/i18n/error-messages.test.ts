import { describe, expect, it } from "vitest";
import { ApiError } from "@/lib/api/errors";
import { getErrorMessage } from "@/lib/i18n/error-messages";

describe("getErrorMessage", () => {
  it("maps a known error code to its Turkish message", () => {
    const error = new ApiError({ status: 401, code: "auth.invalid_credentials", message: "x" });

    expect(getErrorMessage(error)).toBe("E-posta veya şifre hatalı.");
  });

  it("includes the wait time when the rate limit response provides it", () => {
    const error = new ApiError({
      status: 429,
      code: "too_many_requests",
      message: "x",
      retryAfterSeconds: 42,
    });

    expect(getErrorMessage(error)).toContain("42 saniye");
  });

  it("uses a generic rate limit message when the wait time is unknown", () => {
    const error = new ApiError({ status: 429, code: "too_many_requests", message: "x" });

    expect(getErrorMessage(error)).toContain("Çok fazla deneme");
  });

  it("describes a network failure", () => {
    expect(getErrorMessage(ApiError.network())).toContain("Sunucuya ulaşılamıyor");
  });

  it("never exposes the raw server message for an unknown code", () => {
    const error = new ApiError({ status: 500, code: "something.new", message: "NullReference at X" });

    expect(getErrorMessage(error)).toBe("Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.");
  });

  it("falls back to the generic message for non-API errors", () => {
    expect(getErrorMessage(new Error("boom"))).toBe(
      "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.",
    );
  });
});
