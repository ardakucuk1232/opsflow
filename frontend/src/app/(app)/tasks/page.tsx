import type { Metadata } from "next";
import { Suspense } from "react";
import { TasksPage } from "@/components/tasks/tasks-page";

export const metadata: Metadata = {
  title: "Görevler",
};

export default function Page() {
  return (
    <Suspense>
      <TasksPage />
    </Suspense>
  );
}
