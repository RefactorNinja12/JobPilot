import { getJson, sendJson } from './http'

// Mirrors the JSON from GET /api/profile (Jobpilot.Api).

export type SkillLevel = 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert'

export type ProfileSkill = { name: string; level: SkillLevel; yearsOfExperience: number }

export type Occupation = { taxonomyId: string; label: string }

export type Preferences = {
  acceptsFullTime: boolean
  acceptsPartTime: boolean
  excludeStaffingAgencies: boolean
  commute: { homeLatitude: number; homeLongitude: number; maxDistanceKm: number } | null
}

export type Profile = { skills: ProfileSkill[]; targetOccupations: Occupation[]; preferences: Preferences }

export function fetchProfile(signal?: AbortSignal): Promise<Profile> {
  return getJson<Profile>('/api/profile', signal)
}

/** What adding a skill stored: the vocabulary name, and whether it can ever match an ad. */
export type AddedSkill = { name: string; inVocabulary: boolean }

export type SkillSuggestion = { name: string; terms: string[] }

export function addSkill(skill: ProfileSkill): Promise<AddedSkill> {
  return sendJson<AddedSkill>('POST', '/api/profile/skills', skill)
}

export function updateSkill(skill: ProfileSkill): Promise<ProfileSkill> {
  return sendJson<ProfileSkill>('PUT', '/api/profile/skills', skill)
}

export function removeSkill(name: string): Promise<string> {
  return sendJson<string>('DELETE', `/api/profile/skills?name=${encodeURIComponent(name)}`)
}

export function searchSkills(text: string, signal?: AbortSignal): Promise<SkillSuggestion[]> {
  return getJson<SkillSuggestion[]>(`/api/skills/search?q=${encodeURIComponent(text)}`, signal)
}
