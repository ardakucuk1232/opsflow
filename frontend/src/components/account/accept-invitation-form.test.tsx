import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AcceptInvitationForm } from "@/components/account/accept-invitation-form";
import { ApiError } from "@/lib/api/errors";

const mocks = vi.hoisted(() => ({
  previewInvitation: vi.fn(),
  acceptInvitation: vi.fn(),
  replace: vi.fn(),
}));

vi.mock("@/lib/api/auth", () => ({ authApi: { previewInvitation: mocks.previewInvitation } }));
vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ acceptInvitation: mocks.acceptInvitation }) }));
vi.mock("next/navigation", () => ({ useRouter: () => ({ replace: mocks.replace }) }));

const PREVIEW = {
  email: "ayse@abc.com",
  firstName: "Ayşe",
  lastName: "Yılmaz",
  companyName: "ABC Yazılım A.Ş.",
};

beforeEach(() => {
  mocks.previewInvitation.mockReset().mockResolvedValue(PREVIEW);
  mocks.acceptInvitation.mockReset().mockResolvedValue(undefined);
  mocks.replace.mockReset();
  window.location.hash = "#token=invite-token";
});

afterEach(() => {
  window.location.hash = "";
});

describe("AcceptInvitationForm", () => {
  it("shows which company sent the invitation", async () => {
    render(<AcceptInvitationForm />);

    expect(await screen.findByText("ABC Yazılım A.Ş.")).toBeInTheDocument();
    expect(screen.getByLabelText("E-posta")).toHaveValue("ayse@abc.com");
    expect(mocks.previewInvitation).toHaveBeenCalledWith("invite-token", expect.any(AbortSignal));
  });

  it("accepts the invitation and opens the dashboard", async () => {
    const user = userEvent.setup();
    render(<AcceptInvitationForm />);
    await screen.findByText("ABC Yazılım A.Ş.");

    await user.type(screen.getByLabelText("Şifre"), "guvenli123");
    await user.type(screen.getByLabelText("Şifre (tekrar)"), "guvenli123");
    await user.click(screen.getByRole("button", { name: "Hesabımı oluştur" }));

    expect(mocks.acceptInvitation).toHaveBeenCalledWith({ token: "invite-token", password: "guvenli123" });
    await vi.waitFor(() => expect(mocks.replace).toHaveBeenCalledWith("/dashboard"));
  });

  it("explains an invitation that can no longer be used", async () => {
    mocks.previewInvitation.mockRejectedValue(
      new ApiError({ status: 422, code: "auth.invalid_token", message: "x" }),
    );
    render(<AcceptInvitationForm />);

    expect(await screen.findByText("Davet kullanılamıyor")).toBeInTheDocument();
  });

  it("does not call the API without a token", () => {
    window.location.hash = "";
    render(<AcceptInvitationForm />);

    expect(screen.getByText("Davet kullanılamıyor")).toBeInTheDocument();
    expect(mocks.previewInvitation).not.toHaveBeenCalled();
  });
});
