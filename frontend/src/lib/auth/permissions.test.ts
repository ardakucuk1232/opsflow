import { describe, expect, it } from "vitest";
import { getVisibleNavigation } from "@/config/navigation";
import { hasPermission, holdsAll } from "@/lib/auth/permissions";
import { TEST_USER } from "@/test/fixtures";

const employee = { ...TEST_USER, roles: ["Employee"], permissions: ["user.view", "task.update"] };

describe("permission helpers", () => {
  it("checks a single permission", () => {
    expect(hasPermission(employee, "user.view")).toBe(true);
    expect(hasPermission(employee, "user.invite")).toBe(false);
    expect(hasPermission(null, "user.view")).toBe(false);
  });

  it("checks that every permission is held", () => {
    expect(holdsAll(employee, ["user.view", "task.update"])).toBe(true);
    expect(holdsAll(employee, ["user.view", "role.manage"])).toBe(false);
    expect(holdsAll(employee, [])).toBe(true);
  });
});

describe("getVisibleNavigation", () => {
  it("shows every page to an admin", () => {
    expect(getVisibleNavigation(TEST_USER.permissions).map((item) => item.href)).toEqual([
      "/dashboard",
      "/team",
      "/roles",
    ]);
  });

  it("hides pages the user has no permission for", () => {
    expect(getVisibleNavigation(employee.permissions).map((item) => item.href)).toEqual([
      "/dashboard",
      "/team",
    ]);
    expect(getVisibleNavigation([]).map((item) => item.href)).toEqual(["/dashboard"]);
  });
});
