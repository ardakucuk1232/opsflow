import { z } from "zod";

const email = z
  .string()
  .trim()
  .min(1, "E-posta adresi zorunludur.")
  .max(256, "E-posta adresi en fazla 256 karakter olabilir.")
  .pipe(z.email("Geçerli bir e-posta adresi girin."));

const newPassword = z
  .string()
  .min(8, "Şifre en az 8 karakter olmalıdır.")
  .max(128, "Şifre en fazla 128 karakter olabilir.")
  .regex(/[A-Za-z]/, "Şifre en az bir harf içermelidir.")
  .regex(/[0-9]/, "Şifre en az bir rakam içermelidir.");

const passwordConfirmation = z.string().min(1, "Şifreyi tekrar girin.");

const PASSWORD_MISMATCH = {
  path: ["passwordConfirmation"],
  message: "Şifreler birbiriyle eşleşmiyor.",
};

export const PASSWORD_HINT = "En az 8 karakter, bir harf ve bir rakam içermelidir.";

export const loginSchema = z.object({
  email,
  password: z
    .string()
    .min(1, "Şifre zorunludur.")
    .max(128, "Şifre en fazla 128 karakter olabilir."),
});

export const registerSchema = z
  .object({
    companyName: z
      .string()
      .trim()
      .min(1, "Şirket adı zorunludur.")
      .max(200, "Şirket adı en fazla 200 karakter olabilir."),
    firstName: z
      .string()
      .trim()
      .min(1, "Ad zorunludur.")
      .max(100, "Ad en fazla 100 karakter olabilir."),
    lastName: z
      .string()
      .trim()
      .min(1, "Soyad zorunludur.")
      .max(100, "Soyad en fazla 100 karakter olabilir."),
    email,
    password: newPassword,
    passwordConfirmation,
  })
  .refine((values) => values.password === values.passwordConfirmation, PASSWORD_MISMATCH);

export const forgotPasswordSchema = z.object({ email });

export const resetPasswordSchema = z
  .object({
    password: newPassword,
    passwordConfirmation,
  })
  .refine((values) => values.password === values.passwordConfirmation, PASSWORD_MISMATCH);

export const inviteUserSchema = z.object({
  firstName: z
    .string()
    .trim()
    .min(1, "Ad zorunludur.")
    .max(100, "Ad en fazla 100 karakter olabilir."),
  lastName: z
    .string()
    .trim()
    .min(1, "Soyad zorunludur.")
    .max(100, "Soyad en fazla 100 karakter olabilir."),
  email,
  roleIds: z.array(z.string()).min(1, "En az bir rol seçin."),
});

export const userRolesSchema = z.object({
  roleIds: z.array(z.string()).min(1, "En az bir rol seçin."),
});

export const roleSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "Rol adı zorunludur.")
    .max(100, "Rol adı en fazla 100 karakter olabilir."),
  description: z.string().trim().max(500, "Açıklama en fazla 500 karakter olabilir."),
  permissions: z.array(z.string()),
});

export const acceptInvitationSchema = z
  .object({
    password: newPassword,
    passwordConfirmation,
  })
  .refine((values) => values.password === values.passwordConfirmation, PASSWORD_MISMATCH);

const optionalDate = z.string().regex(/^(\d{4}-\d{2}-\d{2})?$/, "Geçerli bir tarih girin.");

const projectFields = {
  name: z
    .string()
    .trim()
    .min(1, "Proje adı zorunludur.")
    .max(200, "Proje adı en fazla 200 karakter olabilir."),
  description: z.string().trim().max(4000, "Açıklama en fazla 4000 karakter olabilir."),
  status: z.enum(["Planning", "Active", "OnHold", "Completed", "Cancelled"]),
  startDate: optionalDate,
  endDate: optionalDate,
};

const DATE_ORDER = {
  path: ["endDate"],
  message: "Bitiş tarihi başlangıç tarihinden önce olamaz.",
};

function datesInOrder(values: { startDate: string; endDate: string }): boolean {
  return values.startDate === "" || values.endDate === "" || values.endDate >= values.startDate;
}

export const createProjectSchema = z
  .object({
    ...projectFields,
    key: z
      .string()
      .trim()
      .regex(/^[A-Za-z][A-Za-z0-9]{1,9}$/, "Kısa ad 2-10 harf veya rakamdan oluşmalı ve harfle başlamalıdır."),
  })
  .refine(datesInOrder, DATE_ORDER);

export const taskSchema = z.object({
  title: z
    .string()
    .trim()
    .min(1, "Başlık zorunludur.")
    .max(300, "Başlık en fazla 300 karakter olabilir."),
  description: z.string().trim().max(10000, "Açıklama en fazla 10000 karakter olabilir."),
  status: z.enum(["Backlog", "Todo", "InProgress", "InReview", "Done", "Cancelled"]),
  priority: z.enum(["Low", "Medium", "High", "Critical"]),
  assigneeId: z.string(),
  dueDate: optionalDate,
});

export type LoginFormValues = z.infer<typeof loginSchema>;
export type RegisterFormValues = z.infer<typeof registerSchema>;
export type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>;
export type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>;
export type InviteUserFormValues = z.infer<typeof inviteUserSchema>;
export type UserRolesFormValues = z.infer<typeof userRolesSchema>;
export type RoleFormValues = z.infer<typeof roleSchema>;
export type AcceptInvitationFormValues = z.infer<typeof acceptInvitationSchema>;
export type CreateProjectFormValues = z.infer<typeof createProjectSchema>;
export type TaskFormValues = z.infer<typeof taskSchema>;
