"use client";

import { Bell } from "lucide-react";
import { useRouter } from "next/navigation";
import { useEffect, useId, useRef, useState } from "react";
import { Alert } from "@/components/ui/alert";
import { Spinner } from "@/components/ui/spinner";
import { notificationsApi } from "@/lib/api/notifications";
import { useApiData } from "@/lib/hooks/use-api-data";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import { useRealtimeEvent, useRealtimeReconnect } from "@/lib/realtime/realtime-provider";
import { cn } from "@/lib/utils/cn";
import { formatRelativeTime } from "@/lib/utils/format";
import type { AppNotification } from "@/types/api";

function unreadLabel(count: number): string {
  return count > 9 ? "9+" : String(count);
}

export function NotificationBell() {
  const router = useRouter();
  const notifications = useApiData(notificationsApi.list);
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const panelId = useId();

  useRealtimeEvent<AppNotification>("notification", notifications.reload);
  useRealtimeReconnect(notifications.reload);

  useEffect(() => {
    if (!open) {
      return;
    }

    function handlePointerDown(event: PointerEvent) {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setOpen(false);
      }
    }

    document.addEventListener("pointerdown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("pointerdown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [open]);

  const items = notifications.data?.items ?? [];
  const unread = notifications.data?.unreadCount ?? 0;

  async function openNotification(notification: AppNotification) {
    setOpen(false);

    if (!notification.isRead) {
      await notificationsApi.markAsRead(notification.id).catch(() => undefined);
      notifications.reload();
    }

    if (notification.link) {
      router.push(notification.link);
    }
  }

  async function markAllAsRead() {
    setError(null);

    try {
      await notificationsApi.markAllAsRead();
      notifications.reload();
    } catch (exception) {
      setError(getErrorMessage(exception));
    }
  }

  return (
    <div ref={containerRef} className="sm:relative">
      <button
        type="button"
        onClick={() => {
          setOpen((current) => !current);
          setError(null);
        }}
        aria-label={unread > 0 ? `Bildirimler, ${unread} okunmamış` : "Bildirimler"}
        aria-expanded={open}
        aria-controls={open ? panelId : undefined}
        className="relative flex size-9 items-center justify-center rounded-lg text-muted transition-colors hover:bg-surface-muted hover:text-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
      >
        <Bell className="size-5" aria-hidden="true" />
        {unread > 0 ? (
          <span
            className="absolute right-0.5 top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-danger px-1 text-[10px] font-semibold leading-none text-white"
            aria-hidden="true"
          >
            {unreadLabel(unread)}
          </span>
        ) : null}
      </button>

      {open ? (
        <div
          id={panelId}
          role="region"
          aria-label="Bildirimler"
          className="fixed inset-x-4 top-16 z-20 overflow-hidden rounded-xl border border-border bg-surface shadow-lg sm:absolute sm:inset-x-auto sm:right-0 sm:top-auto sm:mt-2 sm:w-96"
        >
          <div className="flex items-center justify-between gap-3 border-b border-border px-4 py-3">
            <p className="text-sm font-medium text-foreground">Bildirimler</p>
            <button
              type="button"
              onClick={() => void markAllAsRead()}
              disabled={unread === 0}
              className="rounded-md text-xs font-medium text-primary hover:underline focus-visible:outline-2 focus-visible:outline-ring disabled:cursor-not-allowed disabled:text-muted disabled:no-underline"
            >
              Tümünü okundu işaretle
            </button>
          </div>

          {error ? (
            <div className="px-4 pt-3">
              <Alert>{error}</Alert>
            </div>
          ) : null}

          <div className="max-h-96 overflow-y-auto">
            {notifications.data === undefined && notifications.loading ? (
              <div className="flex justify-center py-8" role="status">
                <Spinner className="size-4 text-primary" />
                <span className="sr-only">Bildirimler yükleniyor</span>
              </div>
            ) : null}

            {notifications.data === undefined && !notifications.loading ? (
              <p className="px-4 py-8 text-center text-sm text-muted">{getErrorMessage(notifications.error)}</p>
            ) : null}

            {notifications.data && items.length === 0 ? (
              <p className="px-4 py-8 text-center text-sm text-muted">Henüz bildiriminiz yok.</p>
            ) : null}

            {items.length > 0 ? (
              <ul className="divide-y divide-border">
                {items.map((notification) => (
                  <li key={notification.id}>
                    <button
                      type="button"
                      onClick={() => void openNotification(notification)}
                      className={cn(
                        "flex w-full gap-3 px-4 py-3 text-left transition-colors hover:bg-surface-muted focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring",
                        !notification.isRead && "bg-primary-soft/40",
                      )}
                    >
                      <span
                        className={cn(
                          "mt-1.5 size-2 shrink-0 rounded-full",
                          notification.isRead ? "bg-transparent" : "bg-primary",
                        )}
                        aria-hidden="true"
                      />
                      <span className="min-w-0 flex-1">
                        <span className="block text-sm font-medium text-foreground">
                          {notification.title}
                          {notification.isRead ? null : <span className="sr-only"> (okunmadı)</span>}
                        </span>
                        <span className="mt-0.5 block break-words text-sm text-muted">{notification.message}</span>
                        <span className="mt-1 block text-xs text-muted">{formatRelativeTime(notification.createdAt)}</span>
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
            ) : null}
          </div>
        </div>
      ) : null}
    </div>
  );
}
