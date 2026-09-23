import { useCallback, useEffect, useState, type DependencyList, type Dispatch, type SetStateAction } from 'react';
import { errorMessage } from './format';

export interface LoadState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
  reload: () => void;
  setData: Dispatch<SetStateAction<T | null>>;
}

/**
 * Runs an async loader whenever `deps` change. The previous data stays visible while the next request runs,
 * so filtering does not flash an empty screen.
 */
export function useLoad<T>(loader: () => Promise<T>, deps: DependencyList): LoadState<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tick, setTick] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    loader()
      .then((result) => {
        if (!cancelled) setData(result);
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(errorMessage(err));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, tick]);

  const reload = useCallback(() => setTick((value) => value + 1), []);
  return { data, loading, error, reload, setData };
}

export function useDebounced<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(timer);
  }, [value, delayMs]);
  return debounced;
}
