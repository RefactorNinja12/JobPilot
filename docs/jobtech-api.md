# JobTech API findings

Findings from real data, used to design `JobAd`, the matching funnel and the JobTech clients.
Sample: 200 ads from JobSearch, query `utvecklare`, fetched 2026-11-16. Other occupations may differ.

## APIs (Arbetsförmedlingen / JobTech Dev, open, no API key)

| API | Base URL | Use |
|---|---|---|
| JobSearch | `https://jobsearch.api.jobtechdev.se` | `GET /search`, `GET /ad/{id}`: current Platsbanken ads |
| JobAd Enrichments | `https://jobad-enrichments-api.jobtechdev.se` | `POST /enrichtextdocuments`: extracts skills/occupations/traits from free text. `GET /synonymdictionary`: its vocabulary |
| Taxonomy | `https://taxonomy.api.jobtechdev.se` | Standard concepts and ids, used for occupations (JobSearch `occupation-name`) |

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
2. **Enrichments returns labels only, no concept ids, and its labels are its own vocabulary.**
   They do not map reliably to Taxonomy (see "Skill vocabulary" below), so profile skills use
   Enrichments concepts as their key, not Taxonomy ids.
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

## Skill vocabulary: Enrichments, not Taxonomy

Measured 2026-11-16 using 20 current ads for occupation `fg7B_yov_smw` (Systemutvecklare). Enrichments
returned 186 competencies with prediction ≥ 0.8, which is 117 unique labels. Each label was matched,
ignoring case, against the 6,752 Taxonomy concepts of type `skill`
(`GET /v1/taxonomy/main/concepts?type=skill`, 1.5 MB), including alternative and hidden labels:

| Matched on | Labels |
|---|---|
| Preferred label | 13 |
| Alternative label | 17 |
| Hidden label | 0 |
| **No match** | **87 (74%)** |

The two vocabularies name the same thing differently. Taxonomy uses `"C#, programmeringsspråk"`
(with alternative label `"C#"`), `"Apache Kafka, streaming-plattform"` and `"Windows Azure, molnplattform"`.
Some labels exist only under other concept types, such as `Ansible` (`esco-skill`) and `Engelska` (`language`).
Others, such as `.net`, have no reasonable match at all. Fuzzy matching would bring wrong matches into a
deterministic funnel.

Enrichments publishes its own vocabulary at `GET /synonymdictionary?type=COMPETENCE&spelling=CORRECTLY_SPELLED`
(7.3 MB, about 6 s; build date 2025-06-17). The response is `{ total, build_datetime, items[] }`, where each item
is `{ concept, term, term_misspelled, type: "KOMPETENS" }`, with 65,512 terms for 58,432 concepts. Every label
from `/enrichtextdocuments` is a `concept` there, and the terms are its synonyms, e.g. `C#` ← `c#`, `c #`, `c-sharp`,
and `Kafka` ← `kafka`, `apache kafka`.

**Decision:** a profile skill is keyed by its Enrichments concept label. The user picks skills from
this dictionary (searching the terms), so ads and profile share one vocabulary and match exactly.
Taxonomy stays in use for occupation ids.
