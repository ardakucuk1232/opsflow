import { api } from "@/lib/api/client";
import type { NotificationList } from "@/types/api";

export const notificationsApi = {
  list(signal?: AbortSignal): Promise<NotificationList> {
    return api<NotificationList>("/api/notifications", { signal });
  },

  markAsRead(id: string): Promise<void> {
    return api<void>(`/api/notifications/${encodeURIComponent(id)}/read`, { method: "POST" });
  },

  markAllAsRead(): Promise<void> {
    return api<void>("/api/notifications/read-all", { method: "POST" });
  },
};
