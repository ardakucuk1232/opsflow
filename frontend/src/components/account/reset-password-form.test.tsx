import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ResetPasswordForm } from "@/components/account/reset-password-form";
import { ApiError } from "@/lib/api/errors";

const mocks = vi.hoisted(() => ({
  resetPassword: vi.fn(),
  logout: vi.fn(),
  replace: vi.fn(),
}));

vi.mock("@/lib/api/auth", () => ({
  authApi: { resetPassword: mocks.resetPassword },
}));

vi.mock("@/lib/auth/auth-provider", () => ({
  useAuth: () => ({ logout: mocks.logout }),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: mocks.replace }),
}));

vi.mock("next/link", () => ({
  default: ({ href, children }: { href: string; children: ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));

beforeEach(() => {
  mocks.resetPassword.mockReset();
  mocks.logout.mockReset().mockResolvedValue(undefined);
  mocks.replace.mockReset();
  window.location.hash = "#token=reset-token";
});

afterEach(() => {
  window.location.hash = "";
});

async function fillPasswords(password: string, confirmation: string) {
  const user = userEvent.setup();

  await user.type(screen.getByLabelText("Yeni şifre"), password);
  await user.type(screen.getByLabelText("Yeni şifre (tekrar)"), confirmation);
  await user.click(screen.getByRole("button", { name: "Şifreyi güncelle" }));
}

describe("ResetPasswordForm", () => {
  it("offers a new link when the URL carries no token", () => {
    window.location.hash = "";
    render(<ResetPasswordForm />);

    expect(screen.getByRole("alert")).toHaveTextContent("geçersiz veya süresi dolmuş");
    expect(screen.getByRole("link", { name: "Yeni bağlantı iste" })).toHaveAttribute(
      "href",
      "/forgot-password",
    );
  });

  it("rejects passwords that do not match", async () => {
    render(<ResetPasswordForm />);

    await fillPasswords("guvenli123", "guvenli124");

    expect(await screen.findByText("Şifreler birbiriyle eşleşmiyor.")).toBeInTheDocument();
    expect(mocks.resetPassword).not.toHaveBeenCalled();
  });

  it("rejects a password that is too weak", async () => {
    render(<ResetPasswordForm />);

    await fillPasswords("sadeceharf", "sadeceharf");

    expect(await screen.findByText("Şifre en az bir rakam içermelidir.")).toBeInTheDocument();
    expect(mocks.resetPassword).not.toHaveBeenCalled();
  });

  it("sends the token from the URL, signs out and returns to the login page", async () => {
    mocks.resetPassword.mockResolvedValueOnce(undefined);
    render(<ResetPasswordForm />);

    await fillPasswords("guvenli123", "guvenli123");

    await vi.waitFor(() => expect(mocks.replace).toHaveBeenCalledWith("/login?reset=1"));
    expect(mocks.resetPassword).toHaveBeenCalledWith({
      token: "reset-token",
      newPassword: "guvenli123",
    });
    expect(mocks.logout).toHaveBeenCalledTimes(1);
  });

  it("offers a new link when the API rejects the token", async () => {
    mocks.resetPassword.mockRejectedValueOnce(
      new ApiError({ status: 422, code: "auth.invalid_token", message: "x" }),
    );
    render(<ResetPasswordForm />);

    await fillPasswords("guvenli123", "guvenli123");

    expect(await screen.findByRole("link", { name: "Yeni bağlantı iste" })).toBeInTheDocument();
    expect(mocks.replace).not.toHaveBeenCalled();
    expect(mocks.logout).not.toHaveBeenCalled();
  });

  it("keeps the form open when the server cannot be reached", async () => {
    mocks.resetPassword.mockRejectedValueOnce(ApiError.network());
    render(<ResetPasswordForm />);

    await fillPasswords("guvenli123", "guvenli123");

    expect(await screen.findByRole("alert")).toHaveTextContent("Sunucuya ulaşılamıyor");
    expect(screen.getByRole("button", { name: "Şifreyi güncelle" })).toBeInTheDocument();
  });
});
