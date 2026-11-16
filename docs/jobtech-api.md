# JobTech API findings

Findings from real data, used to design `JobAd`, the matching funnel and the JobTech clients.
Sample: 200 ads from JobSearch, query `utvecklare`, fetched 2026-11-16. Other occupations may differ.

## APIs (Arbetsförmedlingen / JobTech Dev, open, no API key)

| API | Base URL | Use |
|---|---|---|
| JobSearch | `https://jobsearch.api.jobtechdev.se` | `GET /search`, `GET /ad/{id}`: current Platsbanken ads |
| JobAd Enrichments | `https://jobad-enrichments-api.jobtechdev.se` | `POST /enrichtextdocuments`: extracts skills/occupations/traits from free text |
| Taxonomy | `https://taxonomy.api.jobtechdev.se` | Standard concepts and ids for occupations and skills |

## Field coverage (JobSearch)

| Field | Filled | Notes |
|---|---|---|
| `workplace_address.municipality` | 99.5% | |
| `workplace_address.coordinates` | 90.5% | **Order is `[longitude, latitude]`**, the reverse of `Coordinates.Create(lat, lon)` |
| `working_hours_type` | 99% | `Heltid` / `Deltid` concept; `scope_of_work.min/max` in percent |
| `employer.organization_number` | 100% | Usable for staffing-agency detection (industry code / known list) |
| `application_deadline` | 100% | |
| `occupation` / `occupation_group` / `occupation_field` | 100% | Taxonomy concept ids included |
| `workplace_addresses` with more than one entry | 1% | |
| `workplace_model` (on site / hybrid / remote) | 22% | 69/200 mention distans/remote/hybrid in the text instead |
| `must_have.skills` or `nice_to_have.skills` | **4.5%** | Structured skills are almost never present |
| `salary_description` | **0%** | Amounts in text were benefits, not salary. The salary dealbreaker was dropped |

Other: `experience_required` true in 99%, `driving_license_required` 3.5%, `number_of_vacancies` 1 in 95.5%.

## Consequences for the design

1. **Enrichments is required, not optional.** Skills live in the description text. For one test ad,
   Enrichments returned 46 competencies, 18 with prediction > 0.8. Use a prediction threshold.
2. **Enrichments returns labels only, no concept ids.** Labels must be resolved through Taxonomy
   to get ids, or `Skill` equality (by taxonomy id) will not match.
3. **Remote/hybrid cannot be filtered reliably in code.** It is mostly in the text, so leave it to the AI step.
4. **Unknown is not a failure.** Ads without coordinates must pass the commute filter and be flagged.
5. **Descriptions average ~3,700 characters (~930 tokens).** Never send full texts to Claude in bulk;
   send compact summaries and fetch details on demand (see the 25–50 ad budget).

## Enrichments request example

```json
POST /enrichtextdocuments
{
  "documents_input": [{ "doc_id": "31727571", "doc_headline": "...", "doc_text": "..." }],
  "include_terms_info": true,
  "include_sentences": false,
  "include_synonyms": false,
  "include_misspelled_synonyms": false,
  "sort_by_prediction_score": "DESC"
}
```

Response: a JSON array with one object per input document, matched by `doc_id`. Each has
`enriched_candidates.{occupations, competencies, traits, geos}[]`, and each candidate has
`concept_label`, `term`, `term_misspelled` and `prediction` (0–1).
An empty `doc_text` still returns its document, often with no competencies.
An empty `documents_input` returns `[]` with HTTP 200.
