import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ProjectFormDialog } from "@/components/projects/project-form-dialog";
import { ApiError } from "@/lib/api/errors";
import { PROJECT_DETAIL } from "@/test/projects";

const mocks = vi.hoisted(() => ({ create: vi.fn(), update: vi.fn() }));

vi.mock("@/lib/api/projects", () => ({ projectsApi: { create: mocks.create, update: mocks.update } }));

beforeEach(() => {
  mocks.create.mockReset().mockResolvedValue(PROJECT_DETAIL);
  mocks.update.mockReset().mockResolvedValue(PROJECT_DETAIL);
});

describe("ProjectFormDialog", () => {
  it("suggests a key from the name until the key is edited", async () => {
    const user = userEvent.setup();
    render(<ProjectFormDialog open onClose={vi.fn()} onSaved={vi.fn()} />);

    await user.type(screen.getByLabelText("Proje adı"), "Müşteri portalı");
    expect(screen.getByLabelText("Kısa ad")).toHaveValue("MP");

    await user.clear(screen.getByLabelText("Kısa ad"));
    await user.type(screen.getByLabelText("Kısa ad"), "crm");
    await user.type(screen.getByLabelText("Proje adı"), " v2");

    expect(screen.getByLabelText("Kısa ad")).toHaveValue("crm");
  });

  it("creates the project with an upper case key and empty fields as null", async () => {
    const user = userEvent.setup();
    const onSaved = vi.fn();
    render(<ProjectFormDialog open onClose={vi.fn()} onSaved={onSaved} />);

    await user.type(screen.getByLabelText("Proje adı"), "Web sitesi");
    await user.clear(screen.getByLabelText("Kısa ad"));
    await user.type(screen.getByLabelText("Kısa ad"), "web");
    await user.click(screen.getByRole("button", { name: "Projeyi oluştur" }));

    expect(mocks.create).toHaveBeenCalledWith({
      name: "Web sitesi",
      key: "WEB",
      description: null,
      status: "Planning",
      startDate: null,
      endDate: null,
    });
    expect(onSaved).toHaveBeenCalledWith(PROJECT_DETAIL);
  });

  it("rejects an end date before the start date", async () => {
    const user = userEvent.setup();
    render(<ProjectFormDialog open onClose={vi.fn()} onSaved={vi.fn()} />);

    await user.type(screen.getByLabelText("Proje adı"), "Web sitesi");
    await user.type(screen.getByLabelText("Başlangıç tarihi"), "2026-11-01");
    await user.type(screen.getByLabelText("Bitiş tarihi"), "2026-10-01");
    await user.click(screen.getByRole("button", { name: "Projeyi oluştur" }));

    expect(await screen.findByText("Bitiş tarihi başlangıç tarihinden önce olamaz.")).toBeInTheDocument();
    expect(mocks.create).not.toHaveBeenCalled();
  });

  it("shows a taken key on the key field", async () => {
    const user = userEvent.setup();
    mocks.create.mockRejectedValueOnce(new ApiError({ status: 409, code: "projects.key_taken", message: "x" }));
    render(<ProjectFormDialog open onClose={vi.fn()} onSaved={vi.fn()} />);

    await user.type(screen.getByLabelText("Proje adı"), "Web sitesi");
    await user.click(screen.getByRole("button", { name: "Projeyi oluştur" }));

    expect(await screen.findByText("Bu kısa adı kullanan bir proje zaten var.")).toBeInTheDocument();
  });

  it("edits a project without changing its key", async () => {
    const user = userEvent.setup();
    render(<ProjectFormDialog open project={PROJECT_DETAIL} onClose={vi.fn()} onSaved={vi.fn()} />);

    expect(screen.getByLabelText("Kısa ad")).toHaveAttribute("readonly");

    await user.selectOptions(screen.getByLabelText("Durum"), "Completed");
    await user.click(screen.getByRole("button", { name: "Kaydet" }));

    expect(mocks.update).toHaveBeenCalledWith("project-1", {
      name: "Web sitesi",
      description: "Kurumsal web sitesinin yenilenmesi",
      status: "Completed",
      startDate: "2026-10-01",
      endDate: "2026-12-31",
    });
  });
});
