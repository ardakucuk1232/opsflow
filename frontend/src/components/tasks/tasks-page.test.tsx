import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { TasksPage } from "@/components/tasks/tasks-page";
import { TEST_USER } from "@/test/fixtures";
import { PROJECT_SUMMARY } from "@/test/projects";
import type { PagedResult, TaskListItem } from "@/types/api";

const mocks = vi.hoisted(() => ({
  list: vi.fn(),
  projects: vi.fn(),
  replace: vi.fn(),
  search: "",
}));

vi.mock("@/lib/api/tasks", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@/lib/api/tasks")>()),
  tasksApi: { list: mocks.list },
}));
vi.mock("@/lib/api/projects", () => ({ projectsApi: { list: mocks.projects } }));
vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ user: TEST_USER }) }));
vi.mock("@/lib/tasks/board", () => ({ todayIso: () => "2026-10-11" }));
vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: mocks.replace }),
  usePathname: () => "/tasks",
  useSearchParams: () => new URLSearchParams(mocks.search),
}));
vi.mock("next/link", () => ({
  default: ({ href, children, className }: { href: string; children: ReactNode; className?: string }) => (
    <a href={href} className={className}>
      {children}
    </a>
  ),
}));

function item(overrides: Partial<TaskListItem>): TaskListItem {
  return {
    id: "t1",
    projectId: "project-1",
    projectKey: "WEB",
    projectName: "Web sitesi",
    key: "WEB-1",
    title: "Giriş sayfası",
    status: "Todo",
    priority: "High",
    assignee: null,
    dueDate: null,
    createdAt: "2026-10-01T09:00:00Z",
    updatedAt: null,
    ...overrides,
  };
}

function page<T>(items: T[], totalCount = items.length): PagedResult<T> {
  return { items, page: 1, pageSize: 20, totalCount, totalPages: Math.ceil(totalCount / 20), hasPreviousPage: false, hasNextPage: false };
}

beforeEach(() => {
  mocks.search = "";
  mocks.list.mockReset().mockResolvedValue(
    page([
      item({ dueDate: "2026-10-05", assignee: { id: "u2", firstName: "Ayşe", lastName: "Yılmaz", email: "a@b.c" } }),
      item({ id: "t2", key: "WEB-2", title: "Rapor", status: "Done", dueDate: "2026-10-05" }),
    ]),
  );
  mocks.projects.mockReset().mockResolvedValue(page([PROJECT_SUMMARY]));
  mocks.replace.mockReset();
});

describe("TasksPage", () => {
  it("lists tasks with links to their project board", async () => {
    render(<TasksPage />);

    const link = await screen.findByRole("link", { name: /Giriş sayfası/ });

    expect(link).toHaveAttribute("href", "/projects/project-1?task=t1");
    expect(link).toHaveTextContent("Ayşe Yılmaz");
    expect(link).toHaveTextContent("(gecikti)");
    expect(screen.getByRole("link", { name: /Rapor/ })).not.toHaveTextContent("(gecikti)");
  });

  it("reads its filters from the URL", async () => {
    mocks.search = "q=rapor&status=open&assignee=me&sort=DueDate&page=2";
    render(<TasksPage />);

    await screen.findByText("Giriş sayfası");

    expect(mocks.list).toHaveBeenCalledWith(
      expect.objectContaining({
        search: "rapor",
        status: ["Backlog", "Todo", "InProgress", "InReview"],
        assigneeId: TEST_USER.id,
        sort: "DueDate",
        page: 2,
      }),
      expect.any(AbortSignal),
    );
    expect(screen.getByLabelText("Görev ara")).toHaveValue("rapor");
  });

  it("writes a changed filter to the URL and goes back to the first page", async () => {
    const user = userEvent.setup();
    mocks.search = "page=3";
    render(<TasksPage />);
    await screen.findByText("Giriş sayfası");

    await user.selectOptions(screen.getByLabelText("Öncelik"), "Critical");

    expect(mocks.replace).toHaveBeenCalledWith("/tasks?priority=Critical", { scroll: false });
  });

  it("updates the URL after typing a search", async () => {
    const user = userEvent.setup();
    render(<TasksPage />);
    await screen.findByText("Giriş sayfası");

    await user.type(screen.getByLabelText("Görev ara"), "web-2");

    await waitFor(() => expect(mocks.replace).toHaveBeenCalledWith("/tasks?q=web-2", { scroll: false }));
  });

  it("follows a search that arrives through the URL", async () => {
    mocks.search = "q=eski";
    const { rerender } = render(<TasksPage />);
    await screen.findByText("Giriş sayfası");

    mocks.search = "q=yeni";
    rerender(<TasksPage />);

    expect(screen.getByLabelText("Görev ara")).toHaveValue("yeni");
    await new Promise((resolve) => setTimeout(resolve, 400));
    expect(mocks.replace).not.toHaveBeenCalled();
  });

  it("offers to clear filters when nothing matches", async () => {
    const user = userEvent.setup();
    mocks.search = "priority=Low&sort=Priority";
    mocks.list.mockResolvedValue(page([]));
    render(<TasksPage />);

    await user.click(await screen.findByRole("button", { name: "Filtreleri temizle" }));

    expect(screen.getByText("Filtrelerle eşleşen görev yok")).toBeInTheDocument();
    expect(mocks.replace).toHaveBeenCalledWith("/tasks?sort=Priority", { scroll: false });
  });
});
