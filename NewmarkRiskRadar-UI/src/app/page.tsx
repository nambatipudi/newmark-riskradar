import { Suspense } from "react";

import { LoanTriageTable } from "@/components/features/LoanTriageTable";
import { MaturityWallSection } from "@/components/features/MaturityWallSection";
import { PortfolioMetricsGrid } from "@/components/features/PortfolioMetricsGrid";
import { StressTestControls } from "@/components/features/StressTestControls";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { PanelBoundary } from "@/components/ui/panel-boundary";
import { Skeleton } from "@/components/ui/skeleton";
import { parseTriageQuery, toSearchString, type RawSearchParams } from "@/lib/api/params";

export const dynamic = "force-dynamic";

interface DashboardPageProps {
  searchParams: Promise<RawSearchParams>;
}

export default async function DashboardPage({ searchParams }: DashboardPageProps) {
  const query = parseTriageQuery(await searchParams);

  // Suspense keys are the scenario itself, so changing the slider re-suspends each panel
  // independently instead of blanking the whole dashboard.
  const scenarioKey = toSearchString(query) || "in-place";

  return (
    <>
      <header className="space-y-1">
        <h1 className="text-2xl font-semibold tracking-tight">Portfolio Risk Dashboard</h1>
        <p className="text-sm text-muted-foreground">
          Refinancing stress test across the commercial real estate servicing book.
        </p>
      </header>

      <StressTestControls query={query} />

      <div className="grid gap-6">
        <PanelBoundary title="Portfolio metrics">
          <Suspense key={`metrics-${scenarioKey}`} fallback={<MetricsSkeleton />}>
            <PortfolioMetricsGrid query={query} />
          </Suspense>
        </PanelBoundary>

        <PanelBoundary title="Maturity wall">
          <Suspense key={`chart-${scenarioKey}`} fallback={<ChartSkeleton />}>
            <MaturityWallSection query={query} />
          </Suspense>
        </PanelBoundary>

        <PanelBoundary title="Loan triage">
          <Suspense key={`table-${scenarioKey}`} fallback={<TableSkeleton />}>
            <LoanTriageTable query={query} />
          </Suspense>
        </PanelBoundary>
      </div>
    </>
  );
}

function MetricsSkeleton() {
  return (
    <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4" aria-busy>
      {Array.from({ length: 4 }).map((_, index) => (
        <Card key={index}>
          <CardHeader className="pb-2">
            <Skeleton className="h-4 w-32" />
          </CardHeader>
          <CardContent className="space-y-2">
            <Skeleton className="h-8 w-24" />
            <Skeleton className="h-3 w-40" />
          </CardContent>
        </Card>
      ))}
    </section>
  );
}

function ChartSkeleton() {
  return (
    <Card aria-busy>
      <CardHeader className="space-y-2">
        <Skeleton className="h-4 w-32" />
        <Skeleton className="h-3 w-72" />
      </CardHeader>
      <CardContent>
        <Skeleton className="h-[320px] w-full" />
      </CardContent>
    </Card>
  );
}

function TableSkeleton() {
  return (
    <Card aria-busy>
      <CardHeader className="space-y-2">
        <Skeleton className="h-4 w-28" />
        <Skeleton className="h-3 w-56" />
      </CardHeader>
      <CardContent className="space-y-3">
        {Array.from({ length: 10 }).map((_, index) => (
          <Skeleton key={index} className="h-10 w-full" />
        ))}
      </CardContent>
    </Card>
  );
}
