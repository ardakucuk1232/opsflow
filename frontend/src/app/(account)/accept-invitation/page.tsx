import type { Metadata } from "next";
import { AcceptInvitationForm } from "@/components/account/accept-invitation-form";

export const metadata: Metadata = {
  title: "Daveti kabul et",
};

export default function AcceptInvitationPage() {
  return <AcceptInvitationForm />;
}
