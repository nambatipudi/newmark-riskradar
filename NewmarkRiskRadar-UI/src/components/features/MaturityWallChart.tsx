"use client";

import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { formatCompactCurrency, formatPercent } from "@/lib/utils";
import type { MaturityWallBucket } from "@/types/api";

interface MaturityWallChartProps {
  buckets: MaturityWallBucket[];
  stressRate: number | null;
}

const SERIES = [
  { key: "criticalBalance", label: "Critical", color: "hsl(var(--destructive))" },
  { key: "warningBalance", label: "Warning", color: "hsl(var(--warning))" },
  { key: "performingBalance", label: "Performing", color: "hsl(var(--success))" },
] as const;

export function MaturityWallChart({ buckets, stressRate }: MaturityWallChartProps) {
  const peak = buckets.reduce<MaturityWallBucket | null>(
    (highest, bucket) => (highest === null || bucket.totalBalance > highest.totalBalance ? bucket : highest),
    null,
  );

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <CardTitle className="text-base font-semibold text-foreground">Maturity wall</CardTitle>
            <CardDescription>
              {stressRate == null
                ? "Outstanding balance rolling by calendar year, stacked by triage bucket."
                : `Outstanding balance rolling by calendar year, triaged at a ${stressRate.toFixed(1)}% take-out rate.`}
            </CardDescription>
          </div>
          <div className="flex flex-wrap gap-4">
            {SERIES.map((series) => (
              <span key={series.key} className="flex items-center gap-2 text-xs text-muted-foreground">
                <span className="h-2.5 w-2.5 rounded-sm" style={{ backgroundColor: series.color }} />
                {series.label}
              </span>
            ))}
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <div className="h-[320px] w-full">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={buckets} margin={{ top: 8, right: 8, bottom: 8, left: 8 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
              <XAxis
                dataKey="year"
                stroke="hsl(var(--muted-foreground))"
                tickLine={false}
                axisLine={false}
                fontSize={12}
              />
              <YAxis
                stroke="hsl(var(--muted-foreground))"
                tickLine={false}
                axisLine={false}
                fontSize={12}
                width={64}
                tickFormatter={(value: number) => formatCompactCurrency(value)}
              />
              <Tooltip cursor={{ fill: "hsl(var(--muted) / 0.4)" }} content={<MaturityTooltip />} />
              {SERIES.map((series) => (
                <Bar
                  key={series.key}
                  dataKey={series.key}
                  name={series.label}
                  stackId="maturity"
                  fill={series.color}
                  radius={series.key === "performingBalance" ? [4, 4, 0, 0] : undefined}
                />
              ))}
            </BarChart>
          </ResponsiveContainer>
        </div>
        {peak ? (
          <p className="mt-4 text-xs text-muted-foreground">
            Heaviest rollover year is {peak.year} at {formatCompactCurrency(peak.totalBalance)} across{" "}
            {peak.loanCount} loans, {formatPercent(peak.shareOfPortfolio)} of the book.
          </p>
        ) : null}
      </CardContent>
    </Card>
  );
}

interface TooltipPayloadEntry {
  name?: string;
  value?: number;
  color?: string;
  payload?: MaturityWallBucket;
}

function MaturityTooltip({
  active,
  payload,
  label,
}: {
  active?: boolean;
  payload?: TooltipPayloadEntry[];
  label?: string | number;
}) {
  if (!active || !payload?.length) {
    return null;
  }

  const bucket = payload[0]?.payload;

  return (
    <div className="rounded-md border border-border bg-card p-3">
      <p className="text-sm font-semibold text-foreground">{label}</p>
      {bucket ? (
        <p className="mb-2 text-xs text-muted-foreground">
          {formatCompactCurrency(bucket.totalBalance)} across {bucket.loanCount} loans
        </p>
      ) : null}
      <ul className="space-y-1">
        {payload.map((entry) => (
          <li key={entry.name} className="flex items-center justify-between gap-6 text-xs">
            <span className="flex items-center gap-2 text-muted-foreground">
              <span className="h-2 w-2 rounded-sm" style={{ backgroundColor: entry.color }} />
              {entry.name}
            </span>
            <span className="font-medium text-foreground">{formatCompactCurrency(entry.value ?? 0)}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
