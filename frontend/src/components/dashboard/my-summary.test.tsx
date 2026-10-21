import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { MySummary } from "@/components/dashboard/my-summary";

const mocks = vi.hoisted(() => ({ me: vi.fn() }));

vi.mock("@/lib/api/reports", () => ({ reportsApi: { me: mocks.me } }));
vi.mock("@/lib/tasks/board", () => ({ todayIso: () => "2026-10-21" }));
vi.mock("next/link", () => ({
  default: ({ href, children, className }: { href: string; children: ReactNode; className?: string }) => (
    <a href={href} className={className}>
      {children}
    </a>
  ),
}));

beforeEach(() => {
  mocks.me.mockReset().mockResolvedValue({ open: 5, overdue: 2, dueThisWeek: 1, completedLast7Days: 4 });
});

describe("MySummary", () => {
  it("shows the user's numbers and links to the filtered task list", async () => {
    render(<MySummary />);

    const open = await screen.findByRole("link", { name: /Açık görevlerim/ });

    expect(open).toHaveTextContent("5");
    expect(open).toHaveAttribute("href", "/tasks?assignee=me&status=open");
    expect(screen.getByRole("link", { name: /Geciken/ })).toHaveAttribute("href", "/tasks?assignee=me&due=overdue");
    expect(screen.getByText("Son 7 günde bitirdiğim").closest("div")).toHaveTextContent("4");
    expect(mocks.me).toHaveBeenCalledWith("2026-10-21", expect.any(AbortSignal));
  });
});
