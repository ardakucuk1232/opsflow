"use client";

import { Download, FileText, Paperclip, Trash2 } from "lucide-react";
import { useCallback, useRef, useState } from "react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { attachmentsApi } from "@/lib/api/attachments";
import { ATTACHMENT_ACCEPT, formatFileSize, saveBlob, validateAttachment } from "@/lib/attachments/files";
import { PERMISSIONS } from "@/lib/auth/permissions";
import { usePermission } from "@/lib/auth/use-permission";
import { useApiData } from "@/lib/hooks/use-api-data";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import { formatDateTime } from "@/lib/utils/format";
import type { TaskAttachment } from "@/types/api";

export function TaskAttachments({ taskId, onChanged }: { taskId: string; onChanged: () => void }) {
  const canUpload = usePermission(PERMISSIONS.attachmentUpload);
  const loadAttachments = useCallback((signal: AbortSignal) => attachmentsApi.list(taskId, signal), [taskId]);
  const attachments = useApiData(loadAttachments);
  const inputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function handleFile(file: File | undefined) {
    if (inputRef.current) {
      inputRef.current.value = "";
    }

    if (!file) {
      return;
    }

    const problem = validateAttachment(file);
    setError(problem);

    if (problem) {
      return;
    }

    setUploading(true);

    try {
      await attachmentsApi.upload(taskId, file);
      attachments.reload();
      onChanged();
    } catch (exception) {
      setError(getErrorMessage(exception));
    } finally {
      setUploading(false);
    }
  }

  async function handleDownload(attachment: TaskAttachment) {
    setError(null);

    try {
      saveBlob(await attachmentsApi.download(attachment.id), attachment.fileName);
    } catch (exception) {
      setError(getErrorMessage(exception));
    }
  }

  async function handleDelete(attachment: TaskAttachment) {
    setError(null);
    setConfirmingId(null);

    try {
      await attachmentsApi.remove(attachment.id);
      attachments.reload();
      onChanged();
    } catch (exception) {
      setError(getErrorMessage(exception));
    }
  }

  return (
    <section aria-labelledby="attachments-heading" className="border-t border-border pt-5">
      <div className="flex items-center justify-between gap-3">
        <h3 id="attachments-heading" className="text-sm font-medium text-foreground">
          Dosyalar
        </h3>
        {canUpload ? (
          <>
            <input
              ref={inputRef}
              type="file"
              className="sr-only"
              accept={ATTACHMENT_ACCEPT}
              aria-label="Yüklenecek dosya"
              tabIndex={-1}
              onChange={(event) => void handleFile(event.target.files?.[0])}
            />
            <Button variant="secondary" size="sm" loading={uploading} onClick={() => inputRef.current?.click()}>
              <Paperclip className="size-4" aria-hidden="true" />
              Dosya ekle
            </Button>
          </>
        ) : null}
      </div>

      {error || (attachments.data === undefined && attachments.error) ? (
        <div className="mt-3">
          <Alert>{error ?? getErrorMessage(attachments.error)}</Alert>
        </div>
      ) : null}

      {attachments.data === undefined && attachments.loading ? (
        <div className="mt-3 flex justify-center py-4" role="status">
          <Spinner className="size-4 text-primary" />
          <span className="sr-only">Dosyalar yükleniyor</span>
        </div>
      ) : null}

      {attachments.data && attachments.data.length === 0 ? (
        <p className="mt-3 text-sm text-muted">Henüz dosya eklenmemiş.</p>
      ) : null}

      {attachments.data && attachments.data.length > 0 ? (
        <ul className="mt-3 flex flex-col gap-2">
          {attachments.data.map((attachment) => (
            <li key={attachment.id} className="flex items-center gap-3 rounded-lg border border-border px-3 py-2">
              <FileText className="size-5 shrink-0 text-muted" aria-hidden="true" />
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium text-foreground">{attachment.fileName}</p>
                <p className="truncate text-xs text-muted">
                  {formatFileSize(attachment.sizeBytes)} · {attachment.uploadedBy.firstName} {attachment.uploadedBy.lastName} ·{" "}
                  {formatDateTime(attachment.createdAt)}
                </p>
              </div>

              {confirmingId === attachment.id ? (
                <div className="flex shrink-0 items-center gap-2">
                  <Button variant="secondary" size="sm" onClick={() => setConfirmingId(null)}>
                    Vazgeç
                  </Button>
                  <Button variant="danger" size="sm" onClick={() => void handleDelete(attachment)}>
                    Dosyayı sil
                  </Button>
                </div>
              ) : (
                <div className="flex shrink-0 items-center gap-1">
                  <button
                    type="button"
                    aria-label={`${attachment.fileName} dosyasını indir`}
                    onClick={() => void handleDownload(attachment)}
                    className="flex size-8 items-center justify-center rounded-md text-muted hover:bg-surface-muted hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
                  >
                    <Download className="size-4" aria-hidden="true" />
                  </button>
                  {attachment.canDelete ? (
                    <button
                      type="button"
                      aria-label={`${attachment.fileName} dosyasını sil`}
                      onClick={() => setConfirmingId(attachment.id)}
                      className="flex size-8 items-center justify-center rounded-md text-muted hover:bg-danger-soft hover:text-danger focus-visible:outline-2 focus-visible:outline-ring"
                    >
                      <Trash2 className="size-4" aria-hidden="true" />
                    </button>
                  ) : null}
                </div>
              )}
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}
