import { beforeEach, describe, expect, it, vi } from "vitest";
import { send } from "@/lib/api/http";
import { jsonResponse, requestHeader } from "@/test/fixtures";

const fetchMock = vi.fn<typeof fetch>();

beforeEach(() => {
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
});

describe("send", () => {
  it("sends form data as is and lets the browser set the content type", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ id: "1" }));
    const form = new FormData();
    form.append("file", new Blob(["hi"]), "note.txt");

    await send("/api/upload", { method: "POST", body: form });

    const init = fetchMock.mock.calls[0][1] as RequestInit;
    expect(init.body).toBe(form);
    expect(requestHeader(fetchMock.mock.calls[0], "Content-Type")).toBeNull();
  });

  it("serializes other bodies as JSON", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({}));

    await send("/api/items", { method: "POST", body: { name: "x" } });

    expect((fetchMock.mock.calls[0][1] as RequestInit).body).toBe(JSON.stringify({ name: "x" }));
    expect(requestHeader(fetchMock.mock.calls[0], "Content-Type")).toBe("application/json");
  });

  it("returns a blob when asked to", async () => {
    fetchMock.mockResolvedValueOnce(new Response("file-content", { headers: { "Content-Type": "text/plain" } }));

    const blob = await send<Blob>("/api/file", { responseType: "blob" });

    await expect(blob.text()).resolves.toBe("file-content");
  });
});
