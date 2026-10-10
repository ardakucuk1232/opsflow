import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { TaskAttachments } from "@/components/tasks/task-attachments";
import { ApiError } from "@/lib/api/errors";
import { TEST_USER } from "@/test/fixtures";
import type { AuthUser, TaskAttachment } from "@/types/api";

const mocks = vi.hoisted(() => ({
  list: vi.fn(),
  upload: vi.fn(),
  download: vi.fn(),
  remove: vi.fn(),
  saveBlob: vi.fn(),
  user: null as AuthUser | null,
}));

vi.mock("@/lib/api/attachments", () => ({
  attachmentsApi: { list: mocks.list, upload: mocks.upload, download: mocks.download, remove: mocks.remove },
}));
vi.mock("@/lib/attachments/files", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@/lib/attachments/files")>()),
  saveBlob: mocks.saveBlob,
}));
vi.mock("@/lib/auth/auth-provider", () => ({ useAuth: () => ({ user: mocks.user }) }));

const ATTACHMENT: TaskAttachment = {
  id: "a1",
  fileName: "tasarim.pdf",
  contentType: "application/pdf",
  sizeBytes: 2_048,
  uploadedBy: { id: "u2", firstName: "Ayşe", lastName: "Yılmaz", email: "ayse@abc.com" },
  createdAt: "2026-10-09T10:00:00Z",
  canDelete: true,
};

beforeEach(() => {
  mocks.list.mockReset().mockResolvedValue([ATTACHMENT]);
  mocks.upload.mockReset().mockResolvedValue(ATTACHMENT);
  mocks.download.mockReset().mockResolvedValue(new Blob(["pdf"]));
  mocks.remove.mockReset().mockResolvedValue(undefined);
  mocks.saveBlob.mockReset();
  mocks.user = TEST_USER;
});

function renderAttachments(onChanged = vi.fn()) {
  render(<TaskAttachments taskId="t1" onChanged={onChanged} />);

  return onChanged;
}

describe("TaskAttachments", () => {
  it("lists the files of the task", async () => {
    renderAttachments();

    expect(await screen.findByText("tasarim.pdf")).toBeInTheDocument();
    expect(screen.getByText(/2 KB · Ayşe Yılmaz/)).toBeInTheDocument();
  });

  it("uploads a chosen file", async () => {
    const user = userEvent.setup();
    const onChanged = renderAttachments();
    await screen.findByText("tasarim.pdf");
    const file = new File(["hello"], "notlar.txt", { type: "text/plain" });

    await user.upload(screen.getByLabelText("Yüklenecek dosya"), file);

    await waitFor(() => expect(mocks.upload).toHaveBeenCalledWith("t1", file));
    expect(onChanged).toHaveBeenCalled();
  });

  it("checks the file type before uploading", async () => {
    const user = userEvent.setup({ applyAccept: false });
    renderAttachments();
    await screen.findByText("tasarim.pdf");

    await user.upload(screen.getByLabelText("Yüklenecek dosya"), new File(["x"], "virus.exe"));

    expect(await screen.findByText(/Bu dosya türü yüklenemez/)).toBeInTheDocument();
    expect(mocks.upload).not.toHaveBeenCalled();
  });

  it("shows the server's reason when an upload is rejected", async () => {
    const user = userEvent.setup();
    mocks.upload.mockRejectedValue(new ApiError({ status: 422, code: "attachments.content_mismatch", message: "x" }));
    renderAttachments();
    await screen.findByText("tasarim.pdf");

    await user.upload(screen.getByLabelText("Yüklenecek dosya"), new File(["x"], "resim.png", { type: "image/png" }));

    expect(await screen.findByText("Dosyanın içeriği uzantısıyla uyuşmuyor.")).toBeInTheDocument();
  });

  it("downloads a file with the session", async () => {
    const user = userEvent.setup();
    renderAttachments();

    await user.click(await screen.findByRole("button", { name: "tasarim.pdf dosyasını indir" }));

    expect(mocks.download).toHaveBeenCalledWith("a1");
    await waitFor(() => expect(mocks.saveBlob).toHaveBeenCalledWith(expect.any(Blob), "tasarim.pdf"));
  });

  it("asks before deleting a file", async () => {
    const user = userEvent.setup();
    const onChanged = renderAttachments();

    await user.click(await screen.findByRole("button", { name: "tasarim.pdf dosyasını sil" }));
    expect(mocks.remove).not.toHaveBeenCalled();

    await user.click(screen.getByRole("button", { name: "Dosyayı sil" }));

    expect(mocks.remove).toHaveBeenCalledWith("a1");
    await waitFor(() => expect(onChanged).toHaveBeenCalled());
  });

  it("hides upload and delete when the user is not allowed", async () => {
    mocks.user = { ...TEST_USER, permissions: ["user.view"] };
    mocks.list.mockResolvedValue([{ ...ATTACHMENT, canDelete: false }]);
    renderAttachments();

    await screen.findByText("tasarim.pdf");

    expect(screen.queryByRole("button", { name: "Dosya ekle" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "tasarim.pdf dosyasını sil" })).not.toBeInTheDocument();
  });
});
