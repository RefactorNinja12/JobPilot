import { http, HttpResponse, type JsonBodyType } from "msw";
import { setupServer } from "msw/node";
import type { BaseDocument } from "../api/documents";
import type { AssessedJob } from "../api/jobs";
import type { Occupation, Place, Profile } from "../api/profile";

/** An assessed ad as GET /api/jobs/assessed returns it; override what a test cares about. */
export function anAssessedJob(overrides: Partial<AssessedJob> = {}): AssessedJob {
  return {
    adId: "31744367",
    headline: "Backendutvecklare C#",
    employer: "Exempel AB",
    municipality: "Göteborg",
    url: "https://arbetsformedlingen.se/platsbanken/annonser/31744367",
    publishedAt: "2026-11-16T09:00:00+01:00",
    applicationDeadline: "2026-12-01",
    chance: 70,
    summary: "Stark C#-match, Kafka saknas.",
    assessedAt: "2026-11-17T18:00:00Z",
    matchedSkills: ["C#"],
    missingSkills: ["Kafka"],
    ...overrides,
  };
}

/** Answers GET /api/jobs/assessed with the given body and status, like Jobpilot.Api. */
export function respondWithAssessedJobs(body: JsonBodyType, status = 200) {
  server.use(http.get("/api/jobs/assessed", () => HttpResponse.json(body, { status })));
}

/** Answers GET /api/jobs/{adId} with the given body and status, like Jobpilot.Api. Returns the requests seen. */
export function respondWithJob(body: JsonBodyType, status = 200) {
  const requests: string[] = [];
  server.use(
    http.get("/api/jobs/:adId", ({ params }) => {
      requests.push(String(params.adId));
      return HttpResponse.json(body, { status });
    }),
  );
  return requests;
}

export function aProfile(overrides: Partial<Profile> = {}): Profile {
  return {
    skills: [
      { name: "C#", level: "Expert" },
      { name: "Kafka", level: "Beginner" },
    ],
    targetOccupations: [
      { taxonomyId: "7wdX_4rv_33z", label: "Backend-utvecklare" },
    ],
    preferences: {
      acceptsFullTime: true,
      acceptsPartTime: false,
      excludeStaffingAgencies: true,
      excludeSeniorRoles: false,
      maxRequiredYears: null,
      commute: {
        homeLatitude: 57.7089,
        homeLongitude: 11.9746,
        maxDistanceKm: 40,
      },
    },
    ...overrides,
  };
}

/** Answers GET /api/profile, like Jobpilot.Api. */
export function respondWithProfile(body: JsonBodyType, status = 200) {
  server.use(
    http.get("/api/profile", () => HttpResponse.json(body, { status })),
  );
}

type SkillApiCall = { method: string; url: string; body: unknown };

/**
 * Fakes the skill endpoints of Jobpilot.Api. Each mutation answers with the given status and body; search
 * answers with the given suggestions. Returns the calls made, so tests can check what was sent.
 */
export function respondToSkillCalls(options: {
  suggestions?: { name: string; terms: string[] }[];
  answer?: { status: number; body: JsonBodyType };
}) {
  const calls: SkillApiCall[] = [];
  const answer = options.answer ?? {
    status: 200,
    body: { name: "Kafka", inVocabulary: true },
  };
  const record = async (request: Request) => {
    calls.push({
      method: request.method,
      url: request.url,
      body: request.method === "DELETE" ? null : await request.json(),
    });
    return HttpResponse.json(answer.body, { status: answer.status });
  };
  server.use(
    http.get("/api/skills/search", () =>
      HttpResponse.json(options.suggestions ?? []),
    ),
    http.post("/api/profile/skills", ({ request }) => record(request)),
    http.put("/api/profile/skills", ({ request }) => record(request)),
    http.delete("/api/profile/skills", ({ request }) => record(request)),
  );
  return calls;
}

type OccupationApiCall = { method: string; body: unknown };

/**
 * Fakes the occupation endpoints of Jobpilot.Api: search answers with the given suggestions, PUT with the given
 * status and body (by default the list it was sent). Returns the PUTs made, so tests can check what was sent.
 */
export function respondToOccupationCalls(options: {
  suggestions?: Occupation[];
  answer?: { status: number; body: JsonBodyType };
}) {
  const calls: OccupationApiCall[] = [];
  server.use(
    http.get("/api/occupations/search", () =>
      HttpResponse.json(options.suggestions ?? []),
    ),
    http.put("/api/profile/occupations", async ({ request }) => {
      const body = (await request.json()) as JsonBodyType;
      calls.push({ method: request.method, body });
      return HttpResponse.json(options.answer?.body ?? body, {
        status: options.answer?.status ?? 200,
      });
    }),
  );
  return calls;
}

/** Fakes PUT /api/profile/preferences: answers with the given status and body, by default what it was sent. */
export function respondToPreferenceCalls(answer?: {
  status: number;
  body: JsonBodyType;
}) {
  const sent: unknown[] = [];
  server.use(
    http.put("/api/profile/preferences", async ({ request }) => {
      const body = (await request.json()) as JsonBodyType;
      sent.push(body);
      return HttpResponse.json(answer?.body ?? body, {
        status: answer?.status ?? 200,
      });
    }),
  );
  return sent;
}

export const server = setupServer();

/** Fakes GET /api/places/search: answers with the given places. */
export function respondToPlaceSearch(places: Place[]) {
  server.use(http.get("/api/places/search", () => HttpResponse.json(places)));
}

/**
 * Fakes the document endpoints of Jobpilot.Api: GET answers with the given documents, PUT stores what it is sent
 * (or answers with the given status and body), and extract answers with the given text (or status and body).
 * Returns what was saved and the contents of the files uploaded (jsdom loses the file name), so tests can check what
 * was sent.
 */
export function respondToDocumentCalls(
  options: {
    documents?: BaseDocument[];
    saveAnswer?: { status: number; body: JsonBodyType };
    extractAnswer?: { status: number; body: JsonBodyType };
  } = {},
) {
  const saved: { kind: string; text: string }[] = [];
  const uploads: string[] = [];
  server.use(
    http.get("/api/documents", () => HttpResponse.json(options.documents ?? [])),
    http.put("/api/documents", async ({ request }) => {
      const body = (await request.json()) as { kind: string; text: string };
      saved.push(body);
      if (options.saveAnswer) {
        return HttpResponse.json(options.saveAnswer.body, { status: options.saveAnswer.status });
      }
      return HttpResponse.json({ kind: body.kind, text: body.text.trim(), updatedAt: "2026-11-20T10:00:00Z" });
    }),
    http.post("/api/documents/extract", async ({ request }) => {
      const file = (await request.formData()).get("file") as File;
      uploads.push(await file.text());
      const answer = options.extractAnswer ?? { status: 200, body: { text: "Text ur Word-filen" } };
      return HttpResponse.json(answer.body, { status: answer.status });
    }),
  );
  return { saved, uploads };
}
