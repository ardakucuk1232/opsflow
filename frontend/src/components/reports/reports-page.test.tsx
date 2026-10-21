import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ReportsPage } from "@/components/reports/reports-page";
import { PROJECT_SUMMARY } from "@/test/projects";
import type { ReportOverview } from "@/types/api";

const mocks = vi.hoisted(() => ({ overview: vi.fn(), projects: vi.fn() }));

vi.mock("@/lib/api/reports", () => ({ reportsApi: { overview: mocks.overview } }));
vi.mock("@/lib/api/projects", () => ({ projectsApi: { list: mocks.projects } }));
vi.mock("@/lib/tasks/board", () => ({ todayIso: () => "2026-10-21" }));
vi.mock("next/link", () => ({
  default: ({ href, children, className }: { href: string; children: ReactNode; className?: string }) => (
    <a href={href} className={className}>
      {children}
    </a>
  ),
}));

const REPORT: ReportOverview = {
  today: "2026-10-21",
  totals: { activeProjects: 2, openTasks: 7, overdueTasks: 3, unassignedOpenTasks: 1, completedLast30Days: 5 },
  byStatus: [
    { status: "Backlog", count: 0 },
    { status: "Todo", count: 4 },
    { status: "InProgress", count: 3 },
    { status: "InReview", count: 0 },
    { status: "Done", count: 5 },
    { status: "Cancelled", count: 1 },
  ],
  openByPriority: [
    { priority: "Low", count: 1 },
    { priority: "Medium", count: 2 },
    { priority: "High", count: 3 },
    { priority: "Critical", count: 1 },
  ],
  weekly: Array.from({ length: 8 }, (_, index) => ({
    weekStart: `2026-${index < 3 ? "09" : "10"}-${String(index < 3 ? 1 + index * 7 : (index - 3) * 7 + 6).padStart(2, "0")}`,
    created: index,
    completed: index % 3,
  })),
  workload: [{ user: { id: "u2", firstName: "Ayşe", lastName: "Yılmaz", email: "a@b.c" }, open: 4, overdue: 2 }],
  projects: [{ id: "project-1", key: "WEB", name: "Web sitesi", status: "Active", total: 8, done: 2, open: 6, overdue: 3 }],
};

beforeEach(() => {
  mocks.overview.mockReset().mockResolvedValue(REPORT);
  mocks.projects.mockReset().mockResolvedValue({
    items: [PROJECT_SUMMARY],
    page: 1,
    pageSize: 100,
    totalCount: 1,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  });
});

describe("ReportsPage", () => {
  it("shows totals that link to the matching task lists", async () => {
    render(<ReportsPage />);

    const overdue = await screen.findByRole("link", { name: /Geciken/ });

    expect(overdue).toHaveTextContent("3");
    expect(overdue).toHaveAttribute("href", "/tasks?due=overdue");
    expect(screen.getByRole("link", { name: /Atanmamış/ })).toHaveAttribute("href", "/tasks?status=open&assignee=none");
  });

  it("lists counts by status and the workload of each person", async () => {
    render(<ReportsPage />);

    const status = await screen.findByRole("list", { name: "Duruma göre görev sayıları" });
    expect(within(status).getByText("Devam ediyor").closest("li")).toHaveTextContent("3");

    const workload = screen.getByRole("list", { name: "Kişi başına açık görev sayıları" });
    expect(workload).toHaveTextContent("Ayşe Yılmaz");
    expect(workload).toHaveTextContent("2 gecikmiş");
  });

  it("shows project progress as a meter", async () => {
    render(<ReportsPage />);

    const meter = await screen.findByRole("meter", { name: "Web sitesi ilerlemesi" });

    expect(meter).toHaveAttribute("aria-valuenow", "25");
  });

  it("can show the weekly chart as a table", async () => {
    const user = userEvent.setup();
    render(<ReportsPage />);

    await user.click(await screen.findByRole("button", { name: "Tablo olarak göster" }));

    const rows = within(screen.getByRole("table", { name: "Haftalık oluşturulan ve tamamlanan görevler" })).getAllByRole("row");
    expect(rows).toHaveLength(9);
  });

  it("reloads for the chosen project", async () => {
    const user = userEvent.setup();
    render(<ReportsPage />);
    await screen.findByRole("link", { name: /Geciken/ });
    await waitFor(() => expect(screen.getByRole("option", { name: /Web sitesi/ })).toBeInTheDocument());

    await user.selectOptions(screen.getByLabelText("Proje"), "project-1");

    await waitFor(() =>
      expect(mocks.overview).toHaveBeenLastCalledWith({ projectId: "project-1", today: "2026-10-21" }, expect.any(AbortSignal)),
    );
    expect(await screen.findByRole("link", { name: /Geciken/ })).toHaveAttribute("href", "/tasks?due=overdue&project=project-1");
  });
});
