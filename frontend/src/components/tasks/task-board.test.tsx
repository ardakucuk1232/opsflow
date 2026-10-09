import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { TaskBoard } from "@/components/tasks/task-board";
import { TEST_USER } from "@/test/fixtures";
import { PROJECT_DETAIL } from "@/test/projects";
import { taskSummary } from "@/test/tasks";
import type { AuthUser } from "@/types/api";

const mocks = vi.hoisted(() => ({ listForProject: vi.fn(), move: vi.fn(), user: null as AuthUser | null }));

vi.mock("@/lib/api/tasks", () => ({ tasksApi: { listForProject: mocks.listForProject, move: mocks.move } }));
vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ user: mocks.user }) }));

const ME = { id: TEST_USER.id, firstName: "Arda", lastName: "Küçük", email: TEST_USER.email };

const TASKS = [
  taskSummary({ id: "a", key: "WEB-1", title: "Giriş sayfası", status: "Todo", boardOrder: 0, assignee: ME }),
  taskSummary({ id: "b", key: "WEB-2", title: "Kayıt sayfası", status: "Todo", boardOrder: 1 }),
  taskSummary({ id: "c", key: "WEB-3", title: "API bağlantısı", status: "InProgress", boardOrder: 0 }),
  taskSummary({ id: "d", key: "WEB-4", title: "Eski tasarım", status: "Cancelled", boardOrder: 0 }),
];

beforeEach(() => {
  mocks.listForProject.mockReset().mockResolvedValue(TASKS);
  mocks.move.mockReset().mockResolvedValue({});
  mocks.user = TEST_USER;
});

function renderBoard(onChanged = vi.fn()) {
  render(<TaskBoard project={PROJECT_DETAIL} refreshKey={0} onOpenTask={vi.fn()} onChanged={onChanged} />);

  return onChanged;
}

function column(name: string) {
  return screen.getByRole("region", { name });
}

describe("TaskBoard", () => {
  it("places each task in the column of its status and hides cancelled ones", async () => {
    renderBoard();

    await screen.findByText("Giriş sayfası");

    expect(within(column("Yapılacak")).getAllByRole("article")).toHaveLength(2);
    expect(within(column("Devam ediyor")).getByText("API bağlantısı")).toBeInTheDocument();
    expect(screen.queryByText("Eski tasarım")).not.toBeInTheDocument();
  });

  it("shows cancelled tasks on request", async () => {
    const user = userEvent.setup();
    renderBoard();
    await screen.findByText("Giriş sayfası");

    await user.click(screen.getByLabelText("İptal edilenleri göster"));

    expect(within(column("İptal edildi")).getByText("Eski tasarım")).toBeInTheDocument();
  });

  it("filters by assignee and by text", async () => {
    const user = userEvent.setup();
    renderBoard();
    await screen.findByText("Giriş sayfası");

    await user.selectOptions(screen.getByLabelText("Atanan kişiye göre filtrele"), "me");
    expect(screen.queryByText("Kayıt sayfası")).not.toBeInTheDocument();

    await user.selectOptions(screen.getByLabelText("Atanan kişiye göre filtrele"), "all");
    await user.type(screen.getByLabelText("Görev ara"), "web-3");
    expect(screen.getByText("API bağlantısı")).toBeInTheDocument();
    expect(screen.queryByText("Giriş sayfası")).not.toBeInTheDocument();
  });

  it("moves a dropped card before the card it was dropped on", async () => {
    const onChanged = renderBoard();
    await screen.findByText("Giriş sayfası");

    const source = screen.getByText("API bağlantısı").closest("article") as HTMLElement;
    const target = screen.getByText("Kayıt sayfası").closest("article") as HTMLElement;
    const dataTransfer = { setData: vi.fn(), effectAllowed: "" };

    fireEvent.dragStart(source, { dataTransfer });
    fireEvent.dragOver(target, { dataTransfer });
    fireEvent.drop(target, { dataTransfer });

    expect(mocks.move).toHaveBeenCalledWith("c", "Todo", 1);
    expect(within(column("Yapılacak")).getAllByRole("article").map((card) => card.textContent)).toEqual([
      expect.stringContaining("Giriş sayfası"),
      expect.stringContaining("API bağlantısı"),
      expect.stringContaining("Kayıt sayfası"),
    ]);
    await vi.waitFor(() => expect(onChanged).toHaveBeenCalled());
  });

  it("appends a card dropped on an empty part of a column", async () => {
    renderBoard();
    await screen.findByText("Giriş sayfası");

    const source = screen.getByText("Giriş sayfası").closest("article") as HTMLElement;
    const dataTransfer = { setData: vi.fn(), effectAllowed: "" };

    fireEvent.dragStart(source, { dataTransfer });
    fireEvent.dragOver(column("Tamamlandı"), { dataTransfer });
    fireEvent.drop(column("Tamamlandı"), { dataTransfer });

    expect(mocks.move).toHaveBeenCalledWith("a", "Done", 0);
  });

  it("puts the card back when the move fails", async () => {
    const { ApiError } = await import("@/lib/api/errors");
    mocks.move.mockRejectedValueOnce(new ApiError({ status: 403, code: "forbidden", message: "x" }));
    renderBoard();
    await screen.findByText("Giriş sayfası");

    const source = screen.getByText("Giriş sayfası").closest("article") as HTMLElement;
    const dataTransfer = { setData: vi.fn(), effectAllowed: "" };

    fireEvent.dragStart(source, { dataTransfer });
    fireEvent.drop(column("Tamamlandı"), { dataTransfer });

    expect(await screen.findByRole("alert")).toHaveTextContent("yetkiniz yok");
    expect(within(column("Yapılacak")).getByText("Giriş sayfası")).toBeInTheDocument();
  });

  it("does not let a user without update permission drag cards or create tasks", async () => {
    mocks.user = { ...TEST_USER, permissions: ["user.view"] };
    renderBoard();
    await screen.findByText("Giriş sayfası");

    expect(screen.getByText("Giriş sayfası").closest("article")).toHaveAttribute("draggable", "false");
    expect(screen.queryByRole("button", { name: "Yeni görev" })).not.toBeInTheDocument();
  });
});
