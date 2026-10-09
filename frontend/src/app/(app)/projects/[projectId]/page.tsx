import type { Metadata } from "next";
import { Suspense } from "react";
import { ProjectDetailPage } from "@/components/projects/project-detail-page";

export const metadata: Metadata = {
  title: "Proje",
};

export default async function Page({ params }: { params: Promise<{ projectId: string }> }) {
  const { projectId } = await params;

  return (
    <Suspense>
      <ProjectDetailPage projectId={projectId} />
    </Suspense>
  );
}
