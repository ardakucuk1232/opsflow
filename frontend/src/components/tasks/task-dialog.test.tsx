import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { TaskDialog } from "@/components/tasks/task-dialog";
import { TEST_USER } from "@/test/fixtures";
import { PROJECT_DETAIL } from "@/test/projects";
import { TASK_DETAIL } from "@/test/tasks";
import type { AuthUser } from "@/types/api";

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
  update: vi.fn(),
  move: vi.fn(),
  assign: vi.fn(),
  remove: vi.fn(),
  comments: vi.fn(),
  addComment: vi.fn(),
  deleteComment: vi.fn(),
  user: null as AuthUser | null,
}));

vi.mock("@/lib/api/tasks", () => ({ tasksApi: mocks }));
vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ user: mocks.user }) }));

beforeEach(() => {
  for (const mock of [mocks.get, mocks.update, mocks.move, mocks.assign, mocks.remove, mocks.addComment, mocks.deleteComment]) {
    mock.mockReset().mockResolvedValue(TASK_DETAIL);
  }
  mocks.comments.mockReset().mockResolvedValue([]);
  mocks.user = TEST_USER;
});

function renderDialog(callbacks = { onClose: vi.fn(), onChanged: vi.fn() }) {
  render(<TaskDialog taskId="task-1" project={PROJECT_DETAIL} {...callbacks} />);

  return callbacks;
}

describe("TaskDialog", () => {
  it("saves edited fields", async () => {
    const user = userEvent.setup();
    const { onChanged } = renderDialog();

    const title = await screen.findByLabelText("Başlık");
    await user.clear(title);
    await user.type(title, "Yeni başlık");
    await user.selectOptions(screen.getByLabelText("Öncelik"), "Critical");
    await user.click(screen.getByRole("button", { name: "Değişiklikleri kaydet" }));

    expect(mocks.update).toHaveBeenCalledWith("task-1", {
      title: "Yeni başlık",
      description: "Yeni tasarımı uygula",
      priority: "Critical",
      dueDate: "2026-10-20",
    });
    await vi.waitFor(() => expect(onChanged).toHaveBeenCalled());
  });

  it("changes the status and the assignee", async () => {
    const user = userEvent.setup();
    renderDialog();
    await screen.findByLabelText("Başlık");

    await user.selectOptions(screen.getByLabelText("Durum"), "Done");
    await user.selectOptions(screen.getByLabelText("Atanan kişi"), "member-2");

    expect(mocks.move).toHaveBeenCalledWith("task-1", "Done", 1_000_000);
    expect(mocks.assign).toHaveBeenCalledWith("task-1", "member-2");
  });

  it("shows a read only view to someone who cannot update tasks", async () => {
    mocks.user = { ...TEST_USER, permissions: ["user.view", "comment.create"] };
    renderDialog();

    expect(await screen.findByRole("heading", { name: "Ana sayfa tasarımı" })).toBeInTheDocument();
    expect(screen.queryByLabelText("Başlık")).not.toBeInTheDocument();
    expect(screen.queryByRole("combobox", { name: "Durum" })).not.toBeInTheDocument();
    expect(screen.getByLabelText("Yorum yaz")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Sil" })).not.toBeInTheDocument();
  });

  it("deletes the task after confirmation", async () => {
    const user = userEvent.setup();
    const { onClose, onChanged } = renderDialog();
    await screen.findByLabelText("Başlık");

    await user.click(screen.getByRole("button", { name: "Sil" }));
    await user.click(screen.getByRole("button", { name: "Görevi sil" }));

    expect(mocks.remove).toHaveBeenCalledWith("task-1");
    await vi.waitFor(() => expect(onClose).toHaveBeenCalled());
    expect(onChanged).toHaveBeenCalled();
  });

  it("adds a comment", async () => {
    const user = userEvent.setup();
    renderDialog();
    await screen.findByText("Henüz yorum yok.");

    await user.type(screen.getByLabelText("Yorum yaz"), "Tasarım onaylandı");
    await user.click(screen.getByRole("button", { name: "Yorum ekle" }));

    expect(mocks.addComment).toHaveBeenCalledWith("task-1", "Tasarım onaylandı");
    expect(screen.getByLabelText("Yorum yaz")).toHaveValue("");
  });

  it("lets the author delete their comment", async () => {
    const user = userEvent.setup();
    mocks.comments.mockResolvedValue([
      {
        id: "comment-1",
        author: { id: TEST_USER.id, firstName: "Arda", lastName: "Küçük", email: TEST_USER.email },
        body: "İlk yorum",
        createdAt: "2026-10-09T10:00:00Z",
        canDelete: true,
      },
    ]);
    renderDialog();

    await user.click(await screen.findByRole("button", { name: "Yorumu sil" }));

    expect(mocks.deleteComment).toHaveBeenCalledWith("task-1", "comment-1");
  });
});
