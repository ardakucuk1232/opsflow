import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuditLogPage } from "@/components/audit/audit-log-page";
import type { AuditLogEntry, PagedResult } from "@/types/api";

const mocks = vi.hoisted(() => ({ list: vi.fn() }));

vi.mock("@/lib/api/audit-logs", () => ({ auditLogsApi: { list: mocks.list } }));

const ENTRY: AuditLogEntry = {
  id: "a1",
  action: "Updated",
  entityName: "TaskItem",
  entityId: "0b0f6f0e-6f0a-4a57-9d0e-3f0f8f1f2a01",
  entityLabel: "Giriş sayfası",
  user: { id: "u1", firstName: "Arda", lastName: "Küçük", email: "arda@abc.com" },
  changes: [
    { field: "Status", oldValue: "Todo", newValue: "InProgress" },
    { field: "Title", oldValue: "Giriş", newValue: "Giriş sayfası" },
  ],
  ipAddress: "10.0.0.5",
  createdAt: "2026-10-20T09:30:00Z",
};

function page(items: AuditLogEntry[]): PagedResult<AuditLogEntry> {
  return { items, page: 1, pageSize: 25, totalCount: items.length, totalPages: 1, hasPreviousPage: false, hasNextPage: false };
}

beforeEach(() => {
  mocks.list.mockReset().mockResolvedValue(
    page([ENTRY, { ...ENTRY, id: "a2", action: "Created", entityName: "Project", entityLabel: "WEB · Web sitesi", user: null, changes: [] }]),
  );
});

describe("AuditLogPage", () => {
  it("lists entries with action, type, label and actor", async () => {
    render(<AuditLogPage />);

    const row = await screen.findByRole("button", { name: /Giriş sayfası/ });

    expect(row).toHaveTextContent("Güncellendi");
    expect(row).toHaveTextContent("Görev");
    expect(row).toHaveTextContent("Arda Küçük");
    expect(screen.getByRole("button", { name: /Web sitesi/ })).toHaveTextContent("Sistem");
  });

  it("shows the changed fields when an entry is opened", async () => {
    const user = userEvent.setup();
    render(<AuditLogPage />);

    await user.click(await screen.findByRole("button", { name: /Giriş sayfası/ }));

    const table = screen.getByRole("table");
    expect(within(table).getByRole("row", { name: /Durum Yapılacak Devam ediyor/ })).toBeInTheDocument();
    expect(within(table).getByRole("row", { name: /Başlık Giriş Giriş sayfası/ })).toBeInTheDocument();
    expect(screen.getByText(/IP 10\.0\.0\.5/)).toBeInTheDocument();
  });

  it("filters by type, action and dates", async () => {
    const user = userEvent.setup();
    render(<AuditLogPage />);
    await screen.findByRole("button", { name: /Giriş sayfası/ });

    await user.selectOptions(screen.getByLabelText("Kayıt türü"), "Project");
    await user.selectOptions(screen.getByLabelText("İşlem"), "Deleted");
    await user.type(screen.getByLabelText("Başlangıç tarihi"), "2026-10-01");

    await waitFor(() =>
      expect(mocks.list).toHaveBeenLastCalledWith(
        expect.objectContaining({ entityName: "Project", action: "Deleted", from: "2026-10-01", page: 1 }),
        expect.any(AbortSignal),
      ),
    );
  });

  it("says so when nothing matches", async () => {
    mocks.list.mockResolvedValue(page([]));
    render(<AuditLogPage />);

    expect(await screen.findByText("Kayıt bulunamadı")).toBeInTheDocument();
  });
});
