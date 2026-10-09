import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ProjectDetailPage } from "@/components/projects/project-detail-page";
import { ApiError } from "@/lib/api/errors";
import { TEST_USER } from "@/test/fixtures";
import { PROJECT_DETAIL } from "@/test/projects";
import type { AuthUser } from "@/types/api";

const mocks = vi.hoisted(() => ({ get: vi.fn(), user: null as AuthUser | null }));

vi.mock("@/lib/api/projects", () => ({ projectsApi: { get: mocks.get } }));
vi.mock("@/lib/api/users", () => ({ usersApi: { list: vi.fn() } }));
vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ user: mocks.user }) }));
vi.mock("next/navigation", () => ({ useRouter: () => ({ push: vi.fn() }) }));
vi.mock("next/link", () => ({
  default: ({ href, children }: { href: string; children: ReactNode }) => <a href={href}>{children}</a>,
}));

beforeEach(() => {
  mocks.get.mockReset().mockResolvedValue(PROJECT_DETAIL);
  mocks.user = TEST_USER;
});

describe("ProjectDetailPage", () => {
  it("shows the project with its details and members", async () => {
    render(<ProjectDetailPage projectId="project-1" />);

    expect(await screen.findByRole("heading", { name: "Web sitesi" })).toBeInTheDocument();
    expect(screen.getByText("Kurumsal web sitesinin yenilenmesi")).toBeInTheDocument();
    expect(screen.getByText("ayse@abc.com")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Düzenle" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Projeyi sil" })).toBeInTheDocument();
  });

  it("hides edit and delete from a plain member", async () => {
    mocks.user = { ...TEST_USER, permissions: ["user.view"] };
    mocks.get.mockResolvedValue({ ...PROJECT_DETAIL, canManage: false, currentUserRole: "Member" });
    render(<ProjectDetailPage projectId="project-1" />);

    await screen.findByRole("heading", { name: "Web sitesi" });

    expect(screen.queryByRole("button", { name: "Düzenle" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Projeyi sil" })).not.toBeInTheDocument();
  });

  it("shows a not found message for a project the user cannot see", async () => {
    mocks.get.mockRejectedValue(new ApiError({ status: 404, code: "not_found", message: "x" }));
    render(<ProjectDetailPage projectId="other" />);

    expect(await screen.findByText("Proje bulunamadı")).toBeInTheDocument();
  });
});
