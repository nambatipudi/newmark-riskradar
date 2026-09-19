"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Database, Loader2 } from "lucide-react";

import { Button } from "@/components/ui/button";
import { useToast } from "@/components/ui/toast";
import { ApiError, syncServicingTape } from "@/services/apiClient";

export function SyncTapeButton() {
  const [isSyncing, setIsSyncing] = useState(false);
  const [isRefreshing, startRefresh] = useTransition();
  const router = useRouter();
  const { toast } = useToast();

  async function handleSync() {
    setIsSyncing(true);

    try {
      const result = await syncServicingTape();

      toast({
        title: "Servicing Tape Synced",
        description: `${result.recordsInserted} records updated.`,
      });

      // Invalidates the RSC cache so every streamed panel refetches the new tape.
      startRefresh(() => router.refresh());
    } catch (cause) {
      toast({
        title: "Sync Failed",
        description:
          cause instanceof ApiError ? cause.message : "Could not reach the RiskRadar API.",
        variant: "destructive",
      });
    } finally {
      setIsSyncing(false);
    }
  }

  const busy = isSyncing || isRefreshing;

  return (
    <Button variant="outline" size="sm" disabled={busy} onClick={handleSync}>
      {busy ? (
        <Loader2 className="h-4 w-4 animate-spin" aria-hidden />
      ) : (
        <Database className="h-4 w-4" aria-hidden />
      )}
      {busy ? "Syncing…" : "Sync Tape"}
    </Button>
  );
}
