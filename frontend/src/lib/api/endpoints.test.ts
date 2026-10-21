import { beforeEach, describe, expect, it, vi } from "vitest";
import { attachmentsApi } from "@/lib/api/attachments";
import { auditLogsApi } from "@/lib/api/audit-logs";
import { authApi } from "@/lib/api/auth";
import { notificationsApi } from "@/lib/api/notifications";
import { projectsApi } from "@/lib/api/projects";
import { reportsApi } from "@/lib/api/reports";
import { rolesApi } from "@/lib/api/roles";
import { searchApi } from "@/lib/api/search";
import { tasksApi } from "@/lib/api/tasks";
import { usersApi } from "@/lib/api/users";

const mocks = vi.hoisted(() => ({ api: vi.fn(), send: vi.fn() }));

vi.mock("@/lib/api/client", () => ({ api: mocks.api }));
vi.mock("@/lib/api/http", () => ({ send: mocks.send }));

beforeEach(() => {
  mocks.api.mockReset().mockResolvedValue(undefined);
  mocks.send.mockReset().mockResolvedValue(undefined);
});

type Case = [name: string, call: () => Promise<unknown>, path: string, options?: Record<string, unknown>];

function expectCall(mock: typeof mocks.api, path: string, options?: Record<string, unknown>) {
  const [actualPath, actualOptions] = mock.mock.calls[0];

  expect(actualPath).toBe(path);

  if (options) {
    expect(actualOptions).toMatchObject(options);
  }
}

