import { api } from "@/lib/api/client";
import type { SearchResult } from "@/types/api";

export const SEARCH_MIN_LENGTH = 2;

export const searchApi = {
  search(term: string, signal?: AbortSignal): Promise<SearchResult> {
    return api<SearchResult>(`/api/search?${new URLSearchParams({ q: term })}`, { signal });
  },
};
