"use client";

import { useCallback, useEffect, useState } from "react";

type Snapshot<T> = {
  loader: (signal: AbortSignal) => Promise<T>;
  version: number;
  data?: T;
  error?: unknown;
};

export type ApiData<T> = {
  data: T | undefined;
  error: unknown;
  loading: boolean;
  reload: () => void;
};

export function useApiData<T>(loader: (signal: AbortSignal) => Promise<T>): ApiData<T> {
  const [version, setVersion] = useState(0);
  const [snapshot, setSnapshot] = useState<Snapshot<T> | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    loader(controller.signal).then(
      (data) => {
        if (!controller.signal.aborted) {
          setSnapshot({ loader, version, data });
        }
      },
      (error: unknown) => {
        if (!controller.signal.aborted) {
          setSnapshot((previous) => ({ loader, version, data: previous?.data, error }));
        }
      },
    );

    return () => controller.abort();
  }, [loader, version]);

  const reload = useCallback(() => setVersion((current) => current + 1), []);

  const current = snapshot !== null && snapshot.loader === loader && snapshot.version === version;

  return {
    data: snapshot?.data,
    error: current ? snapshot.error : undefined,
    loading: !current,
    reload,
  };
}
