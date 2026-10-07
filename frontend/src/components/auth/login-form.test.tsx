import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LoginForm } from "@/components/auth/login-form";
import { ApiError } from "@/lib/api/errors";

const login = vi.fn();
let search = "";

vi.mock("@/lib/auth/auth-provider", () => ({
  useAuth: () => ({ login }),
}));

vi.mock("next/navigation", () => ({
  useSearchParams: () => new URLSearchParams(search),
}));

vi.mock("next/link", () => ({
  default: ({ href, children }: { href: string; children: ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));

beforeEach(() => {
  login.mockReset();
  search = "";
});

describe("LoginForm", () => {
  it("shows validation messages and does not call the API for an empty form", async () => {
    const user = userEvent.setup();
    render(<LoginForm />);

    await user.click(screen.getByRole("button", { name: "Giriş yap" }));

    expect(await screen.findByText("E-posta adresi zorunludur.")).toBeInTheDocument();
    expect(screen.getByText("Şifre zorunludur.")).toBeInTheDocument();
    expect(login).not.toHaveBeenCalled();
  });

  it("rejects a malformed email address", async () => {
    const user = userEvent.setup();
    render(<LoginForm />);

    await user.type(screen.getByLabelText("E-posta"), "arda@");
    await user.type(screen.getByLabelText("Şifre"), "guvenli123");
    await user.click(screen.getByRole("button", { name: "Giriş yap" }));

    expect(await screen.findByText("Geçerli bir e-posta adresi girin.")).toBeInTheDocument();
    expect(login).not.toHaveBeenCalled();
  });

  it("submits the trimmed credentials", async () => {
    const user = userEvent.setup();
    login.mockResolvedValueOnce(undefined);
    render(<LoginForm />);

    await user.type(screen.getByLabelText("E-posta"), "  arda@abc.com ");
    await user.type(screen.getByLabelText("Şifre"), "guvenli123");
    await user.click(screen.getByRole("button", { name: "Giriş yap" }));

    expect(login).toHaveBeenCalledWith({ email: "arda@abc.com", password: "guvenli123" });
  });

  it("shows the translated API error when the credentials are wrong", async () => {
    const user = userEvent.setup();
    login.mockRejectedValueOnce(
      new ApiError({ status: 401, code: "auth.invalid_credentials", message: "Invalid." }),
    );
    render(<LoginForm />);

    await user.type(screen.getByLabelText("E-posta"), "arda@abc.com");
    await user.type(screen.getByLabelText("Şifre"), "yanlis-sifre");
    await user.click(screen.getByRole("button", { name: "Giriş yap" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("E-posta veya şifre hatalı.");
  });

  it("confirms a completed password reset", () => {
    search = "reset=1";
    render(<LoginForm />);

    expect(screen.getByRole("status")).toHaveTextContent("Şifreniz güncellendi.");
  });

  it("links to the password reset request page", () => {
    render(<LoginForm />);

    expect(screen.getByRole("link", { name: "Şifremi unuttum" })).toHaveAttribute(
      "href",
      "/forgot-password",
    );
  });

  it("toggles password visibility", async () => {
    const user = userEvent.setup();
    render(<LoginForm />);

    const password = screen.getByLabelText("Şifre");
    expect(password).toHaveAttribute("type", "password");

    await user.click(screen.getByRole("button", { name: "Şifreyi göster" }));

    expect(password).toHaveAttribute("type", "text");
  });
});
