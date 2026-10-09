import type { Metadata } from "next";
import { ProjectDetailPage } from "@/components/projects/project-detail-page";

export const metadata: Metadata = {
  title: "Proje",
};

export default async function Page({ params }: { params: Promise<{ projectId: string }> }) {
  const { projectId } = await params;

  return <ProjectDetailPage projectId={projectId} />;
}
