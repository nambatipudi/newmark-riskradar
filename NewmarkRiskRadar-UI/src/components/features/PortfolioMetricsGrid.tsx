import { AlertTriangle, CalendarClock, Gauge, Landmark } from "lucide-react";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { PanelError } from "@/components/ui/panel-error";
import { fetchPortfolioSummary } from "@/services/apiClient";
import { load } from "@/lib/api/load";
import type { TriageQuery } from "@/lib/api/params";
import { formatCompactCurrency, formatPercent, formatRatio, formatYears } from "@/lib/utils";

interface PortfolioMetricsGridProps {
  query: TriageQuery;
}

export async function PortfolioMetricsGrid({ query }: PortfolioMetricsGridProps) {
  const result = await load(() => fetchPortfolioSummary(query));

  if (!result.ok) {
    return <PanelError title="Portfolio metrics" message={result.message} detail={result.detail} />;
  }

  const summary = result.data;
  const stressed = summary.appliedStressRate != null;
  const coverage = stressed ? summary.stressedWeightedAverageDscr : summary.weightedAverageDscr;

  const metrics = [
    {
      label: "Total servicing volume",
      value: formatCompactCurrency(summary.totalServicingVolume),
      description: `${summary.loanCount} loans under management`,
      icon: Landmark,
      tone: "text-foreground",
    },
    {
      label: stressed ? "Stressed weighted DSCR" : "Weighted average DSCR",
      value: formatRatio(coverage),
      description: stressed
        ? `In place ${formatRatio(summary.weightedAverageDscr)} at ${formatPercent(summary.appliedStressRate, 1)} take-out`
        : `WALT ${formatYears(summary.weightedAverageLoanTermYears)}`,
      icon: Gauge,
      tone: (coverage ?? 0) < 1.25 ? "text-warning" : "text-success",
    },
    {
      label: "Critical default exposure",
      value: formatCompactCurrency(summary.criticalDefaultExposure),
      description: `${formatPercent(summary.criticalDefaultExposureShare)} of book across ${summary.criticalLoanCount} loans`,
      icon: AlertTriangle,
      tone: "text-destructive",
    },
    {
      label: "Maturing within 12 months",
      value: formatCompactCurrency(summary.maturingWithinTwelveMonths),
      description: `${summary.warningLoanCount} on watch, ${summary.performingLoanCount} performing`,
      icon: CalendarClock,
      tone: "text-warning",
    },
  ];

  return (
    <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      {metrics.map((metric) => (
        <Card key={metric.label}>
          <CardHeader className="flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle>{metric.label}</CardTitle>
            <metric.icon className={`h-4 w-4 ${metric.tone}`} aria-hidden />
          </CardHeader>
          <CardContent>
            <p className={`text-3xl font-semibold tracking-tight ${metric.tone}`}>{metric.value}</p>
            <CardDescription className="mt-1">{metric.description}</CardDescription>
          </CardContent>
        </Card>
      ))}
    </section>
  );
}
