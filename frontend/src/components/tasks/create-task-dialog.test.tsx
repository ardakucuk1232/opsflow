import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { CreateTaskDialog } from "@/components/tasks/create-task-dialog";
import { TEST_USER } from "@/test/fixtures";
import { PROJECT_DETAIL } from "@/test/projects";
import { TASK_DETAIL } from "@/test/tasks";
import type { AuthUser } from "@/types/api";

const mocks = vi.hoisted(() => ({ create: vi.fn(), user: null as AuthUser | null }));

vi.mock("@/lib/api/tasks", () => ({ tasksApi: { create: mocks.create } }));
vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ user: mocks.user }) }));

beforeEach(() => {
  mocks.create.mockReset().mockResolvedValue(TASK_DETAIL);
  mocks.user = TEST_USER;
});

describe("CreateTaskDialog", () => {
  it("creates a task with the chosen fields", async () => {
    const user = userEvent.setup();
    const onCreated = vi.fn();
    render(<CreateTaskDialog open project={PROJECT_DETAIL} onClose={vi.fn()} onCreated={onCreated} />);

    await user.type(screen.getByLabelText("Başlık"), "Ödeme sayfası");
    await user.selectOptions(screen.getByLabelText("Öncelik"), "High");
    await user.selectOptions(screen.getByLabelText("Atanan kişi"), "member-2");
    await user.type(screen.getByLabelText("Bitiş tarihi"), "2026-11-15");
    await user.click(screen.getByRole("button", { name: "Görevi oluştur" }));

    expect(mocks.create).toHaveBeenCalledWith("project-1", {
      title: "Ödeme sayfası",
      description: null,
      status: "Todo",
      priority: "High",
      assigneeId: "member-2",
      dueDate: "2026-11-15",
    });
    expect(onCreated).toHaveBeenCalledWith(TASK_DETAIL);
  });

  it("requires a title", async () => {
    const user = userEvent.setup();
    render(<CreateTaskDialog open project={PROJECT_DETAIL} onClose={vi.fn()} onCreated={vi.fn()} />);

    await user.click(screen.getByRole("button", { name: "Görevi oluştur" }));

    expect(await screen.findByText("Başlık zorunludur.")).toBeInTheDocument();
    expect(mocks.create).not.toHaveBeenCalled();
  });

  it("hides the assignee field from someone who cannot assign tasks", () => {
    mocks.user = { ...TEST_USER, permissions: ["task.create"] };
    render(<CreateTaskDialog open project={PROJECT_DETAIL} onClose={vi.fn()} onCreated={vi.fn()} />);

    expect(screen.queryByLabelText("Atanan kişi")).not.toBeInTheDocument();
  });
});
