import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RequirePermission } from "@/components/auth/require-permission";
import { TEST_USER } from "@/test/fixtures";
import type { AuthUser } from "@/types/api";

const mocks = vi.hoisted(() => ({ user: null as AuthUser | null }));

vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ user: mocks.user }) }));

beforeEach(() => {
  mocks.user = TEST_USER;
});

describe("RequirePermission", () => {
  it("renders the page for a user with the permission", () => {
    render(
      <RequirePermission permission="role.manage">
        <p>Roller</p>
      </RequirePermission>,
    );

    expect(screen.getByText("Roller")).toBeInTheDocument();
  });

  it("explains the missing permission instead of rendering the page", () => {
    mocks.user = { ...TEST_USER, permissions: ["user.view"] };

    render(
      <RequirePermission permission="role.manage">
        <p>Roller</p>
      </RequirePermission>,
    );

    expect(screen.queryByText("Roller")).not.toBeInTheDocument();
    expect(screen.getByText("Bu sayfayı görüntüleme yetkiniz yok")).toBeInTheDocument();
  });
});
