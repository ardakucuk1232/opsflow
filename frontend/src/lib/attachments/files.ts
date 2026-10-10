export const MAX_ATTACHMENT_BYTES = 10 * 1024 * 1024;

export const ALLOWED_ATTACHMENT_EXTENSIONS = [
  ".pdf",
  ".png",
  ".jpg",
  ".jpeg",
  ".gif",
  ".webp",
  ".txt",
  ".csv",
  ".docx",
  ".xlsx",
  ".pptx",
  ".zip",
] as const;

export const ATTACHMENT_ACCEPT = ALLOWED_ATTACHMENT_EXTENSIONS.join(",");

function extensionOf(fileName: string): string {
  const dot = fileName.lastIndexOf(".");

  return dot === -1 ? "" : fileName.slice(dot).toLowerCase();
}

export function validateAttachment(file: { name: string; size: number }): string | null {
  if (!(ALLOWED_ATTACHMENT_EXTENSIONS as readonly string[]).includes(extensionOf(file.name))) {
    return "Bu dosya türü yüklenemez. PDF, görsel, metin, Office belgesi veya ZIP dosyası seçin.";
  }

  if (file.size === 0) {
    return "Boş dosya yüklenemez.";
  }

  if (file.size > MAX_ATTACHMENT_BYTES) {
    return "Dosya 10 MB'tan büyük olamaz.";
  }

  return null;
}

const sizeFormatter = new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 1 });

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${sizeFormatter.format(bytes / 1024)} KB`;
  }

  return `${sizeFormatter.format(bytes / (1024 * 1024))} MB`;
}

export function saveBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");

  link.href = url;
  link.download = fileName;
  document.body.append(link);
  link.click();
  link.remove();

  window.setTimeout(() => URL.revokeObjectURL(url), 1_000);
}
