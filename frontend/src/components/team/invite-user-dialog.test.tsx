import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { InviteUserDialog } from "@/components/team/invite-user-dialog";
import { ApiError } from "@/lib/api/errors";
import { TEST_USER } from "@/test/fixtures";
import { SYSTEM_ROLES } from "@/test/roles";
import type { AuthUser } from "@/types/api";

const mocks = vi.hoisted(() => ({
  invite: vi.fn(),
  user: null as AuthUser | null,
}));

vi.mock("@/lib/api/users", () => ({ usersApi: { invite: mocks.invite } }));
vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ user: mocks.user }) }));

beforeEach(() => {
  mocks.invite.mockReset();
  mocks.user = TEST_USER;
});

function renderDialog(onInvited = vi.fn()) {
  render(<InviteUserDialog open roles={SYSTEM_ROLES} onClose={vi.fn()} onInvited={onInvited} />);

  return onInvited;
}

async function fillForm(email = "ayse@abc.com") {
  const user = userEvent.setup();

  await user.type(screen.getByLabelText("Ad"), "Ayşe");
  await user.type(screen.getByLabelText("Soyad"), "Yılmaz");
  await user.type(screen.getByLabelText("E-posta"), email);

  return user;
}

describe("InviteUserDialog", () => {
  it("selects the employee role by default", () => {
    renderDialog();

    expect(screen.getByRole("checkbox", { name: /Çalışan/ })).toBeChecked();
    expect(screen.getByRole("checkbox", { name: /Admin/ })).not.toBeChecked();
  });

  it("sends the invitation with the selected roles", async () => {
    mocks.invite.mockResolvedValueOnce({ id: "new-user", email: "ayse@abc.com" });
    const onInvited = renderDialog();

    const user = await fillForm();
    await user.click(screen.getByRole("checkbox", { name: /Yönetici/ }));
    await user.click(screen.getByRole("button", { name: "Davet gönder" }));

    expect(mocks.invite).toHaveBeenCalledWith({
      firstName: "Ayşe",
      lastName: "Yılmaz",
      email: "ayse@abc.com",
      roleIds: ["role-employee", "role-manager"],
    });
    expect(onInvited).toHaveBeenCalledWith({ id: "new-user", email: "ayse@abc.com" });
  });

  it("requires at least one role", async () => {
    renderDialog();

    const user = await fillForm();
    await user.click(screen.getByRole("checkbox", { name: /Çalışan/ }));
    await user.click(screen.getByRole("button", { name: "Davet gönder" }));

    expect(await screen.findByText("En az bir rol seçin.")).toBeInTheDocument();
    expect(mocks.invite).not.toHaveBeenCalled();
  });

  it("disables roles that hold permissions the inviter does not have", () => {
    mocks.user = { ...TEST_USER, permissions: ["user.view", "user.invite", "task.update", "comment.create", "attachment.upload"] };
    renderDialog();

    expect(screen.getByRole("checkbox", { name: /Admin/ })).toBeDisabled();
    expect(screen.getByRole("checkbox", { name: /Çalışan/ })).toBeEnabled();
  });

  it("shows a taken email address on the email field", async () => {
    mocks.invite.mockRejectedValueOnce(
      new ApiError({ status: 409, code: "auth.email_already_in_use", message: "x" }),
    );
    renderDialog();

    const user = await fillForm();
    await user.click(screen.getByRole("button", { name: "Davet gönder" }));

    expect(await screen.findByText("Bu e-posta adresiyle kayıtlı bir hesap zaten var.")).toBeInTheDocument();
  });

  it("explains why an unverified inviter cannot invite", async () => {
    mocks.invite.mockRejectedValueOnce(
      new ApiError({ status: 422, code: "users.email_not_verified", message: "x" }),
    );
    renderDialog();

    const user = await fillForm();
    await user.click(screen.getByRole("button", { name: "Davet gönder" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("e-posta adresinizi doğrulayın");
  });
});
