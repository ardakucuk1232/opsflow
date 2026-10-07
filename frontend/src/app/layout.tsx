import type { Metadata } from "next";
import type { ReactNode } from "react";
import "@fontsource-variable/inter";
import "./globals.css";
import { AuthProvider } from "@/lib/auth/auth-provider";

export const metadata: Metadata = {
  title: {
    default: "OpsFlow",
    template: "%s · OpsFlow",
  },
  description: "Şirketiniz için proje, görev ve ekip yönetim platformu.",
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="tr" className="h-full antialiased">
      <body className="min-h-full">
        <AuthProvider>{children}</AuthProvider>
      </body>
    </html>
  );
}
