import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RolesPage } from "@/components/roles/roles-page";
import { ApiError } from "@/lib/api/errors";
import { SYSTEM_ROLES } from "@/test/roles";
import type { Role } from "@/types/api";

const mocks = vi.hoisted(() => ({ list: vi.fn(), permissions: vi.fn(), remove: vi.fn() }));

vi.mock("@/lib/api/roles", () => ({ rolesApi: { list: mocks.list, permissions: mocks.permissions, remove: mocks.remove } }));

const CUSTOM: Role = {
  id: "role-auditor",
  name: "Denetçi",
  description: "Raporları inceler",
  isSystemRole: false,
  permissions: ["report.view", "user.view"],
  userCount: 0,
};

beforeEach(() => {
  mocks.list.mockReset().mockResolvedValue([...SYSTEM_ROLES, CUSTOM]);
  mocks.permissions.mockReset().mockResolvedValue([
    { code: "report.view", group: "Insights", description: "View reports" },
    { code: "user.view", group: "Users", description: "View users" },
  ]);
  mocks.remove.mockReset().mockResolvedValue(undefined);
});

describe("RolesPage", () => {
  it("lists system and custom roles with their counts", async () => {
    render(<RolesPage />);

    const custom = (await screen.findByText("Denetçi")).closest("li") as HTMLElement;

    expect(custom).toHaveTextContent("Raporları inceler");
    expect(custom).toHaveTextContent("2 izin");
    expect(screen.getByText("Yönetici").closest("li")).toHaveTextContent("Sistem rolü");
  });

  it("only offers edit and delete actions on custom roles", async () => {
    render(<RolesPage />);
    await screen.findByText("Denetçi");

    expect(screen.getByRole("button", { name: "Denetçi rolü için işlemler" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Admin rolü için işlemler" })).not.toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: "İzinleri gör" })).toHaveLength(3);
  });

  it("deletes a custom role after confirmation", async () => {
    const user = userEvent.setup();
    render(<RolesPage />);
    await screen.findByText("Denetçi");

    await user.click(screen.getByRole("button", { name: "Denetçi rolü için işlemler" }));
    await user.click(screen.getByRole("menuitem", { name: "Sil" }));
    const dialog = screen.getByRole("dialog", { name: "Rolü sil" });
    await user.click(within(dialog).getByRole("button", { name: "Sil" }));

    expect(mocks.remove).toHaveBeenCalledWith("role-auditor");
    expect(await screen.findByText("Denetçi rolü silindi.")).toBeInTheDocument();
  });

  it("explains why a role in use cannot be deleted", async () => {
    const user = userEvent.setup();
    mocks.remove.mockRejectedValue(new ApiError({ status: 409, code: "roles.in_use", message: "x" }));
    render(<RolesPage />);
    await screen.findByText("Denetçi");

    await user.click(screen.getByRole("button", { name: "Denetçi rolü için işlemler" }));
    await user.click(screen.getByRole("menuitem", { name: "Sil" }));
    await user.click(within(screen.getByRole("dialog", { name: "Rolü sil" })).getByRole("button", { name: "Sil" }));

    expect(await screen.findByText(/Bu rol kullanıcılara atanmış/)).toBeInTheDocument();
  });

  it("shows an error with a retry when loading fails", async () => {
    const user = userEvent.setup();
    mocks.list.mockRejectedValueOnce(new ApiError({ status: 500, code: "internal_error", message: "x" }));
    render(<RolesPage />);

    await user.click(await screen.findByRole("button", { name: "Tekrar dene" }));

    await waitFor(() => expect(screen.getByText("Denetçi")).toBeInTheDocument());
  });
});
