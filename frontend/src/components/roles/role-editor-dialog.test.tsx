import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RoleEditorDialog } from "@/components/roles/role-editor-dialog";
import { TEST_USER } from "@/test/fixtures";
import { EMPLOYEE_ROLE } from "@/test/roles";
import type { AuthUser, Permission, Role } from "@/types/api";

const mocks = vi.hoisted(() => ({
  create: vi.fn(),
  update: vi.fn(),
  user: null as AuthUser | null,
}));

vi.mock("@/lib/api/roles", () => ({ rolesApi: { create: mocks.create, update: mocks.update } }));
vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ user: mocks.user }) }));

const CATALOG: Permission[] = [
  { code: "user.view", group: "Users", description: "View users" },
  { code: "user.invite", group: "Users", description: "Invite new users" },
  { code: "report.view", group: "Insights", description: "View reports" },
  { code: "audit_log.view", group: "Insights", description: "View audit logs" },
];

const CUSTOM_ROLE: Role = {
  id: "role-auditor",
  name: "Auditor",
  description: "Reads the audit trail",
  isSystemRole: false,
  permissions: ["audit_log.view"],
  userCount: 0,
};

beforeEach(() => {
  mocks.create.mockReset();
  mocks.update.mockReset();
  mocks.user = TEST_USER;
});

describe("RoleEditorDialog", () => {
  it("creates a role with the chosen permissions", async () => {
    const user = userEvent.setup();
    const onSaved = vi.fn();
    mocks.create.mockResolvedValueOnce({ ...CUSTOM_ROLE, id: "new" });
    render(<RoleEditorDialog target={{ mode: "create" }} catalog={CATALOG} onClose={vi.fn()} onSaved={onSaved} />);

    await user.type(screen.getByLabelText("Rol adı"), "Raporcu");
    await user.click(screen.getByRole("checkbox", { name: "Raporları görüntüleme" }));
    await user.click(screen.getByRole("checkbox", { name: "Kullanıcıları görüntüleme" }));
    await user.click(screen.getByRole("button", { name: "Rolü oluştur" }));

    expect(mocks.create).toHaveBeenCalledWith({
      name: "Raporcu",
      description: null,
      permissions: ["report.view", "user.view"],
    });
    expect(onSaved).toHaveBeenCalled();
  });

  it("groups permissions under Turkish headings", () => {
    render(<RoleEditorDialog target={{ mode: "create" }} catalog={CATALOG} onClose={vi.fn()} onSaved={vi.fn()} />);

    expect(screen.getByRole("group", { name: "Kullanıcılar" })).toBeInTheDocument();
    expect(screen.getByRole("group", { name: "Raporlar" })).toBeInTheDocument();
  });

  it("disables permissions the editor does not have", () => {
    mocks.user = { ...TEST_USER, permissions: ["user.view", "role.manage"] };
    render(<RoleEditorDialog target={{ mode: "create" }} catalog={CATALOG} onClose={vi.fn()} onSaved={vi.fn()} />);

    expect(screen.getByRole("checkbox", { name: "Kullanıcıları görüntüleme" })).toBeEnabled();
    expect(screen.getByRole("checkbox", { name: /Denetim kayıtlarını görüntüleme/ })).toBeDisabled();
  });

  it("blocks editing a role that holds permissions the editor lacks", () => {
    mocks.user = { ...TEST_USER, permissions: ["user.view", "role.manage"] };
    render(<RoleEditorDialog target={{ mode: "edit", role: CUSTOM_ROLE }} catalog={CATALOG} onClose={vi.fn()} onSaved={vi.fn()} />);

    expect(screen.getByRole("alert")).toHaveTextContent("sizde olmayan izinler var");
    expect(screen.getByRole("button", { name: "Kaydet" })).toBeDisabled();
  });

  it("shows a system role as read only", () => {
    render(<RoleEditorDialog target={{ mode: "edit", role: EMPLOYEE_ROLE }} catalog={CATALOG} onClose={vi.fn()} onSaved={vi.fn()} />);

    expect(screen.getByText("Sistem rolleri değiştirilemez.")).toBeInTheDocument();
    expect(screen.getByRole("checkbox", { name: "Kullanıcıları görüntüleme" })).toBeChecked();
    expect(screen.getByRole("checkbox", { name: "Kullanıcıları görüntüleme" })).toBeDisabled();
    expect(screen.queryByRole("button", { name: "Kaydet" })).not.toBeInTheDocument();
  });
});
