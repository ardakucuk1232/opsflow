import type { Metadata } from "next";
import { ResetPasswordForm } from "@/components/account/reset-password-form";

export const metadata: Metadata = {
  title: "Yeni şifre belirle",
};

export default function ResetPasswordPage() {
  return <ResetPasswordForm />;
}
