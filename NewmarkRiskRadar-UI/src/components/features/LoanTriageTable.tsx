import Link from "next/link";
import { ChevronLeft, ChevronRight } from "lucide-react";

import { LoanTriageRows } from "@/components/features/LoanTriageRows";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { PanelError } from "@/components/ui/panel-error";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { toSearchString, type TriageQuery } from "@/lib/api/params";
import { load } from "@/lib/api/load";
import { fetchTriagedLoans } from "@/services/apiClient";
import { cn } from "@/lib/utils";

interface LoanTriageTableProps {
  query: TriageQuery;
}

export async function LoanTriageTable({ query }: LoanTriageTableProps) {
  const result = await load(() => fetchTriagedLoans(query));

  if (!result.ok) {
    return <PanelError title="Loan triage" message={result.message} detail={result.detail} />;
  }

  const page = result.data;
  const stressed = query.stressRate != null;

  const from = page.totalCount === 0 ? 0 : (page.page - 1) * page.pageSize + 1;
  const to = Math.min(page.page * page.pageSize, page.totalCount);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-semibold text-foreground">Loan triage</CardTitle>
        <CardDescription>
          {page.totalCount === 0
            ? "No loans match the current stress test parameters."
            : `Showing ${from} to ${to} of ${page.totalCount} loans${stressed ? ` at a ${query.stressRate?.toFixed(1)}% take-out rate` : ""}. Select a row for the full position.`}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Loan ID</TableHead>
              <TableHead>Asset class</TableHead>
              <TableHead className="text-right">In-place NOI</TableHead>
              <TableHead className="text-right">Months to maturity</TableHead>
              <TableHead className="text-right">{stressed ? "Stressed DSCR" : "DSCR"}</TableHead>
              <TableHead>Risk rating</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {page.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="py-12 text-center">
                  <p className="text-sm font-medium text-foreground">
                    No loans match the current stress test parameters.
                  </p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    Widen the asset class filter or lower the pro-forma rate.
                  </p>
                </TableCell>
              </TableRow>
            ) : (
              <LoanTriageRows loans={page.items} stressed={stressed} />
            )}
          </TableBody>
        </Table>

        {page.totalPages > 1 ? (
          <nav className="mt-4 flex items-center justify-between" aria-label="Triage pagination">
            <p className="text-xs text-muted-foreground">
              Page {page.page} of {page.totalPages}
            </p>
            <div className="flex gap-2">
              <PageLink query={query} page={page.page - 1} disabled={page.page <= 1}>
                <ChevronLeft className="h-4 w-4" aria-hidden />
                Previous
              </PageLink>
              <PageLink query={query} page={page.page + 1} disabled={!page.hasNextPage}>
                Next
                <ChevronRight className="h-4 w-4" aria-hidden />
              </PageLink>
            </div>
          </nav>
        ) : null}
      </CardContent>
    </Card>
  );
}

function PageLink({
  query,
  page,
  disabled,
  children,
}: {
  query: TriageQuery;
  page: number;
  disabled: boolean;
  children: React.ReactNode;
}) {
  const className =
    "inline-flex h-8 items-center gap-1 rounded-md border border-border px-3 text-xs font-medium";

  if (disabled) {
    return (
      <span className={cn(className, "pointer-events-none opacity-50")} aria-disabled>
        {children}
      </span>
    );
  }

  const search = toSearchString({ ...query, page });

  return (
    <Link
      href={search ? `/?${search}` : "/"}
      scroll={false}
      className={cn(className, "hover:bg-accent")}
    >
      {children}
    </Link>
  );
}
