import { AlertTriangle } from "lucide-react";

import { Card, CardContent } from "@/components/ui/card";

interface PanelErrorProps {
  title: string;
  message: string;
  detail?: string;
}

export function PanelError({ title, message, detail }: PanelErrorProps) {
  return (
    <Card className="border-destructive/40">
      <CardContent className="flex items-start gap-3 p-6">
        <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-destructive" aria-hidden />
        <div className="min-w-0">
          <p className="text-sm font-semibold text-destructive">{title} is unavailable.</p>
          <p className="mt-1 text-sm text-muted-foreground">{message}</p>
          {detail ? (
            <pre className="mt-2 overflow-auto rounded bg-muted p-2 text-xs text-muted-foreground">
              {detail}
            </pre>
          ) : null}
        </div>
      </CardContent>
    </Card>
  );
}
