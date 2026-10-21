import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { GuestOnly } from "@/components/auth/guest-only";
import { RequireAuth } from "@/components/auth/require-auth";

const mocks = vi.hoisted(() => ({
  status: "loading" as string,
  retry: vi.fn(),
  replace: vi.fn(),
  search: "",
}));

vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ status: mocks.status, retry: mocks.retry }) }));
vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: mocks.replace }),
  usePathname: () => "/projects/p1",
  useSearchParams: () => new URLSearchParams(mocks.search),
}));

beforeEach(() => {
  mocks.status = "loading";
  mocks.retry.mockReset().mockResolvedValue(undefined);
  mocks.replace.mockReset();
  mocks.search = "";
});

describe("RequireAuth", () => {
  it("renders the page for a signed in user", () => {
    mocks.status = "authenticated";
    render(<RequireAuth>gizli içerik</RequireAuth>);

    expect(screen.getByText("gizli içerik")).toBeInTheDocument();
    expect(mocks.replace).not.toHaveBeenCalled();
  });

  it("sends a signed out user to the login page and remembers where they were", () => {
    mocks.status = "unauthenticated";
    render(<RequireAuth>gizli içerik</RequireAuth>);

    expect(screen.queryByText("gizli içerik")).not.toBeInTheDocument();
    expect(mocks.replace).toHaveBeenCalledWith("/login?next=%2Fprojects%2Fp1");
  });

  it("offers a retry when the server cannot be reached", async () => {
    const user = userEvent.setup();
    mocks.status = "unavailable";
    render(<RequireAuth>gizli içerik</RequireAuth>);

    await user.click(screen.getByRole("button", { name: "Tekrar dene" }));

    expect(screen.getByText("Sunucuya ulaşılamıyor")).toBeInTheDocument();
    expect(mocks.retry).toHaveBeenCalled();
  });

  it("waits while the session loads", () => {
    render(<RequireAuth>gizli içerik</RequireAuth>);

    expect(screen.queryByText("gizli içerik")).not.toBeInTheDocument();
    expect(mocks.replace).not.toHaveBeenCalled();
  });
});

describe("GuestOnly", () => {
  it("shows the form to a signed out visitor", () => {
    mocks.status = "unauthenticated";
    render(<GuestOnly>giriş formu</GuestOnly>);

    expect(screen.getByText("giriş formu")).toBeInTheDocument();
  });

  it("sends a signed in user to the requested page", () => {
    mocks.status = "authenticated";
    mocks.search = "next=%2Ftasks";
    render(<GuestOnly>giriş formu</GuestOnly>);

    expect(mocks.replace).toHaveBeenCalledWith("/tasks");
  });

  it("ignores a redirect to another site", () => {
    mocks.status = "authenticated";
    mocks.search = "next=https%3A%2F%2Fevil.example";
    render(<GuestOnly>giriş formu</GuestOnly>);

    expect(mocks.replace).toHaveBeenCalledWith("/dashboard");
  });
});
