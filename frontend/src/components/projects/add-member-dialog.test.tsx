import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AddMemberDialog } from "@/components/projects/add-member-dialog";
import { ApiError } from "@/lib/api/errors";
import { TEST_USER } from "@/test/fixtures";
import { PROJECT_DETAIL } from "@/test/projects";
import type { UserSummary } from "@/types/api";

const mocks = vi.hoisted(() => ({ users: vi.fn(), addMember: vi.fn() }));

vi.mock("@/lib/api/users", () => ({ usersApi: { list: mocks.users } }));
vi.mock("@/lib/api/projects", () => ({ projectsApi: { addMember: mocks.addMember } }));

function user(id: string, firstName: string): UserSummary {
  return {
    id,
    email: `${id}@abc.com`,
    firstName,
    lastName: "Test",
    isActive: true,
    isEmailVerified: true,
    invitationPending: false,
    roles: [],
    lastLoginAt: null,
    createdAt: "2026-10-01T10:00:00Z",
  };
}

function page(items: UserSummary[]) {
  return { items, page: 1, pageSize: 100, totalCount: items.length, totalPages: 1, hasPreviousPage: false, hasNextPage: false };
}

beforeEach(() => {
  mocks.users.mockReset().mockResolvedValue(page([user(TEST_USER.id, "Arda"), user("u2", "Ayşe")]));
  mocks.addMember.mockReset().mockResolvedValue(PROJECT_DETAIL);
});

function renderDialog(onAdded = vi.fn()) {
  render(<AddMemberDialog open project={PROJECT_DETAIL} onClose={vi.fn()} onAdded={onAdded} />);

  return onAdded;
}

describe("AddMemberDialog", () => {
  it("offers only active users who are not members yet", async () => {
    renderDialog();

    const select = await screen.findByLabelText("Kişi");

    expect(select).toHaveTextContent("Ayşe Test");
    expect(select).not.toHaveTextContent("Arda Test");
    expect(mocks.users).toHaveBeenCalledWith({ page: 1, pageSize: 100, status: "Active" }, expect.any(AbortSignal));
  });

  it("adds the chosen person with the chosen role", async () => {
    const actor = userEvent.setup();
    const onAdded = renderDialog();

    await actor.selectOptions(await screen.findByLabelText("Kişi"), "u2");
    await actor.selectOptions(screen.getByLabelText("Projedeki rolü"), "Lead");
    await actor.click(screen.getByRole("button", { name: "Ekle" }));

    expect(mocks.addMember).toHaveBeenCalledWith(PROJECT_DETAIL.id, "u2", "Lead");
    expect(onAdded).toHaveBeenCalledWith(PROJECT_DETAIL);
  });

  it("asks for a person before submitting", async () => {
    const actor = userEvent.setup();
    renderDialog();

    await actor.click(await screen.findByRole("button", { name: "Ekle" }));

    expect(screen.getByText("Eklemek istediğiniz kişiyi seçin.")).toBeInTheDocument();
    expect(mocks.addMember).not.toHaveBeenCalled();
  });

  it("shows the server's reason when adding fails", async () => {
    const actor = userEvent.setup();
    mocks.addMember.mockRejectedValue(new ApiError({ status: 409, code: "projects.member_exists", message: "x" }));
    renderDialog();

    await actor.selectOptions(await screen.findByLabelText("Kişi"), "u2");
    await actor.click(screen.getByRole("button", { name: "Ekle" }));

    expect(await screen.findByText("Bu kullanıcı zaten projenin üyesi.")).toBeInTheDocument();
  });

  it("explains when everyone is already a member", async () => {
    mocks.users.mockResolvedValue(page([user(TEST_USER.id, "Arda")]));
    renderDialog();

    expect(await screen.findByText(/bütün aktif kullanıcılar zaten bu projede/)).toBeInTheDocument();
  });
});
