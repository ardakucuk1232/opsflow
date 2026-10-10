import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { act } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { NotificationBell } from "@/components/layout/notification-bell";
import type { AppNotification, NotificationList } from "@/types/api";

const mocks = vi.hoisted(() => ({
  list: vi.fn(),
  markAsRead: vi.fn(),
  markAllAsRead: vi.fn(),
  push: vi.fn(),
  handlers: new Map<string, (payload: unknown) => void>(),
}));

vi.mock("@/lib/api/notifications", () => ({
  notificationsApi: { list: mocks.list, markAsRead: mocks.markAsRead, markAllAsRead: mocks.markAllAsRead },
}));
vi.mock("next/navigation", () => ({ useRouter: () => ({ push: mocks.push }) }));
vi.mock("@/lib/realtime/realtime-provider", () => ({
  useRealtimeEvent: (event: string, handler: (payload: unknown) => void) => mocks.handlers.set(event, handler),
  useRealtimeReconnect: vi.fn(),
}));

function notification(overrides: Partial<AppNotification>): AppNotification {
  return {
    id: "n1",
    type: "TaskAssigned",
    title: "Size görev atandı",
    message: "Arda Küçük, WEB-1 Giriş sayfası görevini size atadı.",
    isRead: false,
    createdAt: new Date().toISOString(),
    link: "/projects/p1?task=t1",
    ...overrides,
  };
}

const LIST: NotificationList = {
  items: [
    notification({}),
    notification({ id: "n2", type: "ProjectMemberAdded", title: "Projeye eklendiniz", isRead: true, link: "/projects/p1" }),
  ],
  unreadCount: 1,
};

beforeEach(() => {
  mocks.list.mockReset().mockResolvedValue(LIST);
  mocks.markAsRead.mockReset().mockResolvedValue(undefined);
  mocks.markAllAsRead.mockReset().mockResolvedValue(undefined);
  mocks.push.mockReset();
  mocks.handlers.clear();
});

describe("NotificationBell", () => {
  it("shows the unread count and lists notifications", async () => {
    const user = userEvent.setup();
    render(<NotificationBell />);

    const bell = await screen.findByRole("button", { name: "Bildirimler, 1 okunmamış" });
    await user.click(bell);

    const panel = screen.getByRole("region", { name: "Bildirimler" });
    expect(within(panel).getByText("Size görev atandı")).toBeInTheDocument();
    expect(within(panel).getByText("Projeye eklendiniz")).toBeInTheDocument();
    expect(within(panel).getByText("(okunmadı)", { exact: false })).toBeInTheDocument();
  });

  it("marks an unread notification as read and opens its link", async () => {
    const user = userEvent.setup();
    render(<NotificationBell />);
    await user.click(await screen.findByRole("button", { name: /Bildirimler/ }));

    await user.click(screen.getByRole("button", { name: /Size görev atandı/ }));

    expect(mocks.markAsRead).toHaveBeenCalledWith("n1");
    expect(mocks.push).toHaveBeenCalledWith("/projects/p1?task=t1");
    expect(screen.queryByRole("region", { name: "Bildirimler" })).not.toBeInTheDocument();
  });

  it("does not mark an already read notification again", async () => {
    const user = userEvent.setup();
    render(<NotificationBell />);
    await user.click(await screen.findByRole("button", { name: /Bildirimler/ }));

    await user.click(screen.getByRole("button", { name: /Projeye eklendiniz/ }));

    expect(mocks.markAsRead).not.toHaveBeenCalled();
    expect(mocks.push).toHaveBeenCalledWith("/projects/p1");
  });

  it("marks everything as read", async () => {
    const user = userEvent.setup();
    render(<NotificationBell />);
    await user.click(await screen.findByRole("button", { name: /Bildirimler/ }));
    mocks.list.mockResolvedValue({ items: LIST.items.map((item) => ({ ...item, isRead: true })), unreadCount: 0 });

    await user.click(screen.getByRole("button", { name: "Tümünü okundu işaretle" }));

    expect(mocks.markAllAsRead).toHaveBeenCalled();
    expect(await screen.findByRole("button", { name: "Bildirimler" })).toBeInTheDocument();
  });

  it("reloads when a notification arrives live", async () => {
    render(<NotificationBell />);
    await screen.findByRole("button", { name: "Bildirimler, 1 okunmamış" });
    mocks.list.mockResolvedValue({ items: [notification({ id: "n3" }), ...LIST.items], unreadCount: 2 });

    act(() => mocks.handlers.get("notification")?.(notification({ id: "n3" })));

    expect(await screen.findByRole("button", { name: "Bildirimler, 2 okunmamış" })).toBeInTheDocument();
  });

  it("says so when there are no notifications", async () => {
    const user = userEvent.setup();
    mocks.list.mockResolvedValue({ items: [], unreadCount: 0 });
    render(<NotificationBell />);

    await user.click(await screen.findByRole("button", { name: "Bildirimler" }));

    expect(await screen.findByText("Henüz bildiriminiz yok.")).toBeInTheDocument();
  });
});
