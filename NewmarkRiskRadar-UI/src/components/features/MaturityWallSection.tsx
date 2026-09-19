import { MaturityWallChart } from "@/components/features/MaturityWallChart";
import { PanelError } from "@/components/ui/panel-error";
import { fetchMaturityWall } from "@/services/apiClient";
import { load } from "@/lib/api/load";
import type { TriageQuery } from "@/lib/api/params";

interface MaturityWallSectionProps {
  query: TriageQuery;
}

/** Server half of the chart: fetches on the server so Recharts never ships a data loader. */
export async function MaturityWallSection({ query }: MaturityWallSectionProps) {
  const result = await load(() => fetchMaturityWall(query));

  if (!result.ok) {
    return <PanelError title="Maturity wall" message={result.message} detail={result.detail} />;
  }

  return <MaturityWallChart buckets={result.data} stressRate={query.stressRate} />;
}
