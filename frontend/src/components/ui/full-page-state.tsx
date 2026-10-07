import type { ReactNode } from "react";
import { Spinner } from "@/components/ui/spinner";

export function FullPageSpinner({ label = "Yükleniyor" }: { label?: string }) {
  return (
    <div className="flex min-h-dvh items-center justify-center bg-background" role="status">
      <Spinner className="size-6 text-primary" />
      <span className="sr-only">{label}</span>
    </div>
  );
}

type FullPageMessageProps = {
  title: string;
  description: string;
  action?: ReactNode;
};

export function FullPageMessage({ title, description, action }: FullPageMessageProps) {
  return (
    <div className="flex min-h-dvh items-center justify-center bg-background px-6">
      <div className="max-w-sm text-center">
        <h1 className="text-lg font-semibold text-foreground">{title}</h1>
        <p className="mt-2 text-sm text-muted">{description}</p>
        {action ? <div className="mt-6 flex justify-center">{action}</div> : null}
      </div>
    </div>
  );
}
