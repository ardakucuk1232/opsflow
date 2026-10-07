import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ForgotPasswordForm } from "@/components/auth/forgot-password-form";
import { ApiError } from "@/lib/api/errors";

const forgotPassword = vi.hoisted(() => vi.fn());

vi.mock("@/lib/api/auth", () => ({
  authApi: { forgotPassword },
}));

vi.mock("next/link", () => ({
  default: ({ href, children }: { href: string; children: ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));

beforeEach(() => {
  forgotPassword.mockReset();
});

describe("ForgotPasswordForm", () => {
  it("does not call the API for a malformed email address", async () => {
    const user = userEvent.setup();
    render(<ForgotPasswordForm />);

    await user.type(screen.getByLabelText("E-posta"), "arda@");
    await user.click(screen.getByRole("button", { name: "Bağlantı gönder" }));

    expect(await screen.findByText("Geçerli bir e-posta adresi girin.")).toBeInTheDocument();
    expect(forgotPassword).not.toHaveBeenCalled();
  });

  it("shows the same confirmation whether or not the account exists", async () => {
    const user = userEvent.setup();
    forgotPassword.mockResolvedValueOnce(undefined);
    render(<ForgotPasswordForm />);

    await user.type(screen.getByLabelText("E-posta"), " arda@abc.com ");
    await user.click(screen.getByRole("button", { name: "Bağlantı gönder" }));

    expect(await screen.findByRole("status")).toHaveTextContent(
      "Bu adresle kayıtlı bir hesap varsa şifre sıfırlama bağlantısını gönderdik.",
    );
    expect(forgotPassword).toHaveBeenCalledWith("arda@abc.com");
    expect(screen.queryByRole("button", { name: "Bağlantı gönder" })).not.toBeInTheDocument();
  });

  it("shows the wait time when the request is rate limited", async () => {
    const user = userEvent.setup();
    forgotPassword.mockRejectedValueOnce(
      new ApiError({ status: 429, code: "too_many_requests", message: "x", retryAfterSeconds: 30 }),
    );
    render(<ForgotPasswordForm />);

    await user.type(screen.getByLabelText("E-posta"), "arda@abc.com");
    await user.click(screen.getByRole("button", { name: "Bağlantı gönder" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("30 saniye");
  });
});
