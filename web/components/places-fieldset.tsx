'use client';

import { useEffect, useId, useRef, useState } from 'react';
import { Api } from '@/lib/api';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { cn } from '@/lib/utils';

export type PlaceFields = { city?: string; state?: string; zip?: string };

type OdRow = { city: string; stateCode: string };

function normState(s?: string) {
  return (s || '').trim().toUpperCase();
}

function useDebounced<T>(value: T, ms: number): T {
  const [d, setD] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setD(value), ms);
    return () => clearTimeout(t);
  }, [value, ms]);
  return d;
}

/** City autocomplete + read-only state; same `/locations/places` list used for origin and destination (legacy MVC parity). */
function CityStateAutocomplete({
  idPrefix,
  label,
  city,
  onPick,
  onCityChange,
}: {
  idPrefix: string;
  label: string;
  city: string;
  onPick: (row: OdRow) => void;
  onCityChange: (v: string) => void;
}) {
  const wrapRef = useRef<HTMLDivElement>(null);
  const debouncedQ = useDebounced((city || '').trim(), 140);
  const [open, setOpen] = useState(false);
  const [rows, setRows] = useState<OdRow[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const ac = new AbortController();
    if (debouncedQ.length < 1) {
      setRows([]);
      setLoading(false);
      return;
    }
    setLoading(true);
    Api.locationsPlaces(debouncedQ, 100, { signal: ac.signal })
      .then((list) =>
        setRows(
          Array.isArray(list)
            ? list.map((x: any) => ({
                city: String(x.city || '').trim(),
                stateCode: normState(x.stateCode),
              }))
            : [],
        ),
      )
      .catch(() => setRows([]))
      .finally(() => setLoading(false));
    return () => ac.abort();
  }, [debouncedQ]);

  useEffect(() => {
    function onDoc(e: MouseEvent) {
      const el = wrapRef.current;
      if (!el || !open) return;
      if (e.target instanceof Node && !el.contains(e.target)) setOpen(false);
    }
    document.addEventListener('mousedown', onDoc);
    return () => document.removeEventListener('mousedown', onDoc);
  }, [open]);

  return (
    <div ref={wrapRef} className={cn('space-y-2', 'relative')}>
      <Label htmlFor={idPrefix}>{label}</Label>
      <Input
        id={idPrefix}
        autoComplete="off"
        spellCheck={false}
        className="uppercase font-medium"
        placeholder="Search city…"
        value={city}
        onFocus={() => setOpen(true)}
        onChange={(e) => {
          onCityChange(e.target.value.toUpperCase());
          setOpen(true);
        }}
        onKeyDown={(e) => {
          if (e.key === 'Escape') setOpen(false);
        }}
      />
      {open && debouncedQ.length >= 1 && (rows.length > 0 || loading) && (
        <ul
          className="absolute z-50 mt-1 max-h-56 w-full overflow-y-auto rounded-md border bg-background shadow-md"
          role="listbox"
        >
          {loading && rows.length === 0 ? (
            <li className="px-3 py-2 text-sm text-muted-foreground">Searching…</li>
          ) : (
            rows.map((r) => (
              <li key={`${r.city}|${r.stateCode}`} role="presentation">
                <button
                  type="button"
                  className="flex w-full flex-col items-start gap-0 px-3 py-2 text-left hover:bg-accent"
                  onMouseDown={(e) => {
                    e.preventDefault();
                    onPick(r);
                    setOpen(false);
                  }}
                >
                  <span className="text-sm font-semibold leading-tight">{r.city.toUpperCase()}</span>
                  <span className="text-xs leading-tight text-muted-foreground">{r.stateCode}</span>
                </button>
              </li>
            ))
          )}
        </ul>
      )}
    </div>
  );
}

/**
 * Legacy load-board layout: origin city + readonly state | destination city + readonly state (no ZIP).
 * Both sides use one shared OriginDestination-derived suggestion list via {@link Api.locationsPlaces}.
 */
export function PlacesFieldset({
  origin,
  destination,
  onOrigin,
  onDestination,
}: {
  origin: PlaceFields;
  destination: PlaceFields;
  onOrigin: (patch: Partial<PlaceFields>) => void;
  onDestination: (patch: Partial<PlaceFields>) => void;
}) {
  const idBase = useId().replace(/:/g, '');
  const oCity = origin.city || '';
  const oState = normState(origin.state);
  const dCity = destination.city || '';
  const dState = normState(destination.state);

  return (
    <div className="rounded-md border p-4">
      <div className="grid grid-cols-1 gap-4 md:grid-cols-12 md:gap-3 md:items-end">
        <div className="md:col-span-4">
          <CityStateAutocomplete
            idPrefix={`${idBase}-origin-city`}
            label="Origin city"
            city={oCity}
            onCityChange={(v) => onOrigin({ city: v })}
            onPick={(r) =>
              onOrigin({
                city: r.city.toUpperCase(),
                state: r.stateCode,
              })
            }
          />
        </div>
        <div className="md:col-span-2">
          <div className="space-y-2">
            <Label htmlFor={`${idBase}-origin-state`}>Origin state</Label>
            <Input
              id={`${idBase}-origin-state`}
              readOnly
              tabIndex={-1}
              required
              className="uppercase font-medium bg-muted/50"
              value={oState}
            />
          </div>
        </div>
        <div className="md:col-span-4">
          <CityStateAutocomplete
            idPrefix={`${idBase}-dest-city`}
            label="Destination city"
            city={dCity}
            onCityChange={(v) => onDestination({ city: v })}
            onPick={(r) =>
              onDestination({
                city: r.city.toUpperCase(),
                state: r.stateCode,
              })
            }
          />
        </div>
        <div className="md:col-span-2">
          <div className="space-y-2">
            <Label htmlFor={`${idBase}-dest-state`}>Destination state</Label>
            <Input
              id={`${idBase}-dest-state`}
              readOnly
              tabIndex={-1}
              required
              className="uppercase font-medium bg-muted/50"
              value={dState}
            />
          </div>
        </div>
      </div>
    </div>
  );
}
