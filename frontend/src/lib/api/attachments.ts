import { api } from "@/lib/api/client";
import type { TaskAttachment } from "@/types/api";

function attachmentPath(id: string, suffix = ""): string {
  return `/api/attachments/${encodeURIComponent(id)}${suffix}`;
}

export const attachmentsApi = {
  list(taskId: string, signal?: AbortSignal): Promise<TaskAttachment[]> {
    return api<TaskAttachment[]>(`/api/tasks/${encodeURIComponent(taskId)}/attachments`, { signal });
  },

  upload(taskId: string, file: File): Promise<TaskAttachment> {
    const form = new FormData();
    form.append("file", file);

    return api<TaskAttachment>(`/api/tasks/${encodeURIComponent(taskId)}/attachments`, { method: "POST", body: form });
  },

  download(id: string): Promise<Blob> {
    return api<Blob>(attachmentPath(id, "/content"), { responseType: "blob" });
  },

  remove(id: string): Promise<void> {
    return api<void>(attachmentPath(id), { method: "DELETE" });
  },
};
