import { getJson } from './http'

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
