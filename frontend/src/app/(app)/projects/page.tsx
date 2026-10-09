import type { Metadata } from "next";
import { ProjectsPage } from "@/components/projects/projects-page";

export const metadata: Metadata = {
  title: "Projeler",
};

export default function Page() {
  return <ProjectsPage />;
}
