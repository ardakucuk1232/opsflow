import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AssignedTasks } from "@/components/dashboard/assigned-tasks";

const mocks = vi.hoisted(() => ({ assignedToMe: vi.fn() }));

vi.mock("@/lib/api/tasks", () => ({ tasksApi: { assignedToMe: mocks.assignedToMe } }));
vi.mock("next/link", () => ({
  default: ({ href, children }: { href: string; children: ReactNode }) => <a href={href}>{children}</a>,
}));

beforeEach(() => {
  mocks.assignedToMe.mockReset();
});

describe("AssignedTasks", () => {
  it("links each task to its project board and flags overdue ones", async () => {
    mocks.assignedToMe.mockResolvedValue([
      {
        id: "task-1",
        projectId: "project-1",
        projectName: "Web sitesi",
        key: "WEB-1",
        title: "Ana sayfa tasarımı",
        status: "InProgress",
        priority: "High",
        dueDate: "2020-01-01",
      },
    ]);
    render(<AssignedTasks />);

    const link = await screen.findByRole("link", { name: /Ana sayfa tasarımı/ });

    expect(link).toHaveAttribute("href", "/projects/project-1?task=task-1");
    expect(link).toHaveTextContent("Devam ediyor");
    expect(link).toHaveTextContent("gecikti");
  });

  it("says when nothing is assigned", async () => {
    mocks.assignedToMe.mockResolvedValue([]);
    render(<AssignedTasks />);

    expect(await screen.findByText("Size atanmış açık görev yok.")).toBeInTheDocument();
  });
});
