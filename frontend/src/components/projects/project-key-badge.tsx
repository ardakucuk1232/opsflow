export function ProjectKeyBadge({ projectKey }: { projectKey: string }) {
  return (
    <span className="inline-flex items-center rounded-md bg-surface-muted px-1.5 py-0.5 font-mono text-xs font-semibold tracking-wide text-muted">
      {projectKey}
    </span>
  );
}