describe("authenticated endpoints", () => {
  const file = new File(["x"], "a.txt");
  const project = { name: "Web", description: null, status: "Active" as const, startDate: null, endDate: null };

  const cases: Case[] = [
    ["projects.list", () => projectsApi.list({ page: 2, pageSize: 10, search: "web", status: "Active", memberOnly: true }), "/api/projects?page=2&pageSize=10&search=web&status=Active&memberOnly=true"],
    ["projects.get", () => projectsApi.get("p 1"), "/api/projects/p%201"],
    ["projects.create", () => projectsApi.create({ ...project, key: "WEB" }), "/api/projects", { method: "POST" }],
    ["projects.update", () => projectsApi.update("p1", project), "/api/projects/p1", { method: "PUT", body: project }],
    ["projects.remove", () => projectsApi.remove("p1"), "/api/projects/p1", { method: "DELETE" }],
    ["projects.addMember", () => projectsApi.addMember("p1", "u1", "Lead"), "/api/projects/p1/members", { method: "POST", body: { userId: "u1", role: "Lead" } }],
    ["projects.updateMember", () => projectsApi.updateMember("p1", "u1", "Member"), "/api/projects/p1/members/u1", { method: "PUT", body: { role: "Member" } }],
    ["projects.removeMember", () => projectsApi.removeMember("p1", "u1"), "/api/projects/p1/members/u1", { method: "DELETE" }],
    ["users.list", () => usersApi.list({ page: 1, pageSize: 20, search: "ar", status: "Invited", roleId: "r1" }), "/api/users?page=1&pageSize=20&search=ar&status=Invited&roleId=r1"],
    ["users.invite", () => usersApi.invite({ email: "a@b.c", firstName: "A", lastName: "B", roleIds: ["r1"] }), "/api/users/invitations", { method: "POST" }],
    ["users.resendInvitation", () => usersApi.resendInvitation("u1"), "/api/users/u1/resend-invitation", { method: "POST" }],
    ["users.updateRoles", () => usersApi.updateRoles("u1", ["r1"]), "/api/users/u1/roles", { method: "PUT", body: { roleIds: ["r1"] } }],
    ["users.deactivate", () => usersApi.deactivate("u1"), "/api/users/u1/deactivate", { method: "POST" }],
    ["users.activate", () => usersApi.activate("u1"), "/api/users/u1/activate", { method: "POST" }],
    ["roles.list", () => rolesApi.list(), "/api/roles"],
    ["roles.permissions", () => rolesApi.permissions(), "/api/roles/permissions"],
    ["roles.create", () => rolesApi.create({ name: "R", description: null, permissions: [] }), "/api/roles", { method: "POST" }],
    ["roles.update", () => rolesApi.update("r1", { name: "R", description: null, permissions: ["user.view"] }), "/api/roles/r1", { method: "PUT" }],
    ["roles.remove", () => rolesApi.remove("r1"), "/api/roles/r1", { method: "DELETE" }],
    ["tasks.listForProject", () => tasksApi.listForProject("p1"), "/api/projects/p1/tasks"],
    ["tasks.assignedToMe", () => tasksApi.assignedToMe(), "/api/tasks/assigned-to-me"],
    ["tasks.get", () => tasksApi.get("t1"), "/api/tasks/t1"],
    ["tasks.assign", () => tasksApi.assign("t1", null), "/api/tasks/t1/assignee", { method: "PUT", body: { assigneeId: null } }],
    ["tasks.move", () => tasksApi.move("t1", "Done", 2), "/api/tasks/t1/move", { method: "POST", body: { status: "Done", position: 2 } }],
    ["tasks.remove", () => tasksApi.remove("t1"), "/api/tasks/t1", { method: "DELETE" }],
    ["tasks.comments", () => tasksApi.comments("t1"), "/api/tasks/t1/comments"],
    ["tasks.addComment", () => tasksApi.addComment("t1", "Merhaba"), "/api/tasks/t1/comments", { method: "POST", body: { body: "Merhaba" } }],
    ["tasks.deleteComment", () => tasksApi.deleteComment("t1", "c1"), "/api/tasks/t1/comments/c1", { method: "DELETE" }],
    ["attachments.list", () => attachmentsApi.list("t1"), "/api/tasks/t1/attachments"],
    ["attachments.upload", () => attachmentsApi.upload("t1", file), "/api/tasks/t1/attachments", { method: "POST" }],
    ["attachments.download", () => attachmentsApi.download("a1"), "/api/attachments/a1/content", { responseType: "blob" }],
    ["attachments.remove", () => attachmentsApi.remove("a1"), "/api/attachments/a1", { method: "DELETE" }],
    ["notifications.list", () => notificationsApi.list(), "/api/notifications"],
    ["notifications.markAsRead", () => notificationsApi.markAsRead("n1"), "/api/notifications/n1/read", { method: "POST" }],
    ["notifications.markAllAsRead", () => notificationsApi.markAllAsRead(), "/api/notifications/read-all", { method: "POST" }],
    ["search", () => searchApi.search("ödeme planı"), "/api/search?q=%C3%B6deme+plan%C4%B1"],
    ["reports.overview", () => reportsApi.overview({ today: "2026-10-21", projectId: "p1" }), "/api/reports/overview?today=2026-10-21&projectId=p1"],
    ["reports.me", () => reportsApi.me("2026-10-21"), "/api/reports/me?today=2026-10-21"],
    ["auditLogs.list", () => auditLogsApi.list({ page: 1, pageSize: 25, entityName: "TaskItem", action: "Deleted", from: "2026-10-01" }), "/api/audit-logs?page=1&pageSize=25&entityName=TaskItem&action=Deleted&from=2026-10-01"],
    ["auth.me", () => authApi.me(), "/api/auth/me"],
    ["auth.resendVerification", () => authApi.resendVerification(), "/api/auth/resend-verification", { method: "POST" }],
  ];

  it.each(cases)("%s", async (_name, call, path, options) => {
    await call();

    expectCall(mocks.api, path, options);
  });

  it("sends the upload as form data with the file", async () => {
    await attachmentsApi.upload("t1", file);

    const body = mocks.api.mock.calls[0][1].body as FormData;
    expect(body.get("file")).toBe(file);
  });
});

describe("anonymous auth endpoints", () => {
  const cases: Case[] = [
    ["login", () => authApi.login({ email: "a@b.c", password: "x" }), "/api/auth/login", { method: "POST", body: { email: "a@b.c", password: "x" } }],
    ["register", () => authApi.register({ companyName: "C", firstName: "A", lastName: "B", email: "a@b.c", password: "x" }), "/api/auth/register", { method: "POST" }],
    ["verifyEmail", () => authApi.verifyEmail("tok"), "/api/auth/verify-email", { method: "POST", body: { token: "tok" } }],
    ["forgotPassword", () => authApi.forgotPassword("a@b.c"), "/api/auth/forgot-password", { method: "POST", body: { email: "a@b.c" } }],
    ["resetPassword", () => authApi.resetPassword({ token: "tok", newPassword: "x" }), "/api/auth/reset-password", { method: "POST" }],
    ["previewInvitation", () => authApi.previewInvitation("tok"), "/api/auth/invitations/preview", { method: "POST", body: { token: "tok" } }],
    ["acceptInvitation", () => authApi.acceptInvitation({ token: "tok", password: "x" }), "/api/auth/invitations/accept", { method: "POST" }],
  ];

  it.each(cases)("%s does not need a session", async (_name, call, path, options) => {
    await call();

    expect(mocks.api).not.toHaveBeenCalled();
    expectCall(mocks.send, path, options);
  });
});
