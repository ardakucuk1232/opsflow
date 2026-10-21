import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RegisterForm } from "@/components/auth/register-form";
import { ApiError } from "@/lib/api/errors";

const mocks = vi.hoisted(() => ({ register: vi.fn() }));

vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ register: mocks.register }) }));
vi.mock("next/link", () => ({
  default: ({ href, children }: { href: string; children: ReactNode }) => <a href={href}>{children}</a>,
}));

beforeEach(() => {
  mocks.register.mockReset().mockResolvedValue(undefined);
});

async function fill(user: ReturnType<typeof userEvent.setup>, password = "Guvenli-sifre-1", confirmation = password) {
  await user.type(screen.getByLabelText("Şirket adı"), "ABC Yazılım");
  await user.type(screen.getByLabelText("Ad"), "Arda");
  await user.type(screen.getByLabelText("Soyad"), "Küçük");
  await user.type(screen.getByLabelText("E-posta"), "arda@abc.com");
  await user.type(screen.getByLabelText("Şifre", { exact: true }), password);
  await user.type(screen.getByLabelText("Şifre (tekrar)"), confirmation);
}

describe("RegisterForm", () => {
  it("creates the account with the entered details", async () => {
    const user = userEvent.setup();
    render(<RegisterForm />);

    await fill(user);
    await user.click(screen.getByRole("button", { name: "Hesabı oluştur" }));

    expect(mocks.register).toHaveBeenCalledWith({
      companyName: "ABC Yazılım",
      firstName: "Arda",
      lastName: "Küçük",
      email: "arda@abc.com",
      password: "Guvenli-sifre-1",
    });
  });

  it("does not submit when the passwords differ", async () => {
    const user = userEvent.setup();
    render(<RegisterForm />);

    await fill(user, "Guvenli-sifre-1", "Baska-sifre-2");
    await user.click(screen.getByRole("button", { name: "Hesabı oluştur" }));

    expect(mocks.register).not.toHaveBeenCalled();
    expect(screen.getByLabelText("Şifre (tekrar)")).toHaveAttribute("aria-invalid", "true");
  });

  it("marks the e-mail field when the address is already registered", async () => {
    const user = userEvent.setup();
    mocks.register.mockRejectedValue(new ApiError({ status: 409, code: "auth.email_already_in_use", message: "x" }));
    render(<RegisterForm />);

    await fill(user);
    await user.click(screen.getByRole("button", { name: "Hesabı oluştur" }));

    expect(await screen.findByText("Bu e-posta adresiyle kayıtlı bir hesap zaten var.")).toBeInTheDocument();
    expect(screen.getByLabelText("E-posta")).toHaveAttribute("aria-invalid", "true");
  });

  it("shows a general error when the server is unreachable", async () => {
    const user = userEvent.setup();
    mocks.register.mockRejectedValue(ApiError.network());
    render(<RegisterForm />);

    await fill(user);
    await user.click(screen.getByRole("button", { name: "Hesabı oluştur" }));

    expect(await screen.findByText(/Sunucuya ulaşılamıyor/)).toBeInTheDocument();
  });
});
