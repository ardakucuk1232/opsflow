import { expect, test } from "@playwright/test";
import { registerCompany, signIn } from "./support";

test("a project goes from creation to a finished task", async ({ page, request }) => {
  const account = await registerCompany(request, "E2E Proje A.Ş.");
  await signIn(page, account);

  await page.getByRole("navigation", { name: "Ana menü" }).getByRole("link", { name: "Projeler" }).click();
  await page.getByRole("button", { name: "Yeni proje" }).first().click();
  const projectDialog = page.getByRole("dialog");
  await projectDialog.getByLabel("Proje adı").fill("Kurumsal web sitesi");
  await projectDialog.getByRole("button", { name: "Projeyi oluştur" }).click();
  await expect(page.getByRole("heading", { name: "Kurumsal web sitesi" })).toBeVisible();

  for (const title of ["Ana sayfa tasarımı", "İletişim formu"]) {
    await page.getByRole("button", { name: "Yeni görev" }).click();
    const taskDialog = page.getByRole("dialog");
    await taskDialog.getByLabel("Başlık").fill(title);
    await taskDialog.getByRole("button", { name: "Görevi oluştur" }).click();
    await expect(taskDialog).toBeHidden();
  }

  const todo = page.getByRole("region", { name: "Yapılacak" });
  const inProgress = page.getByRole("region", { name: "Devam ediyor" });
  await expect(todo.getByRole("article")).toHaveCount(2);

  await todo.getByRole("article").filter({ hasText: "Ana sayfa tasarımı" }).dragTo(inProgress);
  await expect(inProgress.getByText("Ana sayfa tasarımı")).toBeVisible();
  await page.waitForTimeout(500);
  await page.reload();
  await expect(page.getByRole("region", { name: "Devam ediyor" }).getByText("Ana sayfa tasarımı")).toBeVisible();

  await page.getByRole("region", { name: "Devam ediyor" }).getByText("Ana sayfa tasarımı").click();
  const details = page.getByRole("dialog");
  await details.getByLabel("Yorum yaz").fill("Tasarım onaylandı.");
  await details.getByRole("button", { name: "Yorum ekle" }).click();
  await expect(details.getByText("Tasarım onaylandı.")).toBeVisible();
  await details.getByLabel("Durum").selectOption("Done");
  await page.keyboard.press("Escape");

  await expect(page.getByRole("region", { name: "Tamamlandı" }).getByText("Ana sayfa tasarımı")).toBeVisible();
});

test("quick search finds a task and opens it", async ({ page, request }) => {
  const account = await registerCompany(request, "E2E Arama A.Ş.");
  await signIn(page, account);

  await page.goto("/projects");
  await page.getByRole("button", { name: "Yeni proje" }).first().click();
  await page.getByRole("dialog").getByLabel("Proje adı").fill("Mobil uygulama");
  await page.getByRole("dialog").getByRole("button", { name: "Projeyi oluştur" }).click();
  await page.getByRole("button", { name: "Yeni görev" }).click();
  await page.getByRole("dialog").getByLabel("Başlık").fill("Ödeme ekranı");
  await page.getByRole("dialog").getByRole("button", { name: "Görevi oluştur" }).click();
  await expect(page.getByRole("dialog")).toBeHidden();

  await page.goto("/dashboard");
  await page.keyboard.press("Control+k");
  await page.getByRole("combobox", { name: "Proje veya görev ara" }).fill("ödeme");
  await page.getByRole("option", { name: /Ödeme ekranı/ }).click();

  await expect(page).toHaveURL(/\?task=/);
  await expect(page.getByRole("dialog").getByLabel("Başlık")).toHaveValue("Ödeme ekranı");
});
