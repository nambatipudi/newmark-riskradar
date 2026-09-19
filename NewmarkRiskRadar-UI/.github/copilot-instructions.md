# Newmark Risk Radar - UI Constraints

## Next.js App Router Architecture
- Default to React Server Components (RSC) for all data fetching and layout wrappers.
- Use `"use client"` strictly at the interactive leaf nodes (e.g., Recharts graphs, dropdown filters, form inputs).
- Use native `fetch` inside Server Components with `next: { revalidate: 0 }` for real-time triage data. Do not use SWR or React Query.

## State Management
- Bind all triage filtering state (Asset Class, Risk Rating) directly to URL SearchParams using `useRouter` and `useSearchParams`. Do not use Zustand, Redux, or Context for table filters.

## Data Contracts & Type Safety
- Never trust the API payload blindly. 
- Define Zod schemas for all backend DTOs in `src/lib/api/schemas.ts`.
- Wrap all `fetch` calls in a utility that parses the JSON through the corresponding Zod schema before returning it to the React component.
- Enforce strict TypeScript (no `any`, `unknown`, or non-null assertions).

## Containerization
- Build for Next.js standalone mode (`output: 'standalone'` in next.config).