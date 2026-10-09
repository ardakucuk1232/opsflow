import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ProjectsPage } from "@/components/projects/projects-page";
import { TEST_USER } from "@/test/fixtures";
import { PROJECT_SUMMARY } from "@/test/projects";
import type { AuthUser, PagedResult, ProjectSummary } from "@/types/api";

const mocks = vi.hoisted(() => ({ list: vi.fn(), push: vi.fn(), user: null as AuthUser | null }));

vi.mock("@/lib/api/projects", () => ({ projectsApi: { list: mocks.list } }));
vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ user: mocks.user }) }));
vi.mock("next/navigation", () => ({ useRouter: () => ({ push: mocks.push }) }));
vi.mock("next/link", () => ({
  default: ({ href, children, className }: { href: string; children: ReactNode; className?: string }) => (
    <a href={href} className={className}>
      {children}
    </a>
  ),
}));

function page(items: ProjectSummary[]): PagedResult<ProjectSummary> {
  return { items, page: 1, pageSize: 12, totalCount: items.length, totalPages: 1, hasPreviousPage: false, hasNextPage: false };
}

const EMPLOYEE: AuthUser = { ...TEST_USER, roles: ["Employee"], permissions: ["user.view", "task.update"] };

beforeEach(() => {
  mocks.list.mockReset().mockResolvedValue(page([PROJECT_SUMMARY]));
  mocks.user = TEST_USER;
});

describe("ProjectsPage", () => {
  it("links each project card to its page", async () => {
    render(<ProjectsPage />);

    const card = await screen.findByRole("link", { name: /Web sitesi/ });

    expect(card).toHaveAttribute("href", "/projects/project-1");
    expect(card).toHaveTextContent("WEB");
    expect(card).toHaveTextContent("Aktif");
    expect(card).toHaveTextContent("Lider: Arda Küçük");
    expect(card).toHaveTextContent("Proje lideri");
  });

  it("lets a user with the permission create projects", async () => {
    render(<ProjectsPage />);
    await screen.findByRole("link", { name: /Web sitesi/ });

    expect(screen.getByRole("button", { name: "Yeni proje" })).toBeInTheDocument();
    expect(screen.getByLabelText("Sadece üyesi olduklarım")).toBeInTheDocument();
  });

  it("explains an empty list to an employee without create permission", async () => {
    mocks.user = EMPLOYEE;
    mocks.list.mockResolvedValue(page([]));
    render(<ProjectsPage />);

    expect(await screen.findByText("Bir projeye eklendiğinizde burada görünecek.")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Yeni proje" })).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Sadece üyesi olduklarım")).not.toBeInTheDocument();
  });
});
