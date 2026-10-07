import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { StrictMode, type ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { VerifyEmailStatus } from "@/components/account/verify-email-status";
import { ApiError } from "@/lib/api/errors";

const mocks = vi.hoisted(() => ({
  verifyEmail: vi.fn(),
  reloadUser: vi.fn(),
  status: "unauthenticated",
}));

vi.mock("@/lib/api/auth", () => ({
  authApi: { verifyEmail: mocks.verifyEmail },
}));

vi.mock("@/lib/auth/auth-provider", () => ({
  useAuth: () => ({ status: mocks.status, reloadUser: mocks.reloadUser }),
}));

vi.mock("next/link", () => ({
  default: ({ href, children }: { href: string; children: ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));

beforeEach(() => {
  mocks.verifyEmail.mockReset();
  mocks.reloadUser.mockReset().mockResolvedValue(undefined);
  mocks.status = "unauthenticated";
  window.location.hash = "#token=verify-token";
});

afterEach(() => {
  window.location.hash = "";
});

describe("VerifyEmailStatus", () => {
  it("submits the token from the URL exactly once, even under strict mode", async () => {
    mocks.verifyEmail.mockResolvedValue(undefined);

    render(
      <StrictMode>
        <VerifyEmailStatus />
      </StrictMode>,
    );

    expect(await screen.findByText("E-posta adresiniz doğrulandı")).toBeInTheDocument();
    expect(mocks.verifyEmail).toHaveBeenCalledTimes(1);
    expect(mocks.verifyEmail).toHaveBeenCalledWith("verify-token");
  });

  it("refreshes the signed-in user and links to the dashboard", async () => {
    mocks.status = "authenticated";
    mocks.verifyEmail.mockResolvedValue(undefined);
    render(<VerifyEmailStatus />);

    expect(await screen.findByRole("link", { name: "Panele git" })).toHaveAttribute(
      "href",
      "/dashboard",
    );
    expect(mocks.reloadUser).toHaveBeenCalledTimes(1);
  });

  it("links to the login page for a visitor who is not signed in", async () => {
    mocks.verifyEmail.mockResolvedValue(undefined);
    render(<VerifyEmailStatus />);

    expect(await screen.findByRole("link", { name: "Giriş yap" })).toHaveAttribute("href", "/login");
  });

  it("explains that the link cannot be used when the API rejects the token", async () => {
    mocks.verifyEmail.mockRejectedValue(
      new ApiError({ status: 422, code: "auth.invalid_token", message: "x" }),
    );
    render(<VerifyEmailStatus />);

    expect(await screen.findByRole("alert")).toHaveTextContent("geçersiz veya süresi dolmuş");
  });

  it("does not call the API when the URL carries no token", () => {
    window.location.hash = "";
    render(<VerifyEmailStatus />);

    expect(screen.getByText("Bağlantı kullanılamıyor")).toBeInTheDocument();
    expect(mocks.verifyEmail).not.toHaveBeenCalled();
  });

  it("lets the visitor retry after a network failure", async () => {
    const user = userEvent.setup();
    mocks.verifyEmail.mockRejectedValueOnce(ApiError.network()).mockResolvedValueOnce(undefined);
    render(<VerifyEmailStatus />);

    await user.click(await screen.findByRole("button", { name: "Tekrar dene" }));

    expect(await screen.findByText("E-posta adresiniz doğrulandı")).toBeInTheDocument();
    expect(mocks.verifyEmail).toHaveBeenCalledTimes(2);
  });
});
