export const DEFAULT_AUTHENTICATED_PATH = "/dashboard";

export function getSafeRedirectPath(value: string | null | undefined): string {
  if (!value || !value.startsWith("/") || value.startsWith("//") || value.includes("\\")) {
    return DEFAULT_AUTHENTICATED_PATH;
  }

  return value;
}
