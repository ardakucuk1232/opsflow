import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { EmailVerificationBanner } from "@/components/account/email-verification-banner";
import { ApiError } from "@/lib/api/errors";
import { TEST_USER } from "@/test/fixtures";
import type { AuthUser } from "@/types/api";

const mocks = vi.hoisted(() => ({
  resendVerification: vi.fn(),
  reloadUser: vi.fn(),
  user: null as AuthUser | null,
}));

vi.mock("@/lib/api/auth", () => ({
  authApi: { resendVerification: mocks.resendVerification },
}));

vi.mock("@/lib/auth/auth-provider", () => ({
  useAuth: () => ({ user: mocks.user, reloadUser: mocks.reloadUser }),
}));

beforeEach(() => {
  mocks.resendVerification.mockReset();
  mocks.reloadUser.mockReset().mockResolvedValue(undefined);
  mocks.user = { ...TEST_USER, isEmailVerified: false };
});

describe("EmailVerificationBanner", () => {
  it("is hidden once the email address is verified", () => {
    mocks.user = { ...TEST_USER, isEmailVerified: true };

    const { container } = render(<EmailVerificationBanner />);

    expect(container).toBeEmptyDOMElement();
  });

  it("tells an unverified user which address to check", () => {
    render(<EmailVerificationBanner />);

    expect(screen.getByRole("status")).toHaveTextContent("arda@abc.com");
  });

  it("requests a new verification email", async () => {
    const user = userEvent.setup();
    mocks.resendVerification.mockResolvedValueOnce(undefined);
    render(<EmailVerificationBanner />);

    await user.click(screen.getByRole("button", { name: "Tekrar gönder" }));

    expect(await screen.findByText(/Gelen kutunuzu kontrol edin/)).toBeInTheDocument();
    expect(mocks.resendVerification).toHaveBeenCalledTimes(1);
  });

  it("shows why a new email could not be requested", async () => {
    const user = userEvent.setup();
    mocks.resendVerification.mockRejectedValueOnce(ApiError.network());
    render(<EmailVerificationBanner />);

    await user.click(screen.getByRole("button", { name: "Tekrar gönder" }));

    expect(await screen.findByText(/Sunucuya ulaşılamıyor/)).toBeInTheDocument();
  });

  it("checks the verification state again when the window regains focus", () => {
    render(<EmailVerificationBanner />);

    fireEvent.focus(window);

    expect(mocks.reloadUser).toHaveBeenCalledTimes(1);
  });
});
