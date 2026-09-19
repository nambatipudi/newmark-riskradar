"use client";

import { Component, type ReactNode } from "react";
import { AlertTriangle } from "lucide-react";

import { Card, CardContent } from "@/components/ui/card";

interface PanelBoundaryProps {
  title: string;
  children: ReactNode;
}

interface PanelBoundaryState {
  message: string | null;
}

/**
 * Keeps one failing panel from taking down the dashboard: a streamed server component that throws
 * is caught here, so the remaining Suspense boundaries still resolve.
 */
export class PanelBoundary extends Component<PanelBoundaryProps, PanelBoundaryState> {
  state: PanelBoundaryState = { message: null };

  static getDerivedStateFromError(error: unknown): PanelBoundaryState {
    return { message: error instanceof Error ? error.message : "Unknown error." };
  }

  render() {
    if (this.state.message === null) {
      return this.props.children;
    }

    return (
      <Card className="border-destructive/40">
        <CardContent className="flex items-start gap-3 p-6">
          <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-destructive" aria-hidden />
          <div>
            <p className="text-sm font-semibold text-destructive">
              {this.props.title} is unavailable.
            </p>
            <p className="mt-1 text-sm text-muted-foreground">{this.state.message}</p>
            <button
              type="button"
              className="mt-3 text-xs text-muted-foreground underline underline-offset-2 hover:text-foreground"
              onClick={() => this.setState({ message: null })}
            >
              Retry
            </button>
          </div>
        </CardContent>
      </Card>
    );
  }
}
