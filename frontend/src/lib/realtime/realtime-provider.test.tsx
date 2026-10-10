import { act, render, screen } from "@testing-library/react";
import { useState } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RealtimeProvider, useProjectChanges, useRealtimeEvent } from "@/lib/realtime/realtime-provider";

type Handler = (payload: unknown) => void;

const fake = vi.hoisted(() => {
  const connection = {
    state: "Connected",
    handlers: new Map<string, Set<(payload: unknown) => void>>(),
    start: vi.fn(),
    stop: vi.fn(),
    invoke: vi.fn(),
    on(event: string, handler: (payload: unknown) => void) {
      if (!connection.handlers.has(event)) {
        connection.handlers.set(event, new Set());
      }
      connection.handlers.get(event)?.add(handler);
    },
    off(event: string, handler: (payload: unknown) => void) {
      connection.handlers.get(event)?.delete(handler);
    },
    onreconnecting: vi.fn(),
    onreconnected: vi.fn(),
    onclose: vi.fn(),
    emit(event: string, payload: unknown) {
      connection.handlers.get(event)?.forEach((handler) => handler(payload));
    },
  };

  return { connection, options: null as null | { accessTokenFactory: () => Promise<string> } };
});

vi.mock("@microsoft/signalr", () => {
  class HubConnectionBuilder {
    withUrl(_url: string, options: { accessTokenFactory: () => Promise<string> }) {
      fake.options = options;
      return this;
    }
    withAutomaticReconnect() {
      return this;
    }
    configureLogging() {
      return this;
    }
    build() {
      return fake.connection;
    }
  }

  return { HubConnectionBuilder, HubConnectionState: { Connected: "Connected" }, LogLevel: { None: 6 } };
});

vi.mock("@/lib/auth/session", () => ({ session: { getAccessToken: vi.fn().mockResolvedValue("access-1") } }));

beforeEach(() => {
  fake.connection.handlers.clear();
  fake.connection.start.mockReset().mockResolvedValue(undefined);
  fake.connection.stop.mockReset().mockResolvedValue(undefined);
  fake.connection.invoke.mockReset().mockResolvedValue(true);
});

function Board({ projectId }: { projectId: string }) {
  const [reloads, setReloads] = useState(0);

  useProjectChanges(projectId, () => setReloads((count) => count + 1));

  return <p>Yenileme: {reloads}</p>;
}

function Listener({ onEvent }: { onEvent: Handler }) {
  useRealtimeEvent("notification", onEvent);

  return null;
}

describe("RealtimeProvider", () => {
  it("connects with the session's access token and stops on unmount", async () => {
    const { unmount } = render(<RealtimeProvider><p>içerik</p></RealtimeProvider>);

    await act(async () => undefined);

    expect(fake.connection.start).toHaveBeenCalled();
    await expect(fake.options?.accessTokenFactory()).resolves.toBe("access-1");

    unmount();

    expect(fake.connection.stop).toHaveBeenCalled();
  });

  it("delivers events to subscribers", async () => {
    const onEvent = vi.fn();
    render(<RealtimeProvider><Listener onEvent={onEvent} /></RealtimeProvider>);
    await act(async () => undefined);

    act(() => fake.connection.emit("notification", { id: "n1" }));

    expect(onEvent).toHaveBeenCalledWith({ id: "n1" });
  });

  it("watches a project and reloads only for its own changes", async () => {
    vi.useFakeTimers();

    try {
      const { unmount } = render(<RealtimeProvider><Board projectId="p1" /></RealtimeProvider>);
      await act(async () => undefined);

      expect(fake.connection.invoke).toHaveBeenCalledWith("WatchProject", "p1");

      act(() => {
        fake.connection.emit("taskChanged", { projectId: "p2", taskId: "x" });
        fake.connection.emit("taskChanged", { projectId: "p1", taskId: "a" });
        fake.connection.emit("taskChanged", { projectId: "p1", taskId: "b" });
      });
      act(() => vi.advanceTimersByTime(300));

      expect(screen.getByText("Yenileme: 1")).toBeInTheDocument();

      unmount();

      expect(fake.connection.invoke).toHaveBeenCalledWith("UnwatchProject", "p1");
    } finally {
      vi.useRealTimers();
    }
  });

  it("retries when the first connection attempt fails", async () => {
    vi.useFakeTimers();

    try {
      fake.connection.start.mockRejectedValueOnce(new Error("offline")).mockResolvedValue(undefined);
      render(<RealtimeProvider><p>içerik</p></RealtimeProvider>);
      await act(async () => undefined);

      expect(fake.connection.start).toHaveBeenCalledTimes(1);

      await act(async () => vi.advanceTimersByTime(2_000));

      expect(fake.connection.start).toHaveBeenCalledTimes(2);
    } finally {
      vi.useRealTimers();
    }
  });
});
