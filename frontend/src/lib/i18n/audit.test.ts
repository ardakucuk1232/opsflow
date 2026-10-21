import { describe, expect, it } from "vitest";
import { formatAuditValue, getAuditEntityLabel, getAuditFieldLabel } from "@/lib/i18n/audit";

describe("audit labels", () => {
  it("translates entity and field names, keeping unknown ones", () => {
    expect(getAuditEntityLabel("TaskItem")).toBe("Görev");
    expect(getAuditEntityLabel("Something")).toBe("Something");
    expect(getAuditFieldLabel("DueDate")).toBe("Bitiş tarihi");
    expect(getAuditFieldLabel("Custom")).toBe("Custom");
  });
});

describe("formatAuditValue", () => {
  it("formats enums with their Turkish names", () => {
    expect(formatAuditValue("InProgress", "Status")).toBe("Devam ediyor");
    expect(formatAuditValue("Active", "Status")).toBe("Aktif");
    expect(formatAuditValue("Critical", "Priority")).toBe("Kritik");
    expect(formatAuditValue("Lead", "Role")).toBe("Proje lideri");
  });

  it("formats simple values", () => {
    expect(formatAuditValue(null, "Title")).toBe("—");
    expect(formatAuditValue(true, "IsActive")).toBe("Evet");
    expect(formatAuditValue(false, "IsActive")).toBe("Hayır");
    expect(formatAuditValue("***", "PasswordHash")).toBe("Gizli");
    expect(formatAuditValue("0b0f6f0e-6f0a-4a57-9d0e-3f0f8f1f2a01", "AssigneeId")).toBe("#0b0f6f0e");
    expect(formatAuditValue(["task.create", "task.update"], "Permissions")).toBe("task.create, task.update");
    expect(formatAuditValue(2048, "SizeBytes")).toBe("2.048 B");
  });

  it("formats due dates as days", () => {
    expect(formatAuditValue("2026-10-20T00:00:00+00:00", "DueDate")).toBe("20 Eki 2026");
    expect(formatAuditValue("2026-11-01", "StartDate")).toBe("1 Kas 2026");
  });
});
