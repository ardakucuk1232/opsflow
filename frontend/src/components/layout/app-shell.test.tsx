import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AppShell } from "@/components/layout/app-shell";
import { TEST_USER } from "@/test/fixtures";
import type { AuthUser } from "@/types/api";

const mocks = vi.hoisted(() => ({ user: null as AuthUser | null, logout: vi.fn(), pathname: "/projects/p1" }));

vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ user: mocks.user, logout: mocks.logout }) }));
vi.mock("@/lib/realtime/realtime-provider", () => ({ RealtimeProvider: ({ children }: { children: ReactNode }) => children }));
vi.mock("@/components/layout/notification-bell", () => ({ NotificationBell: () => null }));
vi.mock("@/components/layout/quick-search", () => ({ QuickSearch: () => null }));
vi.mock("@/components/account/email-verification-banner", () => ({ EmailVerificationBanner: () => null }));
vi.mock("next/navigation", () => ({ usePathname: () => mocks.pathname }));
vi.mock("next/link", () => ({
  default: ({ href, children, className, onClick, ...rest }: { href: string; children: ReactNode; className?: string; onClick?: () => void }) => (
    <a href={href} className={className} onClick={onClick} {...rest}>
      {children}
    </a>
  ),
}));

const EMPLOYEE: AuthUser = { ...TEST_USER, firstName: "Ayşe", lastName: "Yılmaz", roles: ["Employee"], permissions: ["user.view", "task.update"] };

beforeEach(() => {
  mocks.user = TEST_USER;
  mocks.logout.mockReset().mockResolvedValue(undefined);
  mocks.pathname = "/projects/p1";
});

describe("AppShell", () => {
  it("shows the page with the company name and marks the current section", () => {
    render(<AppShell>sayfa içeriği</AppShell>);

    expect(screen.getByText("sayfa içeriği")).toBeInTheDocument();
    expect(screen.getByText(TEST_USER.companyName)).toBeInTheDocument();

    const nav = screen.getByRole("navigation", { name: "Ana menü" });
    expect(within(nav).getByRole("link", { name: "Projeler" })).toHaveAttribute("aria-current", "page");
    expect(within(nav).getByRole("link", { name: "Panel" })).not.toHaveAttribute("aria-current");
  });

  it("hides menu entries the user has no permission for", () => {
    mocks.user = EMPLOYEE;
    render(<AppShell>sayfa</AppShell>);

    const nav = screen.getByRole("navigation", { name: "Ana menü" });
    expect(within(nav).queryByRole("link", { name: "Roller" })).not.toBeInTheDocument();
    expect(within(nav).queryByRole("link", { name: "Raporlar" })).not.toBeInTheDocument();
    expect(within(nav).getByRole("link", { name: "Ekip" })).toBeInTheDocument();
  });

  it("opens and closes the mobile menu", async () => {
    const user = userEvent.setup();
    render(<AppShell>sayfa</AppShell>);

    await user.click(screen.getByRole("button", { name: "Menüyü aç" }));
    expect(screen.getAllByRole("navigation", { name: "Ana menü" })).toHaveLength(2);

    await user.keyboard("{Escape}");
    expect(screen.getAllByRole("navigation", { name: "Ana menü" })).toHaveLength(1);
  });

  it("signs out from the user menu", async () => {
    const user = userEvent.setup();
    mocks.user = EMPLOYEE;
    render(<AppShell>sayfa</AppShell>);

    await user.click(screen.getByRole("button", { name: /Ayşe Yılmaz/ }));
    const menu = screen.getByRole("menu", { name: "Hesap menüsü" });
    expect(menu).toHaveTextContent(EMPLOYEE.email);

    await user.click(within(menu).getByRole("menuitem", { name: "Çıkış yap" }));

    expect(mocks.logout).toHaveBeenCalled();
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
  });

  it("renders nothing without a user", () => {
    mocks.user = null;
    const { container } = render(<AppShell>sayfa</AppShell>);

    expect(container).toBeEmptyDOMElement();
  });
});
