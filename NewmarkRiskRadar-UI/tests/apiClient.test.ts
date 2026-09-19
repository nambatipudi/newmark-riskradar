import { beforeEach, describe, expect, it, vi } from "vitest";

import { ApiError, fetchPortfolioSummary, fetchTriagedLoans, syncServicingTape } from "@/services/apiClient";
import type { TriageQuery } from "@/lib/api/params";

const fetchMock = vi.fn();

const inPlace: TriageQuery = {
  risk: "all",
  assetClass: "all",
  stressRate: null,
  search: "",
  page: 1,
};

const validSummary = {
  asOfDate: "2026-06-15",
  appliedStressRate: null,
  loanCount: 50,
  totalServicingVolume: 2_594_157_494.7,
  weightedAverageDscr: 1.2704,
  stressedWeightedAverageDscr: null,
  weightedAverageLoanTermYears: 3.0317,
  criticalDefaultExposure: 1_100_976_659.5,
  criticalDefaultExposureShare: 0.4244,
  warningExposure: 939_614_112.7,
  performingExposure: 553_566_722.5,
  criticalLoanCount: 21,
  warningLoanCount: 18,
  performingLoanCount: 11,
  maturingWithinTwelveMonths: 818_102_771.4,
};

function respondWith(body: unknown, init: { ok?: boolean; status?: number } = {}) {
  fetchMock.mockResolvedValueOnce({
    ok: init.ok ?? true,
    status: init.status ?? 200,
    json: async () => body,
  });
}

function requestedUrl(): string {
  return fetchMock.mock.calls[0][0] as string;
}

beforeEach(() => {
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
});

describe("fetchPortfolioSummary", () => {
  it("returns the parsed payload", async () => {
    respondWith(validSummary);

    const summary = await fetchPortfolioSummary(inPlace);

    expect(summary.loanCount).toBe(50);
    expect(requestedUrl()).toBe("/api/v1/portfolio/summary");
  });

  it("passes the stress scenario through to the API", async () => {
    respondWith({ ...validSummary, appliedStressRate: 0.095 });

    await fetchPortfolioSummary({ ...inPlace, stressRate: 9.5 });

    expect(requestedUrl()).toBe("/api/v1/portfolio/summary?stressRate=9.5");
  });

  it("never caches, so every scenario is recomputed server side", async () => {
    respondWith(validSummary);

    await fetchPortfolioSummary(inPlace);

    expect(fetchMock.mock.calls[0][1]).toMatchObject({ next: { revalidate: 0 } });
  });

  it("raises an ApiError when the API returns a payload that fails validation", async () => {
    respondWith({ ...validSummary, loanCount: "fifty" });

    const error = await fetchPortfolioSummary(inPlace).catch((cause: unknown) => cause);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).message).toMatch(/unexpected payload/i);
    expect((error as ApiError).detail).toContain("loanCount");
  });

  it("surfaces the problem document from a 400", async () => {
    respondWith(
      {
        title: "Invalid financial input.",
        detail: "Stress rate must be between 1% and 25%, but was 80%.",
        status: 400,
        traceId: "00-abc-123",
      },
      { ok: false, status: 400 },
    );

    const error = await fetchPortfolioSummary(inPlace).catch((cause: unknown) => cause);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(400);
    expect((error as ApiError).message).toBe("Invalid financial input.");
    expect((error as ApiError).detail).toBe("Stress rate must be between 1% and 25%, but was 80%.");
    expect((error as ApiError).traceId).toBe("00-abc-123");
  });

  it("falls back to a status message when the error body is not a problem document", async () => {
    respondWith("<html>502</html>", { ok: false, status: 502 });

    const error = await fetchPortfolioSummary(inPlace).catch((cause: unknown) => cause);

    expect((error as ApiError).message).toBe("Request failed with status 502.");
  });

  it("reports an unreachable API rather than leaking the transport error", async () => {
    fetchMock.mockRejectedValueOnce(new Error("ECONNREFUSED"));

    const error = await fetchPortfolioSummary(inPlace).catch((cause: unknown) => cause);

    expect((error as ApiError).status).toBe(0);
    expect((error as ApiError).message).toBe("Could not reach the RiskRadar API.");
    expect((error as ApiError).detail).toBe("ECONNREFUSED");
  });
});

describe("fetchTriagedLoans", () => {
  const emptyPage = { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0, hasNextPage: false };

  it("always requests an explicit page and page size", async () => {
    respondWith(emptyPage);

    await fetchTriagedLoans(inPlace);

    expect(requestedUrl()).toBe("/api/v1/loans/triage?page=1&pageSize=10");
  });

  it("omits filters that are set to all or blank", async () => {
    respondWith(emptyPage);

    await fetchTriagedLoans({ ...inPlace, risk: "all", assetClass: "all", search: "" });

    expect(requestedUrl()).not.toContain("risk=");
    expect(requestedUrl()).not.toContain("assetClass=");
    expect(requestedUrl()).not.toContain("search=");
  });

  it("forwards every active filter", async () => {
    respondWith(emptyPage);

    await fetchTriagedLoans({
      risk: "Critical",
      assetClass: "Office",
      stressRate: 8.5,
      search: "Miami",
      page: 3,
    });

    const url = requestedUrl();
    expect(url).toContain("stressRate=8.5");
    expect(url).toContain("page=3");
    expect(url).toContain("risk=Critical");
    expect(url).toContain("assetClass=Office");
    expect(url).toContain("search=Miami");
  });
});

describe("syncServicingTape", () => {
  it("POSTs and returns the validated result", async () => {
    respondWith({
      message: "Servicing tape successfully synced.",
      recordsInserted: 50,
      syncedAtUtc: "2026-09-19T02:51:39.694823+00:00",
    });

    const result = await syncServicingTape();

    expect(result.recordsInserted).toBe(50);
    expect(requestedUrl()).toBe("/api/v1/system/sync");
    expect(fetchMock.mock.calls[0][1]).toMatchObject({ method: "POST" });
  });

  it("raises an ApiError on a 409 lock conflict", async () => {
    respondWith(
      { title: "Servicing tape is locked.", status: 409 },
      { ok: false, status: 409 },
    );

    const error = await syncServicingTape().catch((cause: unknown) => cause);

    expect((error as ApiError).status).toBe(409);
    expect((error as ApiError).message).toBe("Servicing tape is locked.");
  });
});
