import { describe, expect, it } from "vitest";
import { readFragmentParam } from "@/lib/utils/fragment";

describe("readFragmentParam", () => {
  it("reads a parameter from the URL fragment", () => {
    expect(readFragmentParam("#token=abc-123_XYZ", "token")).toBe("abc-123_XYZ");
  });

  it("decodes escaped characters", () => {
    expect(readFragmentParam("#token=a%20b%26c", "token")).toBe("a b&c");
  });

  it.each(["", "#", "#token=", "#other=1"])("returns null for %j", (hash) => {
    expect(readFragmentParam(hash, "token")).toBeNull();
  });
});
