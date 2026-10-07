import { z } from "zod";

const email = z
  .string()
  .trim()
  .min(1, "E-posta adresi zorunludur.")
  .max(256, "E-posta adresi en fazla 256 karakter olabilir.")
  .pipe(z.email("Geçerli bir e-posta adresi girin."));

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
    password: z
      .string()
      .min(8, "Şifre en az 8 karakter olmalıdır.")
      .max(128, "Şifre en fazla 128 karakter olabilir.")
      .regex(/[A-Za-z]/, "Şifre en az bir harf içermelidir.")
      .regex(/[0-9]/, "Şifre en az bir rakam içermelidir."),
    passwordConfirmation: z.string().min(1, "Şifreyi tekrar girin."),
  })
  .refine((values) => values.password === values.passwordConfirmation, {
    path: ["passwordConfirmation"],
    message: "Şifreler birbiriyle eşleşmiyor.",
  });

export type LoginFormValues = z.infer<typeof loginSchema>;
export type RegisterFormValues = z.infer<typeof registerSchema>;
