"use client";

import { Trash2 } from "lucide-react";
import { useCallback, useState, type FormEvent } from "react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { Textarea } from "@/components/ui/textarea";
import { tasksApi } from "@/lib/api/tasks";
import { PERMISSIONS } from "@/lib/auth/permissions";
import { getInitials } from "@/lib/auth/roles";
import { usePermission } from "@/lib/auth/use-permission";
import { useApiData } from "@/lib/hooks/use-api-data";
import { getErrorMessage } from "@/lib/i18n/error-messages";
import { formatDateTime } from "@/lib/utils/format";

export function TaskComments({ taskId, onChanged }: { taskId: string; onChanged: () => void }) {
  const canComment = usePermission(PERMISSIONS.commentCreate);
  const loadComments = useCallback((signal: AbortSignal) => tasksApi.comments(taskId, signal), [taskId]);
  const comments = useApiData(loadComments);
  const [body, setBody] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();

    if (body.trim() === "") {
      return;
    }

    setPending(true);
    setError(null);

    try {
      await tasksApi.addComment(taskId, body);
      setBody("");
      comments.reload();
      onChanged();
    } catch (exception) {
      setError(getErrorMessage(exception));
    } finally {
      setPending(false);
    }
  }

  async function handleDelete(commentId: string) {
    setError(null);

    try {
      await tasksApi.deleteComment(taskId, commentId);
      comments.reload();
      onChanged();
    } catch (exception) {
      setError(getErrorMessage(exception));
    }
  }

  return (
    <section aria-labelledby="comments-heading" className="border-t border-border pt-5">
      <h3 id="comments-heading" className="text-sm font-medium text-foreground">
        Yorumlar
      </h3>

      {error ? (
        <div className="mt-3">
          <Alert>{error}</Alert>
        </div>
      ) : null}

      {comments.data === undefined && comments.loading ? (
        <div className="mt-3 flex justify-center py-4" role="status">
          <Spinner className="size-4 text-primary" />
          <span className="sr-only">Yorumlar yükleniyor</span>
        </div>
      ) : null}

      {comments.data && comments.data.length === 0 ? (
        <p className="mt-3 text-sm text-muted">Henüz yorum yok.</p>
      ) : null}

      {comments.data && comments.data.length > 0 ? (
        <ul className="mt-3 flex flex-col gap-4">
          {comments.data.map((comment) => (
            <li key={comment.id} className="flex gap-3">
              <span
                className="flex size-8 shrink-0 items-center justify-center rounded-full bg-primary-soft text-xs font-semibold text-primary"
                aria-hidden="true"
              >
                {getInitials(comment.author.firstName, comment.author.lastName)}
              </span>
              <div className="min-w-0 flex-1">
                <div className="flex items-center justify-between gap-2">
                  <p className="text-sm">
                    <span className="font-medium text-foreground">
                      {comment.author.firstName} {comment.author.lastName}
                    </span>{" "}
                    <span className="text-xs text-muted">{formatDateTime(comment.createdAt)}</span>
                  </p>
                  {comment.canDelete ? (
                    <button
                      type="button"
                      aria-label="Yorumu sil"
                      onClick={() => void handleDelete(comment.id)}
                      className="flex size-7 items-center justify-center rounded-md text-muted hover:bg-danger-soft hover:text-danger focus-visible:outline-2 focus-visible:outline-ring"
                    >
                      <Trash2 className="size-3.5" aria-hidden="true" />
                    </button>
                  ) : null}
                </div>
                <p className="mt-1 whitespace-pre-line break-words text-sm text-foreground">{comment.body}</p>
              </div>
            </li>
          ))}
        </ul>
      ) : null}

      {canComment ? (
        <form onSubmit={(event) => void handleSubmit(event)} className="mt-4 flex flex-col gap-3">
          <Textarea
            aria-label="Yorum yaz"
            placeholder="Yorum yazın"
            rows={3}
            maxLength={5000}
            value={body}
            onChange={(event) => setBody(event.target.value)}
          />
          <div className="flex justify-end">
            <Button type="submit" size="sm" loading={pending} disabled={body.trim() === ""}>
              Yorum ekle
            </Button>
          </div>
        </form>
      ) : null}
    </section>
  );
}
