import { ApiError } from "@/services/apiClient";

export type LoadResult<T> =
  | { ok: true; data: T }
  | { ok: false; message: string; detail?: string };

/**
 * Turns a failed panel fetch into a value instead of a thrown error. React redacts errors thrown
 * during a server render before they reach the client, so catching here is what preserves a
 * message the analyst can act on.
 */
export async function load<T>(fetcher: () => Promise<T>): Promise<LoadResult<T>> {
  try {
    return { ok: true, data: await fetcher() };
  } catch (cause) {
    if (cause instanceof ApiError) {
      return { ok: false, message: cause.message, detail: cause.detail };
    }

    throw cause;
  }
}
