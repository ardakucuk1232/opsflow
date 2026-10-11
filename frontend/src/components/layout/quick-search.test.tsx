import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { QuickSearch } from "@/components/layout/quick-search";
import type { SearchResult } from "@/types/api";

const mocks = vi.hoisted(() => ({ search: vi.fn(), push: vi.fn() }));

vi.mock("@/lib/api/search", () => ({ SEARCH_MIN_LENGTH: 2, searchApi: { search: mocks.search } }));
vi.mock("next/navigation", () => ({ useRouter: () => ({ push: mocks.push }) }));

const RESULT: SearchResult = {
  projects: [{ id: "p1", key: "PAY", name: "Ödeme sistemi", status: "Active" }],
  tasks: [{ id: "t1", projectId: "p1", projectName: "Ödeme sistemi", key: "PAY-4", title: "Ödeme sayfası", status: "InProgress" }],
};

beforeEach(() => {
  mocks.search.mockReset().mockResolvedValue(RESULT);
  mocks.push.mockReset();
});

describe("QuickSearch", () => {
  it("does not search for a single character", async () => {
    const user = userEvent.setup();
    render(<QuickSearch />);

    await user.type(screen.getByRole("combobox"), "ö");

    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
    expect(mocks.search).not.toHaveBeenCalled();
  });

  it("shows projects and tasks and opens the chosen one with the keyboard", async () => {
    const user = userEvent.setup();
    render(<QuickSearch />);

    await user.type(screen.getByRole("combobox"), "ödeme");

    expect(await screen.findByRole("option", { name: /Ödeme sayfası/ })).toBeInTheDocument();
    expect(mocks.search).toHaveBeenLastCalledWith("ödeme", expect.any(AbortSignal));

    await user.keyboard("{ArrowDown}{Enter}");

    expect(mocks.push).toHaveBeenCalledWith("/projects/p1?task=t1");
    expect(screen.getByRole("combobox")).toHaveValue("");
  });

  it("can open the full task list for the term", async () => {
    const user = userEvent.setup();
    render(<QuickSearch />);

    await user.type(screen.getByRole("combobox"), "rapor");
    await user.click(await screen.findByRole("option", { name: /tüm görevleri listele/ }));

    expect(mocks.push).toHaveBeenCalledWith("/tasks?q=rapor");
  });

  it("says so when nothing matches", async () => {
    const user = userEvent.setup();
    mocks.search.mockResolvedValue({ projects: [], tasks: [] });
    render(<QuickSearch />);

    await user.type(screen.getByRole("combobox"), "xyz");

    expect(await screen.findByText("Eşleşen proje veya görev yok.")).toBeInTheDocument();
  });

  it("focuses with Ctrl+K and closes with Escape", async () => {
    const user = userEvent.setup();
    render(<QuickSearch />);

    await user.keyboard("{Control>}k{/Control}");
    expect(screen.getByRole("combobox")).toHaveFocus();

    await user.type(screen.getByRole("combobox"), "ödeme");
    await screen.findByRole("listbox");
    await user.keyboard("{Escape}");

    await waitFor(() => expect(screen.queryByRole("listbox")).not.toBeInTheDocument());
  });
});
