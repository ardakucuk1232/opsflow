const DEFAULT_API_URL = "https://localhost:7080";

export const env = {
  apiUrl: (process.env.NEXT_PUBLIC_API_URL ?? DEFAULT_API_URL).replace(/\/+$/, ""),
} as const;
