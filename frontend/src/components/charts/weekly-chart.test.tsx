import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ticksFor, WeeklyChart } from "@/components/charts/weekly-chart";

const POINTS = [
  { weekStart: "2026-10-05", created: 4, completed: 1 },
  { weekStart: "2026-10-12", created: 2, completed: 3 },
];

describe("WeeklyChart", () => {
  it("describes every week for assistive technology", () => {
    render(<WeeklyChart points={POINTS} />);

    expect(screen.getByRole("listitem", { name: "5 Eki haftası: 4 oluşturulan, 1 tamamlanan" })).toBeInTheDocument();
  });

  it("shows a tooltip on focus", () => {
    render(<WeeklyChart points={POINTS} />);

    fireEvent.focus(screen.getByRole("listitem", { name: /12 Eki haftası/ }));

    const tooltip = screen.getByRole("tooltip");
    expect(tooltip).toHaveTextContent("12 Eki haftası");
    expect(tooltip).toHaveTextContent("2Oluşturulan");
    expect(tooltip).toHaveTextContent("3Tamamlanan");
  });

  it("picks round axis steps close to the data", () => {
    expect(ticksFor(0)).toEqual([0, 1]);
    expect(ticksFor(5)).toEqual([0, 2, 4, 6]);
    expect(ticksFor(9)).toEqual([0, 5, 10]);
    expect(ticksFor(42)).toEqual([0, 20, 40, 60]);
  });
});
