import { expect, test } from "@playwright/test";
import { PASSWORD, registerCompany, signIn, uniqueEmail } from "./support";

test("a new company can sign up and lands on its dashboard @mobile", async ({ page }) => {
  const email = uniqueEmail("signup");

  await page.goto("/register");
  await page.getByLabel("Şirket adı").fill("E2E Kayıt A.Ş.");
  await page.getByLabel("Ad", { exact: true }).fill("Deniz");
  await page.getByLabel("Soyad").fill("Kaya");
  await page.getByLabel("E-posta").fill(email);
  await page.getByLabel("Şifre", { exact: true }).fill(PASSWORD);
  await page.getByLabel("Şifre (tekrar)").fill(PASSWORD);
  await page.getByRole("button", { name: "Hesabı oluştur" }).click();

  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByRole("heading", { name: "Merhaba, Deniz" })).toBeVisible();
  await expect(page.getByText("E2E Kayıt A.Ş. çalışma alanındasınız.")).toBeVisible();
});

test("signing out and back in keeps the user in their company", async ({ page, request }) => {
  const account = await registerCompany(request, "E2E Oturum A.Ş.", "Selin");

  await signIn(page, account);
  await expect(page.getByRole("heading", { name: "Merhaba, Selin" })).toBeVisible();

  await page.getByRole("button", { name: /Selin Test/ }).click();
  await page.getByRole("menuitem", { name: "Çıkış yap" }).click();
  await expect(page).toHaveURL(/\/login/);

  await page.goto("/projects");
  await expect(page).toHaveURL(/\/login\?next=%2Fprojects/);

  await page.getByLabel("E-posta", { exact: true }).fill(account.email);
  await page.getByLabel("Şifre", { exact: true }).fill(account.password);
  await page.getByRole("button", { name: "Giriş yap" }).click();
  await expect(page).toHaveURL(/\/projects$/);
});

test("a wrong password is refused", async ({ page, request }) => {
  const account = await registerCompany(request, "E2E Hatalı Giriş A.Ş.");

  await page.goto("/login");
  await page.getByLabel("E-posta", { exact: true }).fill(account.email);
  await page.getByLabel("Şifre", { exact: true }).fill("Yanlis-sifre-9");
  await page.getByRole("button", { name: "Giriş yap" }).click();

  await expect(page.getByText("E-posta veya şifre hatalı.")).toBeVisible();
  await expect(page).toHaveURL(/\/login/);
});
