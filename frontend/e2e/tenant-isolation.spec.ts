import { expect, test } from "@playwright/test";
import { apiPost, registerCompany, signIn } from "./support";

test("one company cannot open another company's project or task by URL", async ({ page, request }) => {
  const owner = await registerCompany(request, "E2E Sahip A.Ş.");
  const project = await apiPost<{ id: string }>(request, owner, "/api/projects", {
    name: "Gizli proje",
    key: "GZL",
    description: null,
    status: "Active",
    startDate: null,
    endDate: null,
  });
  const task = await apiPost<{ id: string }>(request, owner, `/api/projects/${project.id}/tasks`, {
    title: "Gizli görev",
    description: null,
    status: "Todo",
    priority: "High",
    assigneeId: null,
    dueDate: null,
  });

  const intruder = await registerCompany(request, "E2E Meraklı A.Ş.", "Mert");
  await signIn(page, intruder);

  await page.goto(`/projects/${project.id}`);
  await expect(page.getByText("Gizli proje")).toHaveCount(0);
  await expect(page.getByText(/bulunamadı/i).first()).toBeVisible();

  await page.goto(`/projects/${project.id}?task=${task.id}`);
  await expect(page.getByText("Gizli görev")).toHaveCount(0);

  await page.goto("/tasks?q=Gizli");
  await expect(page.getByText("Filtrelerle eşleşen görev yok")).toBeVisible();

  await page.keyboard.press("Control+k");
  await page.getByRole("combobox", { name: "Proje veya görev ara" }).fill("Gizli");
  await expect(page.getByText("Eşleşen proje veya görev yok.")).toBeVisible();
});
