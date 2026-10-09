import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ProjectDetailPage } from "@/components/projects/project-detail-page";
import { ApiError } from "@/lib/api/errors";
import { TEST_USER } from "@/test/fixtures";
import { PROJECT_DETAIL } from "@/test/projects";
import { TASK_DETAIL, taskSummary } from "@/test/tasks";
import type { AuthUser } from "@/types/api";

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
  listForProject: vi.fn(),
  getTask: vi.fn(),
  comments: vi.fn(),
  replace: vi.fn(),
  search: "",
  user: null as AuthUser | null,
}));

vi.mock("@/lib/api/projects", () => ({ projectsApi: { get: mocks.get } }));
vi.mock("@/lib/api/tasks", () => ({
  tasksApi: { listForProject: mocks.listForProject, get: mocks.getTask, comments: mocks.comments },
}));
vi.mock("@/lib/api/users", () => ({ usersApi: { list: vi.fn() } }));
vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ user: mocks.user }) }));
vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), replace: mocks.replace }),
  useSearchParams: () => new URLSearchParams(mocks.search),
}));
vi.mock("next/link", () => ({
  default: ({ href, children }: { href: string; children: ReactNode }) => <a href={href}>{children}</a>,
}));

beforeEach(() => {
  mocks.get.mockReset().mockResolvedValue(PROJECT_DETAIL);
  mocks.listForProject.mockReset().mockResolvedValue([taskSummary({ title: "Ana sayfa tasarımı" })]);
  mocks.getTask.mockReset().mockResolvedValue(TASK_DETAIL);
  mocks.comments.mockReset().mockResolvedValue([]);
  mocks.replace.mockReset();
  mocks.search = "";
  mocks.user = TEST_USER;
});

describe("ProjectDetailPage", () => {
  it("opens on the board", async () => {
    render(<ProjectDetailPage projectId="project-1" />);

    expect(await screen.findByRole("heading", { name: "Web sitesi" })).toBeInTheDocument();
    expect(await screen.findByText("Ana sayfa tasarımı")).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: "Pano" })).toHaveAttribute("aria-selected", "true");
    expect(screen.getByRole("region", { name: "Yapılacak" })).toBeInTheDocument();
  });

  it("shows the description and members on the overview tab", async () => {
    const user = userEvent.setup();
    render(<ProjectDetailPage projectId="project-1" />);
    await screen.findByRole("heading", { name: "Web sitesi" });

    await user.click(screen.getByRole("tab", { name: "Genel bakış" }));

    expect(screen.getByText("Kurumsal web sitesinin yenilenmesi")).toBeInTheDocument();
    expect(screen.getByText("ayse@abc.com")).toBeInTheDocument();
  });

  it("opens a task from the address and puts it in the address when clicked", async () => {
    const user = userEvent.setup();
    render(<ProjectDetailPage projectId="project-1" />);

    await user.click(await screen.findByRole("button", { name: /WEB-1/ }));

    expect(mocks.replace).toHaveBeenCalledWith("/projects/project-1?task=task-1", { scroll: false });
  });

  it("shows the task dialog when the address names a task", async () => {
    mocks.search = "task=task-1";
    render(<ProjectDetailPage projectId="project-1" />);

    expect(await screen.findByRole("dialog")).toBeInTheDocument();
    expect(await screen.findByDisplayValue("Yeni tasarımı uygula")).toBeInTheDocument();
    expect(mocks.getTask).toHaveBeenCalledWith("task-1", expect.any(AbortSignal));
  });

  it("hides edit and delete from a plain member", async () => {
    mocks.user = { ...TEST_USER, permissions: ["user.view"] };
    mocks.get.mockResolvedValue({ ...PROJECT_DETAIL, canManage: false, currentUserRole: "Member" });
    render(<ProjectDetailPage projectId="project-1" />);

    await screen.findByRole("heading", { name: "Web sitesi" });

    expect(screen.queryByRole("button", { name: "Düzenle" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Projeyi sil" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Yeni görev" })).not.toBeInTheDocument();
  });

  it("shows a not found message for a project the user cannot see", async () => {
    mocks.get.mockRejectedValue(new ApiError({ status: 404, code: "not_found", message: "x" }));
    render(<ProjectDetailPage projectId="other" />);

    expect(await screen.findByText("Proje bulunamadı")).toBeInTheDocument();
  });
});
