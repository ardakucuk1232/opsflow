import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { EditRolesDialog } from "@/components/team/edit-roles-dialog";
import { ApiError } from "@/lib/api/errors";
import { TEST_USER } from "@/test/fixtures";
import { SYSTEM_ROLES } from "@/test/roles";
import type { UserSummary } from "@/types/api";

const mocks = vi.hoisted(() => ({ updateRoles: vi.fn() }));

vi.mock("@/lib/api/users", () => ({ usersApi: { updateRoles: mocks.updateRoles } }));
vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ user: TEST_USER }) }));

const MEMBER: UserSummary = {
  id: "u2",
  email: "ayse@abc.com",
  firstName: "Ayşe",
  lastName: "Yılmaz",
  isActive: true,
  isEmailVerified: true,
  invitationPending: false,
  roles: [{ id: "role-employee", name: "Employee" }],
  lastLoginAt: null,
  createdAt: "2026-10-01T10:00:00Z",
};

beforeEach(() => {
  mocks.updateRoles.mockReset().mockResolvedValue({ ...MEMBER, roles: [{ id: "role-manager", name: "Manager" }] });
});

describe("EditRolesDialog", () => {
  it("starts with the user's current roles and saves the new selection", async () => {
    const user = userEvent.setup();
    const onSaved = vi.fn();
    render(<EditRolesDialog user={MEMBER} roles={SYSTEM_ROLES} onClose={vi.fn()} onSaved={onSaved} />);

    expect(screen.getByRole("checkbox", { name: /^Çalışan/ })).toBeChecked();

    await user.click(screen.getByRole("checkbox", { name: /^Çalışan/ }));
    await user.click(screen.getByRole("checkbox", { name: /^Yönetici/ }));
    await user.click(screen.getByRole("button", { name: "Kaydet" }));

    expect(mocks.updateRoles).toHaveBeenCalledWith("u2", ["role-manager"]);
    expect(onSaved).toHaveBeenCalled();
  });

  it("requires at least one role", async () => {
    const user = userEvent.setup();
    render(<EditRolesDialog user={MEMBER} roles={SYSTEM_ROLES} onClose={vi.fn()} onSaved={vi.fn()} />);

    await user.click(screen.getByRole("checkbox", { name: /^Çalışan/ }));
    await user.click(screen.getByRole("button", { name: "Kaydet" }));

    expect(mocks.updateRoles).not.toHaveBeenCalled();
  });

  it("shows why the change was refused", async () => {
    const user = userEvent.setup();
    mocks.updateRoles.mockRejectedValue(new ApiError({ status: 422, code: "users.last_admin", message: "x" }));
    render(<EditRolesDialog user={MEMBER} roles={SYSTEM_ROLES} onClose={vi.fn()} onSaved={vi.fn()} />);

    await user.click(screen.getByRole("button", { name: "Kaydet" }));

    expect(await screen.findByText("Şirkette en az bir aktif Admin kalmalıdır.")).toBeInTheDocument();
  });
});
