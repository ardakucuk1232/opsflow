import { describe, expect, it } from "vitest";
import { formatFileSize, MAX_ATTACHMENT_BYTES, validateAttachment } from "@/lib/attachments/files";

describe("validateAttachment", () => {
  it("accepts allowed extensions regardless of case", () => {
    expect(validateAttachment({ name: "Rapor.PDF", size: 1_000 })).toBeNull();
    expect(validateAttachment({ name: "tablo.xlsx", size: MAX_ATTACHMENT_BYTES })).toBeNull();
  });

  it("rejects other file types", () => {
    expect(validateAttachment({ name: "kurulum.exe", size: 10 })).toMatch(/dosya türü/);
    expect(validateAttachment({ name: "sayfa.html", size: 10 })).toMatch(/dosya türü/);
    expect(validateAttachment({ name: "README", size: 10 })).toMatch(/dosya türü/);
  });

  it("rejects empty and too large files", () => {
    expect(validateAttachment({ name: "bos.txt", size: 0 })).toBe("Boş dosya yüklenemez.");
    expect(validateAttachment({ name: "buyuk.zip", size: MAX_ATTACHMENT_BYTES + 1 })).toBe("Dosya 10 MB'tan büyük olamaz.");
  });
});

describe("formatFileSize", () => {
  it("uses the closest unit", () => {
    expect(formatFileSize(512)).toBe("512 B");
    expect(formatFileSize(1_536)).toBe("1,5 KB");
    expect(formatFileSize(3 * 1024 * 1024)).toBe("3 MB");
  });
});
