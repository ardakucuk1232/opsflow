import { describe, expect, it } from "vitest";
import { suggestProjectKey } from "@/lib/utils/project-key";

describe("suggestProjectKey", () => {
  it.each([
    ["Web sitesi", "WS"],
    ["Müşteri İlişkileri Yönetimi", "MIY"],
    ["Çağrı merkezi şikayet takibi", "CMST"],
    ["Muhasebe", "MUHA"],
    ["Öğrenci portalı 2026", "OP"],
    ["", ""],
    ["   ", ""],
    ["2026", ""],
  ])("suggests a key for %j", (name, expected) => {
    expect(suggestProjectKey(name)).toBe(expected);
  });

  it("never suggests more than six letters", () => {
    expect(suggestProjectKey("a b c d e f g h")).toBe("ABCDEF");
  });
});
