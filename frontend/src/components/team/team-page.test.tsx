import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { TeamPage } from "@/components/team/team-page";
import { TEST_USER } from "@/test/fixtures";
import { SYSTEM_ROLES } from "@/test/roles";
import type { AuthUser, PagedResult, UserSummary } from "@/types/api";

const mocks = vi.hoisted(() => ({
  list: vi.fn(),
  deactivate: vi.fn(),
  resendInvitation: vi.fn(),
  roles: vi.fn(),
  user: null as AuthUser | null,
}));

vi.mock("@/lib/api/users", () => ({
  usersApi: {
    list: mocks.list,
    deactivate: mocks.deactivate,
    resendInvitation: mocks.resendInvitation,
  },
}));
vi.mock("@/lib/api/roles", () => ({ rolesApi: { list: mocks.roles } }));
vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ user: mocks.user }) }));

function member(overrides: Partial<UserSummary>): UserSummary {
  return {
    id: "member",
    email: "member@abc.com",
    firstName: "Ayşe",
    lastName: "Yılmaz",
    isActive: true,
    isEmailVerified: true,
    invitationPending: false,
    roles: [{ id: "role-employee", name: "Employee" }],
    lastLoginAt: null,
    createdAt: "2026-10-01T10:00:00Z",
    ...overrides,
  };
}

const USERS: UserSummary[] = [
  member({ id: TEST_USER.id, email: TEST_USER.email, firstName: "Arda", lastName: "Küçük", roles: [{ id: "role-admin", name: "Admin" }], lastLoginAt: "2026-10-09T08:30:00Z" }),
  member({ id: "invited", email: "invited@abc.com", firstName: "Can", lastName: "Demir", invitationPending: true, isEmailVerified: false }),
  member({ id: "inactive", email: "inactive@abc.com", firstName: "Deniz", lastName: "Kaya", isActive: false }),
];

function page(items: UserSummary[]): PagedResult<UserSummary> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1, hasPreviousPage: false, hasNextPage: false };
}

beforeEach(() => {
  mocks.list.mockReset().mockResolvedValue(page(USERS));
  mocks.deactivate.mockReset().mockResolvedValue(undefined);
  mocks.resendInvitation.mockReset().mockResolvedValue(undefined);
  mocks.roles.mockReset().mockResolvedValue(SYSTEM_ROLES);
  mocks.user = TEST_USER;
});

function row(name: string) {
  return screen.getByText(name, { exact: false }).closest("tr") as HTMLElement;
}

describe("TeamPage", () => {
  it("lists the users with their roles and status", async () => {
    render(<TeamPage />);

    expect(await screen.findByText("invited@abc.com")).toBeInTheDocument();
    expect(within(row("Can Demir")).getByText("Davet bekliyor")).toBeInTheDocument();
    expect(within(row("Deniz Kaya")).getByText("Pasif")).toBeInTheDocument();
    expect(within(row("Arda Küçük")).getByText("Siz")).toBeInTheDocument();
    expect(within(row("Arda Küçük")).getByText("Admin")).toBeInTheDocument();
  });

  it("offers no actions on your own row", async () => {
    render(<TeamPage />);
    await screen.findByText("invited@abc.com");

    expect(within(row("Arda Küçük")).queryByRole("button", { name: /işlemler/ })).not.toBeInTheDocument();
    expect(within(row("Can Demir")).getByRole("button", { name: /işlemler/ })).toBeInTheDocument();
  });

  it("asks the API for the selected status", async () => {
    const user = userEvent.setup();
    render(<TeamPage />);
    await screen.findByText("invited@abc.com");

    await user.selectOptions(screen.getByLabelText("Duruma göre filtrele"), "Invited");

    await vi.waitFor(() =>
      expect(mocks.list).toHaveBeenLastCalledWith(
        expect.objectContaining({ page: 1, status: "Invited" }),
        expect.any(AbortSignal),
      ),
    );
  });

  it("deactivates a user after confirmation and reloads the list", async () => {
    const user = userEvent.setup();
    render(<TeamPage />);
    await screen.findByText("invited@abc.com");

    await user.click(within(row("Can Demir")).getByRole("button", { name: /işlemler/ }));
    await user.click(screen.getByRole("menuitem", { name: "Pasifleştir" }));
    await user.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Pasifleştir" }));

    expect(mocks.deactivate).toHaveBeenCalledWith("invited");
    expect(await screen.findByText("Can Demir pasif duruma alındı.")).toBeInTheDocument();
    expect(mocks.list.mock.calls.length).toBeGreaterThanOrEqual(2);
  });

  it("resends a pending invitation", async () => {
    const user = userEvent.setup();
    render(<TeamPage />);
    await screen.findByText("invited@abc.com");

    await user.click(within(row("Can Demir")).getByRole("button", { name: /işlemler/ }));
    await user.click(screen.getByRole("menuitem", { name: "Daveti tekrar gönder" }));

    expect(mocks.resendInvitation).toHaveBeenCalledWith("invited");
    expect(await screen.findByText("invited@abc.com adresine davet tekrar gönderildi.")).toBeInTheDocument();
  });

  it("hides management actions from a user who can only view the team", async () => {
    mocks.user = { ...TEST_USER, permissions: ["user.view"] };
    render(<TeamPage />);
    await screen.findByText("invited@abc.com");

    expect(screen.queryByRole("button", { name: "Kullanıcı davet et" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /işlemler/ })).not.toBeInTheDocument();
  });
});
