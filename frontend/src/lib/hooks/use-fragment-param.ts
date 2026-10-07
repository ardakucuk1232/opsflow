"use client";

import { useSyncExternalStore } from "react";
import { readFragmentParam } from "@/lib/utils/fragment";

function subscribe(listener: () => void): () => void {
  window.addEventListener("hashchange", listener);

  return () => window.removeEventListener("hashchange", listener);
}

function getHash(): string {
  return window.location.hash;
}

function getServerHash(): undefined {
  return undefined;
}

export function useFragmentParam(name: string): string | null | undefined {
  const hash = useSyncExternalStore<string | undefined>(subscribe, getHash, getServerHash);

  return hash === undefined ? undefined : readFragmentParam(hash, name);
}
