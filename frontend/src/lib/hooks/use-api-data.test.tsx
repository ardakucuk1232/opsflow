import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useApiData } from "@/lib/hooks/use-api-data";

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((done) => {
    resolve = done;
  });

  return { promise, resolve };
}

describe("useApiData", () => {
  it("starts loading and then exposes the data", async () => {
    const loader = vi.fn().mockResolvedValue(["a", "b"]);

    const { result } = renderHook(() => useApiData(loader));

    expect(result.current.loading).toBe(true);
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.data).toEqual(["a", "b"]);
    expect(result.current.error).toBeUndefined();
  });

  it("exposes the error when loading fails", async () => {
    const failure = new Error("boom");
    const loader = vi.fn().mockRejectedValue(failure);

    const { result } = renderHook(() => useApiData(loader));

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.error).toBe(failure);
  });

  it("loads again on reload and keeps the previous data meanwhile", async () => {
    const second = deferred<string>();
    const loader = vi.fn().mockResolvedValueOnce("first").mockReturnValueOnce(second.promise);

    const { result } = renderHook(() => useApiData(loader));
    await waitFor(() => expect(result.current.data).toBe("first"));

    act(() => result.current.reload());

    expect(result.current.loading).toBe(true);
    expect(result.current.data).toBe("first");

    await act(async () => second.resolve("second"));

    expect(result.current.loading).toBe(false);
    expect(result.current.data).toBe("second");
    expect(loader).toHaveBeenCalledTimes(2);
  });

  it("ignores a response that arrives after the loader changed", async () => {
    const slow = deferred<string>();
    const first = vi.fn(() => slow.promise);
    const second = vi.fn().mockResolvedValue("fresh");

    const { result, rerender } = renderHook(({ loader }) => useApiData(loader), {
      initialProps: { loader: first as (signal: AbortSignal) => Promise<string> },
    });

    rerender({ loader: second });
    await waitFor(() => expect(result.current.data).toBe("fresh"));

    await act(async () => slow.resolve("stale"));

    expect(result.current.data).toBe("fresh");
    expect((first.mock.calls[0] as unknown as [AbortSignal])[0].aborted).toBe(true);
  });
});
