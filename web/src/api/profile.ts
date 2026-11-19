import { getJson, sendJson } from "./http";

// Mirrors the JSON from GET /api/profile (Jobpilot.Api).

export type SkillLevel = "Beginner" | "Intermediate" | "Advanced" | "Expert";

export type ProfileSkill = {
  name: string;
  level: SkillLevel;
  yearsOfExperience: number;
};

export type Occupation = { taxonomyId: string; label: string };

export type Preferences = {
  acceptsFullTime: boolean;
  acceptsPartTime: boolean;
  excludeStaffingAgencies: boolean;
  excludeSeniorRoles: boolean;
  commute: {
    homeLatitude: number;
    homeLongitude: number;
    maxDistanceKm: number;
  } | null;
};

export type Profile = {
  skills: ProfileSkill[];
  targetOccupations: Occupation[];
  preferences: Preferences;
};

export function fetchProfile(signal?: AbortSignal): Promise<Profile> {
  return getJson<Profile>("/api/profile", signal);
}

/** What adding a skill stored: the vocabulary name, and whether it can ever match an ad. */
export type AddedSkill = { name: string; inVocabulary: boolean };

export type SkillSuggestion = { name: string; terms: string[] };

export function addSkill(skill: ProfileSkill): Promise<AddedSkill> {
  return sendJson<AddedSkill>("POST", "/api/profile/skills", skill);
}

export function updateSkill(skill: ProfileSkill): Promise<ProfileSkill> {
  return sendJson<ProfileSkill>("PUT", "/api/profile/skills", skill);
}

export function removeSkill(name: string): Promise<string> {
  return sendJson<string>(
    "DELETE",
    `/api/profile/skills?name=${encodeURIComponent(name)}`,
  );
}

export function searchSkills(
  text: string,
  signal?: AbortSignal,
): Promise<SkillSuggestion[]> {
  return getJson<SkillSuggestion[]>(
    `/api/skills/search?q=${encodeURIComponent(text)}`,
    signal,
  );
}

/** Replaces the occupations job ads are searched for (at most 10). Answers with the stored list. */
export function setOccupations(
  occupations: Occupation[],
): Promise<Occupation[]> {
  return sendJson<Occupation[]>("PUT", "/api/profile/occupations", occupations);
}

export function searchOccupations(
  text: string,
  signal?: AbortSignal,
): Promise<Occupation[]> {
  return getJson<Occupation[]>(
    `/api/occupations/search?q=${encodeURIComponent(text)}`,
    signal,
  );
}

export type Place = {
  name: string;
  municipality: string;
  latitude: number;
  longitude: number;
};

export function searchPlaces(
  text: string,
  signal?: AbortSignal,
): Promise<Place[]> {
  return getJson<Place[]>(
    `/api/places/search?q=${encodeURIComponent(text)}`,
    signal,
  );
}

/** Replaces all dealbreakers; commute null removes the distance limit. Answers with what was stored. */
export function setPreferences(preferences: Preferences): Promise<Preferences> {
  return sendJson<Preferences>("PUT", "/api/profile/preferences", preferences);
}
