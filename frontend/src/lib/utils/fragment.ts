export function readFragmentParam(hash: string, name: string): string | null {
  const params = new URLSearchParams(hash.startsWith("#") ? hash.slice(1) : hash);
  const value = params.get(name);

  return value ? value : null;
}
