import { describe, expect, it } from "vitest";
import { formatDate, formatRelativeTime } from "@/lib/utils/format";

const NOW = Date.parse("2026-10-10T12:00:00Z");

function ago(seconds: number): string {
  return new Date(NOW - seconds * 1000).toISOString();
}

describe("formatRelativeTime", () => {
  it("describes recent moments in Turkish", () => {
    expect(formatRelativeTime(ago(20), NOW)).toBe("az önce");
    expect(formatRelativeTime(ago(5 * 60), NOW)).toBe("5 dakika önce");
    expect(formatRelativeTime(ago(3 * 3_600), NOW)).toBe("3 saat önce");
    expect(formatRelativeTime(ago(86_400), NOW)).toBe("dün");
  });

  it("falls back to the date after a week", () => {
    const value = ago(10 * 86_400);

    expect(formatRelativeTime(value, NOW)).toBe(formatDate(value));
  });
});
