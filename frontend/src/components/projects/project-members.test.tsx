import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ProjectMembers } from "@/components/projects/project-members";
import { TEST_USER } from "@/test/fixtures";
import { PROJECT_DETAIL } from "@/test/projects";

const mocks = vi.hoisted(() => ({ updateMember: vi.fn(), removeMember: vi.fn() }));

vi.mock("@/lib/api/projects", () => ({
  projectsApi: { updateMember: mocks.updateMember, removeMember: mocks.removeMember },
}));
vi.mock("@/lib/api/users", () => ({ usersApi: { list: vi.fn() } }));
vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ user: TEST_USER }) }));

beforeEach(() => {
  mocks.updateMember.mockReset().mockResolvedValue(PROJECT_DETAIL);
  mocks.removeMember.mockReset().mockResolvedValue(undefined);
});

describe("ProjectMembers", () => {
  it("shows roles as text to someone who cannot manage the project", () => {
    render(
      <ProjectMembers project={{ ...PROJECT_DETAIL, canManage: false }} onChanged={vi.fn()} onLeft={vi.fn()} />,
    );

    expect(screen.queryByRole("button", { name: "Üye ekle" })).not.toBeInTheDocument();
    expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
    expect(screen.getByText("Proje lideri")).toBeInTheDocument();
  });

  it("changes the role of a member", async () => {
    const user = userEvent.setup();
    const onChanged = vi.fn();
    render(<ProjectMembers project={PROJECT_DETAIL} onChanged={onChanged} onLeft={vi.fn()} />);

    await user.selectOptions(screen.getByLabelText("Ayşe Yılmaz projedeki rolü"), "Lead");

    expect(mocks.updateMember).toHaveBeenCalledWith("project-1", "member-2", "Lead");
    expect(onChanged).toHaveBeenCalled();
  });

  it("leaves the page after removing yourself", async () => {
    const user = userEvent.setup();
    const onLeft = vi.fn();
    render(<ProjectMembers project={PROJECT_DETAIL} onChanged={vi.fn()} onLeft={onLeft} />);

    await user.click(screen.getByRole("button", { name: "Arda Küçük projeden çıkar" }));
    await user.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Çıkar" }));

    expect(mocks.removeMember).toHaveBeenCalledWith("project-1", TEST_USER.id);
    expect(onLeft).toHaveBeenCalled();
  });

  it("explains why the last lead cannot step down", async () => {
    const user = userEvent.setup();
    const { ApiError } = await import("@/lib/api/errors");
    mocks.updateMember.mockRejectedValueOnce(new ApiError({ status: 422, code: "projects.last_lead", message: "x" }));
    render(<ProjectMembers project={PROJECT_DETAIL} onChanged={vi.fn()} onLeft={vi.fn()} />);

    await user.selectOptions(screen.getByLabelText("Arda Küçük projedeki rolü"), "Member");

    expect(await screen.findByRole("alert")).toHaveTextContent("en az bir aktif proje lideri");
  });
});
