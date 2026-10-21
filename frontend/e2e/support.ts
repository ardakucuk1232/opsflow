import { expect, type APIRequestContext, type Page } from "@playwright/test";

export const API_URL = process.env.E2E_API_URL ?? "https://localhost:7080";
export const PASSWORD = "E2e-guvenli-sifre-1";

export type Account = {
  email: string;
  password: string;
  accessToken: string;
  companyName: string;
  firstName: string;
};

export function uniqueEmail(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@e2e.opsflow.test`;
}

export async function registerCompany(request: APIRequestContext, companyName: string, firstName = "Arda"): Promise<Account> {
  const email = uniqueEmail("admin");
  const response = await request.post(`${API_URL}/api/auth/register`, {
    data: { companyName, firstName, lastName: "Test", email, password: PASSWORD },
  });

  expect(response.status()).toBe(201);

  const body = await response.json();

  return { email, password: PASSWORD, accessToken: body.accessToken, companyName, firstName };
}

export async function apiPost<T>(request: APIRequestContext, account: Account, path: string, data: unknown): Promise<T> {
  const response = await request.post(`${API_URL}${path}`, {
    data,
    headers: { Authorization: `Bearer ${account.accessToken}` },
  });

  expect(response.ok(), `${path} -> ${response.status()}`).toBeTruthy();

  return (await response.json()) as T;
}

export async function signIn(page: Page, account: Pick<Account, "email" | "password">): Promise<void> {
  await page.goto("/login");
  await page.getByLabel("E-posta", { exact: true }).fill(account.email);
  await page.getByLabel("Şifre", { exact: true }).fill(account.password);
  await page.getByRole("button", { name: "Giriş yap" }).click();
  await page.waitForURL("**/dashboard");
}
