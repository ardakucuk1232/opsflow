import type { Metadata } from "next";
import { VerifyEmailStatus } from "@/components/account/verify-email-status";

export const metadata: Metadata = {
  title: "E-posta doğrulama",
};

export default function VerifyEmailPage() {
  return <VerifyEmailStatus />;
}
