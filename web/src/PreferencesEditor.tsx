import { useEffect, useState, type FormEvent } from "react";
import { ApiError } from "./api/http";
import {
  searchPlaces,
  setPreferences,
  type Place,
  type Preferences,
} from "./api/profile";

const message = (e: unknown) =>
  e instanceof ApiError ? e.message : "Något gick fel. Försök igen.";

/** Three decimals is about 100 m: plenty for a distance limit in km, and no more precise than it needs to be. */
const round = (degrees: number) => Math.round(degrees * 1000) / 1000;

/** Home by place name: suggestions while typing, and choosing one fills in the coordinates. */
function PlacePicker({ onPick }: { onPick: (place: Place) => void }) {
  const [text, setText] = useState("");
  const [suggestions, setSuggestions] = useState<Place[]>([]);
  const searching = text.trim().length >= 2;

  useEffect(() => {
    if (!searching) return;
    const controller = new AbortController();
    const timer = setTimeout(() => {
      searchPlaces(text, controller.signal).then(setSuggestions, () =>
        setSuggestions([]),
      );
    }, 250);
    return () => {
      clearTimeout(timer);
      controller.abort();
    };
  }, [text, searching]);

  const pick = (place: Place) => {
    setText("");
    setSuggestions([]);
    onPick(place);
  };

  return (
    <div>
      <label>
        Sök ort
        <input
          placeholder="t.ex. Kvissleby"
          value={text}
          onChange={(e) => setText(e.target.value)}
        />
      </label>
      {searching && suggestions.length > 0 && (
        <ul className="suggestions" aria-label="Ortsförslag">
          {suggestions.map((place) => (
            <li key={`${place.name}-${place.latitude}-${place.longitude}`}>
              <button type="button" onClick={() => pick(place)}>
                {place.name}, {place.municipality}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

/** Form state: numbers as typed, so a field can be empty while editing. */
type Draft = {
  acceptsFullTime: boolean;
  acceptsPartTime: boolean;
  excludeStaffingAgencies: boolean;
  excludeSeniorRoles: boolean;
  limitYears: boolean;
  maxRequiredYears: string;
  limitCommute: boolean;
  maxDistanceKm: string;
  homeLatitude: string;
  homeLongitude: string;
};

function toDraft(p: Preferences): Draft {
  return {
    acceptsFullTime: p.acceptsFullTime,
    acceptsPartTime: p.acceptsPartTime,
    excludeStaffingAgencies: p.excludeStaffingAgencies,
    excludeSeniorRoles: p.excludeSeniorRoles,
    limitYears: p.maxRequiredYears !== null,
    maxRequiredYears: String(p.maxRequiredYears ?? 3),
    limitCommute: p.commute !== null,
    maxDistanceKm: String(p.commute?.maxDistanceKm ?? 30),
    homeLatitude: p.commute ? String(p.commute.homeLatitude) : "",
    homeLongitude: p.commute ? String(p.commute.homeLongitude) : "",
  };
}

function toPreferences(d: Draft): Preferences {
  return {
    acceptsFullTime: d.acceptsFullTime,
    acceptsPartTime: d.acceptsPartTime,
    excludeStaffingAgencies: d.excludeStaffingAgencies,
    excludeSeniorRoles: d.excludeSeniorRoles,
    maxRequiredYears: d.limitYears ? Number(d.maxRequiredYears) : null,
    commute: d.limitCommute
      ? {
          homeLatitude: Number(d.homeLatitude),
          homeLongitude: Number(d.homeLongitude),
          maxDistanceKm: Number(d.maxDistanceKm),
        }
      : null,
  };
}

/** Dealbreakers: ads that break them are never shown. Saved all at once, as Claude does with set_preferences. */
export function PreferencesEditor({
  preferences,
  onChanged,
}: {
  preferences: Preferences;
  onChanged: () => void;
}) {
  const [draft, setDraft] = useState(() => toDraft(preferences));
  const [error, setError] = useState<string>();
  const [locating, setLocating] = useState(false);

  const update = (changes: Partial<Draft>) =>
    setDraft((d) => ({ ...d, ...changes }));
  const changed =
    JSON.stringify(toPreferences(draft)) !==
    JSON.stringify(toPreferences(toDraft(preferences)));
  const noHours = !draft.acceptsFullTime && !draft.acceptsPartTime;

  const useMyPosition = () => {
    setError(undefined);
    setLocating(true);
    navigator.geolocation.getCurrentPosition(
      (position) => {
        setLocating(false);
        update({
          homeLatitude: String(round(position.coords.latitude)),
          homeLongitude: String(round(position.coords.longitude)),
        });
      },
      () => {
        setLocating(false);
        setError(
          "Kunde inte hämta din position. Fyll i latitud och longitud själv.",
        );
      },
    );
  };

  const onSubmit = (e: FormEvent) => {
    e.preventDefault();
    setError(undefined);
    setPreferences(toPreferences(draft)).then(onChanged, (err: unknown) =>
      setError(message(err)),
    );
  };

  return (
    <form
      className="preferences-form"
      onSubmit={onSubmit}
      aria-label="Dina krav"
    >
      <fieldset>
        <legend>Arbetstid</legend>
        <label>
          <input
            type="checkbox"
            checked={draft.acceptsFullTime}
            onChange={(e) => update({ acceptsFullTime: e.target.checked })}
          />
          Heltid
        </label>
        <label>
          <input
            type="checkbox"
            checked={draft.acceptsPartTime}
            onChange={(e) => update({ acceptsPartTime: e.target.checked })}
          />
          Deltid
        </label>
        {noHours && (
          <p className="muted">Välj minst en, annars visas inga annonser.</p>
        )}
      </fieldset>

      <label>
        <input
          type="checkbox"
          checked={draft.excludeStaffingAgencies}
          onChange={(e) =>
            update({ excludeStaffingAgencies: e.target.checked })
          }
        />
        Dölj annonser från bemanningsföretag
      </label>

      <label>
        <input
          type="checkbox"
          checked={draft.excludeSeniorRoles}
          onChange={(e) => update({ excludeSeniorRoles: e.target.checked })}
        />
        Dölj seniora tjänster (titeln säger senior, lead, arkitekt, chef …)
      </label>

      <div>
        <label>
          <input
            type="checkbox"
            checked={draft.limitYears}
            onChange={(e) => update({ limitYears: e.target.checked })}
          />
          Dölj annonser som tydligt kräver fler års erfarenhet än
        </label>
        {draft.limitYears && (
          <label>
            Högst (år)
            <input
              type="number"
              min={0}
              max={15}
              required
              value={draft.maxRequiredYears}
              onChange={(e) => update({ maxRequiredYears: e.target.value })}
            />
          </label>
        )}
      </div>

      <fieldset>
        <legend>Pendling</legend>
        <label>
          <input
            type="checkbox"
            checked={draft.limitCommute}
            onChange={(e) => update({ limitCommute: e.target.checked })}
          />
          Begränsa avståndet från hemmet
        </label>
        {draft.limitCommute && (
          <div className="commute">
            <PlacePicker
              onPick={(place) =>
                update({
                  homeLatitude: String(round(place.latitude)),
                  homeLongitude: String(round(place.longitude)),
                })
              }
            />
            <label>
              Högst (km fågelvägen)
              <input
                type="number"
                min={1}
                max={500}
                required
                value={draft.maxDistanceKm}
                onChange={(e) => update({ maxDistanceKm: e.target.value })}
              />
            </label>
            <label>
              Latitud
              <input
                type="number"
                step="any"
                min={-90}
                max={90}
                required
                value={draft.homeLatitude}
                onChange={(e) => update({ homeLatitude: e.target.value })}
              />
            </label>
            <label>
              Longitud
              <input
                type="number"
                step="any"
                min={-180}
                max={180}
                required
                value={draft.homeLongitude}
                onChange={(e) => update({ homeLongitude: e.target.value })}
              />
            </label>
            {"geolocation" in navigator && (
              <button type="button" onClick={useMyPosition} disabled={locating}>
                {locating ? "Hämtar position…" : "Använd min position"}
              </button>
            )}
          </div>
        )}
      </fieldset>

      <div>
        <button type="submit" disabled={!changed || noHours}>
          Spara krav
        </button>
      </div>
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
    </form>
  );
}
