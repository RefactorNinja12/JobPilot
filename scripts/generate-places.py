#!/usr/bin/env python3
"""Regenerates src/Jobpilot.Infrastructure/Places/places.json, the embedded list of Swedish places.

Places: GeoNames SE dump (CC BY 4.0, https://www.geonames.org), populated places with a known
population, which roughly matches Sweden's ~2,000 localities (tatorter).
Municipality names: JobTech Taxonomy, joined on the municipality code (GeoNames admin2 = LAU 2 code).

Usage: python3 -I scripts/generate-places.py
"""
import csv
import io
import json
import pathlib
import urllib.parse
import urllib.request
import zipfile

GEONAMES_URL = "https://download.geonames.org/export/dump/SE.zip"
TAXONOMY_URL = "https://taxonomy.api.jobtechdev.se/v1/taxonomy/graphql?query=" + urllib.parse.quote(
    '{concepts(type:"municipality"){preferred_label lau_2_code_2015}}')
# Towns and cities. PPLX (city districts) and the like are left out.
FEATURE_CODES = {"PPL", "PPLA", "PPLA2", "PPLC"}
OUTPUT = pathlib.Path(__file__).resolve().parent.parent / "src/Jobpilot.Infrastructure/Places/places.json"


def fetch(url: str) -> bytes:
    request = urllib.request.Request(url, headers={"User-Agent": "jobpilot-place-import/1.0"})
    with urllib.request.urlopen(request, timeout=120) as response:
        return response.read()


def municipalities() -> dict[str, str]:
    concepts = json.loads(fetch(TAXONOMY_URL))["data"]["concepts"]
    return {c["lau_2_code_2015"]: c["preferred_label"] for c in concepts if c["lau_2_code_2015"]}


def places(names: dict[str, str]) -> list[dict]:
    archive = zipfile.ZipFile(io.BytesIO(fetch(GEONAMES_URL)))
    rows = csv.reader(io.TextIOWrapper(archive.open("SE.txt"), encoding="utf-8"), delimiter="\t")
    best: dict[tuple[str, str], dict] = {}
    for row in rows:
        name, lat, lon, feature, municipality_code, population = row[1], row[4], row[5], row[7], row[11], int(row[14] or 0)
        if feature not in FEATURE_CODES or population == 0 or municipality_code not in names:
            continue
        place = {"name": name, "municipality": names[municipality_code],
                 "latitude": float(lat), "longitude": float(lon), "population": population}
        key = (name, place["municipality"])
        if key not in best or best[key]["population"] < population:
            best[key] = place
    return sorted(best.values(), key=lambda p: (p["name"], p["municipality"]))


def main() -> None:
    result = places(municipalities())
    lines = ",\n".join(json.dumps(p, ensure_ascii=False) for p in result)
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(f"[\n{lines}\n]\n", encoding="utf-8")
    print(f"Wrote {len(result)} places to {OUTPUT}")


if __name__ == "__main__":
    main()
