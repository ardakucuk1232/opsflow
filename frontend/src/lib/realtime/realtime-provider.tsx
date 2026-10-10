"use client";

import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from "@microsoft/signalr";
import { createContext, useContext, useEffect, useRef, useState, type ReactNode } from "react";
import { env } from "@/config/env";
import { session } from "@/lib/auth/session";

export const HUB_PATH = "/hubs/notifications";

const RETRY_DELAYS_MS = [0, 2_000, 5_000, 10_000, 30_000];
const RESTART_DELAY_MS = 30_000;

type Realtime = { connection: HubConnection } | null;

type TaskChanged = { projectId: string; taskId: string };

const RealtimeContext = createContext<Realtime>(null);

function createConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${env.apiUrl}${HUB_PATH}`, {
      accessTokenFactory: async () => (await session.getAccessToken()) ?? "",
      withCredentials: false,
    })
    .withAutomaticReconnect(RETRY_DELAYS_MS)
    .configureLogging(LogLevel.None)
    .build();
}

export function RealtimeProvider({ children }: { children: ReactNode }) {
  const [realtime, setRealtime] = useState<Realtime>(null);

  useEffect(() => {
    const connection = createConnection();
    let disposed = false;
    let attempt = 0;
    let timer: ReturnType<typeof setTimeout> | undefined;

    function connected() {
      if (!disposed) {
        setRealtime({ connection });
      }
    }

    function schedule(delay: number) {
      if (!disposed) {
        timer = setTimeout(() => void start(), delay);
      }
    }

    async function start() {
      try {
        await connection.start();
        attempt = 0;
        connected();
      } catch {
        attempt += 1;
        schedule(RETRY_DELAYS_MS[Math.min(attempt, RETRY_DELAYS_MS.length - 1)]);
      }
    }

    connection.onreconnecting(() => {
      if (!disposed) {
        setRealtime(null);
      }
    });
    connection.onreconnected(connected);
    connection.onclose(() => {
      if (!disposed) {
        setRealtime(null);
        schedule(RESTART_DELAY_MS);
      }
    });

    void start();

    return () => {
      disposed = true;
      clearTimeout(timer);
      void connection.stop();
    };
  }, []);

  return <RealtimeContext.Provider value={realtime}>{children}</RealtimeContext.Provider>;
}

function useLatest<T>(value: T) {
  const ref = useRef(value);

  useEffect(() => {
    ref.current = value;
  });

  return ref;
}

export function useRealtimeEvent<T>(event: string, handler: (payload: T) => void): void {
  const realtime = useContext(RealtimeContext);
  const latest = useLatest(handler);

  useEffect(() => {
    if (!realtime) {
      return;
    }

    const listener = (payload: T) => latest.current(payload);

    realtime.connection.on(event, listener);

    return () => realtime.connection.off(event, listener);
  }, [realtime, event, latest]);
}

export function useRealtimeReconnect(callback: () => void): void {
  const realtime = useContext(RealtimeContext);
  const latest = useLatest(callback);
  const seen = useRef(false);

  useEffect(() => {
    if (!realtime) {
      return;
    }

    if (seen.current) {
      latest.current();
    }

    seen.current = true;
  }, [realtime, latest]);
}

export function useProjectChanges(projectId: string, onChanged: () => void): void {
  const realtime = useContext(RealtimeContext);
  const latest = useLatest(onChanged);

  useRealtimeReconnect(onChanged);

  useEffect(() => {
    if (!realtime) {
      return;
    }

    const { connection } = realtime;
    let timer: ReturnType<typeof setTimeout> | undefined;

    const listener = (payload: TaskChanged) => {
      if (payload.projectId !== projectId) {
        return;
      }

      clearTimeout(timer);
      timer = setTimeout(() => latest.current(), 250);
    };

    connection.on("taskChanged", listener);
    connection.invoke<boolean>("WatchProject", projectId).catch(() => undefined);

    return () => {
      clearTimeout(timer);
      connection.off("taskChanged", listener);

      if (connection.state === HubConnectionState.Connected) {
        connection.invoke("UnwatchProject", projectId).catch(() => undefined);
      }
    };
  }, [realtime, projectId, latest]);
}
