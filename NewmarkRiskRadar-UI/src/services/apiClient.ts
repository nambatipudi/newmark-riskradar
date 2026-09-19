import type { z } from "zod";

import {
  loanTriagePageSchema,
  maturityWallSchema,
  portfolioSummarySchema,
  problemDocumentSchema,
  tapeSyncResultSchema,
} from "@/lib/api/schemas";
import { TRIAGE_PAGE_SIZE, type TriageQuery } from "@/lib/api/params";

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly detail?: string,
    readonly traceId?: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

/**
 * Server renders talk to the API container directly; the browser goes through the
 * same-origin /api rewrite so no API host is ever baked into the client bundle.
 */
function resolveBaseUrl(): string {
  if (typeof window === "undefined") {
    const origin = process.env.API_PROXY_TARGET ?? "http://localhost:5000";
    return `${origin.replace(/\/$/, "")}/api/v1`;
  }

  return process.env.NEXT_PUBLIC_API_BASE_URL ?? "/api/v1";
}

/** Fetches, then parses through the matching Zod schema before anything reaches a component. */
async function request<TSchema extends z.ZodType>(
  path: string,
  schema: TSchema,
  init?: Pick<RequestInit, "method">,
): Promise<z.infer<TSchema>> {
  const url = `${resolveBaseUrl()}${path}`;

  let response: Response;
  try {
    response = await fetch(url, {
      ...init,
      headers: { Accept: "application/json" },
      next: { revalidate: 0 },
    });
  } catch (cause) {
    throw new ApiError(
      "Could not reach the RiskRadar API.",
      0,
      cause instanceof Error ? cause.message : undefined,
    );
  }

  const body = await response.json().catch(() => null);

  if (!response.ok) {
    const problem = problemDocumentSchema.safeParse(body);

    throw new ApiError(
      problem.success && problem.data.title
        ? problem.data.title
        : `Request failed with status ${response.status}.`,
      response.status,
      problem.success ? (problem.data.detail ?? undefined) : undefined,
      problem.success ? problem.data.traceId : undefined,
    );
  }

  const parsed = schema.safeParse(body);

  if (!parsed.success) {
    throw new ApiError(
      "The RiskRadar API returned an unexpected payload.",
      response.status,
      parsed.error.issues.map((issue) => `${issue.path.join(".")}: ${issue.message}`).join("; "),
    );
  }

  return parsed.data;
}

function scenarioParams(query: TriageQuery): URLSearchParams {
  const params = new URLSearchParams();

  if (query.stressRate != null) {
    params.set("stressRate", query.stressRate.toFixed(1));
  }

  return params;
}

export function fetchPortfolioSummary(query: TriageQuery) {
  const params = scenarioParams(query).toString();
  return request(`/portfolio/summary${params ? `?${params}` : ""}`, portfolioSummarySchema);
}

export function fetchMaturityWall(query: TriageQuery) {
  const params = scenarioParams(query).toString();
  return request(`/analytics/maturity-wall${params ? `?${params}` : ""}`, maturityWallSchema);
}

export function fetchTriagedLoans(query: TriageQuery) {
  const params = scenarioParams(query);
  params.set("page", String(query.page));
  params.set("pageSize", String(TRIAGE_PAGE_SIZE));

  if (query.risk !== "all") {
    params.set("risk", query.risk);
  }

  if (query.assetClass !== "all") {
    params.set("assetClass", query.assetClass);
  }

  if (query.search) {
    params.set("search", query.search);
  }

  return request(`/loans/triage?${params.toString()}`, loanTriagePageSchema);
}

/** Simulates a servicing tape drop: the API clears the book and writes a fresh batch of loans. */
export function syncServicingTape() {
  return request("/system/sync", tapeSyncResultSchema, { method: "POST" });
}
