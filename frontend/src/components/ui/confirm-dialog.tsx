"use client";

import { useState, type ReactNode } from "react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Dialog, DialogActions } from "@/components/ui/dialog";
import { getErrorMessage } from "@/lib/i18n/error-messages";

type ConfirmDialogProps = {
  open: boolean;
  title: string;
  description: ReactNode;
  confirmLabel: string;
  tone?: "primary" | "danger";
  onConfirm: () => Promise<void>;
  onClose: () => void;
};

export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel,
  tone = "primary",
  onConfirm,
  onClose,
}: ConfirmDialogProps) {
  return (
    <Dialog open={open} onClose={onClose} title={title}>
      <ConfirmBody
        description={description}
        confirmLabel={confirmLabel}
        tone={tone}
        onConfirm={onConfirm}
        onClose={onClose}
      />
    </Dialog>
  );
}

function ConfirmBody({
  description,
  confirmLabel,
  tone,
  onConfirm,
  onClose,
}: Omit<ConfirmDialogProps, "open" | "title">) {
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleConfirm() {
    setPending(true);
    setError(null);

    try {
      await onConfirm();
    } catch (exception) {
      setError(getErrorMessage(exception));
      setPending(false);
    }
  }

  return (
    <div>
      <div className="text-sm leading-relaxed text-muted">{description}</div>
      {error ? (
        <div className="mt-4">
          <Alert>{error}</Alert>
        </div>
      ) : null}
      <DialogActions>
        <Button variant="secondary" onClick={onClose} disabled={pending}>
          Vazgeç
        </Button>
        <Button variant={tone === "danger" ? "danger" : "primary"} loading={pending} onClick={() => void handleConfirm()}>
          {confirmLabel}
        </Button>
      </DialogActions>
    </div>
  );
}
