import { redirect } from "next/navigation";
import { DEFAULT_AUTHENTICATED_PATH } from "@/lib/utils/redirect";

export default function HomePage() {
  redirect(DEFAULT_AUTHENTICATED_PATH);
}
