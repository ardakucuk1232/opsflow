import { describe, expect, it } from "vitest";
import { DEFAULT_AUTHENTICATED_PATH, getSafeRedirectPath } from "@/lib/utils/redirect";

describe("getSafeRedirectPath", () => {
  it.each(["/dashboard", "/projects/42", "/projects?status=active"])(
    "keeps the internal path %s",
    (path) => {
      expect(getSafeRedirectPath(path)).toBe(path);
    },
  );

  it.each([
    null,
    undefined,
    "",
    "dashboard",
    "https://evil.example",
    "//evil.example",
    "/\\evil.example",
    "javascript:alert(1)",
  ])("falls back to the default path for %s", (value) => {
    expect(getSafeRedirectPath(value)).toBe(DEFAULT_AUTHENTICATED_PATH);
  });
});
