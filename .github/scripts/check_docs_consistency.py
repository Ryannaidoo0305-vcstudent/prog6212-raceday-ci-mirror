#!/usr/bin/env python3
"""
check_docs_consistency.py - fail the CI build if the Part 1 documents disagree.

docs/RaceDay_Database.sql is the single source of truth. This script checks:
  1. ERD (docs/RaceDay_ERD.mmd): same tables, columns and PK/FK/UK keys, a
     relationship line for every foreign key, and the right cardinality on each
     line (UNIQUE foreign key = zero-or-one, otherwise zero-or-many; NOT NULL
     foreign key = exactly one at the parent end)
  2. Data dictionary (docs/Data_Dictionary.md): same tables and columns, same data
     types and nullability, the same descriptions that the script stores in SQL
     Server, and every named constraint and index documented
  3. Every table and column in the script has a stored description
  4. README links point at files that exist (warnings only - e.g. the CI
     screenshot is added after the first green build)
In GitHub Actions it also writes a results table to the job summary page.

Usage:  python3 .github/scripts/check_docs_consistency.py   (works from any folder)
"""
import os
import re
import sys

SQL_PATH, ERD_PATH, DICT_PATH, PLAN_PATH, README_PATH = (
    "docs/RaceDay_Database.sql", "docs/RaceDay_ERD.mmd", "docs/Data_Dictionary.md",
    "docs/API_Endpoint_Plan.md", "README.md")

SQL_TYPES = (r"(?:BIGINT|INT|SMALLINT|TINYINT|BIT|DECIMAL|NUMERIC|FLOAT|REAL|MONEY|"
             r"NVARCHAR|VARCHAR|NCHAR|CHAR|NTEXT|TEXT|DATETIME2|DATETIMEOFFSET|DATETIME|"
             r"DATE|TIME|UNIQUEIDENTIFIER|VARBINARY)")

# Crow's-foot symbols as written on the RIGHT-hand side of a Mermaid relationship line
MANY, ZERO_OR_ONE, EXACTLY_ONE = {"o{", "|{"}, {"o|"}, {"||"}


def read_text(path):
    """Read a file whatever encoding the editor saved it in (UTF-8, UTF-8 BOM or UTF-16)."""
    with open(path, "rb") as f:
        raw = f.read()
    if raw.startswith((b"\xff\xfe", b"\xfe\xff")):
        return raw.decode("utf-16")
    return raw.decode("utf-8-sig", errors="replace")


def split_columns(text):
    return {c.strip().strip("[]") for c in text.split(",") if c.strip()}


def norm_type(t):
    return re.sub(r"\s+", "", t).upper()


# --------------------------------------------------------------------------- SQL
def parse_sql(path):
    text = read_text(path)
    code = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    code = re.sub(r"--[^\n]*", "", code)
    tables = {}
    table_re = r"CREATE\s+TABLE\s+(?:\[?dbo\]?\.)?\[?(\w+)\]?\s*\((.*?)\n\s*\)\s*;"
    for name, body in re.findall(table_re, code, flags=re.S | re.I):
        t = {"cols": {}, "order": [], "pk": set(), "fk": {}, "uk": set(), "uq_groups": [],
             "defaults": {}, "checks": {}, "identity": set()}
        last_col = None
        for line in body.splitlines():
            m = re.match(r"\s*\[?(\w+)\]?\s+(" + SQL_TYPES + r"(?:\s*\(\s*\d+\s*(?:,\s*\d+\s*)?\))?)",
                         line, flags=re.I)
            if m and m.group(1).upper() != "CONSTRAINT":
                col = m.group(1)
                nullable = not re.search(r"\bNOT\s+NULL\b", line, flags=re.I)
                t["cols"][col] = {"type": norm_type(m.group(2)), "null": nullable}
                t["order"].append(col)
                if re.search(r"\bIDENTITY\b", line, flags=re.I):
                    t["identity"].add(col)
                last_col = col
            d = re.search(r"CONSTRAINT\s+(DF_\w+)\s+DEFAULT\s*\((.*)\)\s*,?\s*$", line, flags=re.I)
            if d and last_col:
                t["defaults"][last_col] = (d.group(1), d.group(2).strip())
            c = re.search(r"CONSTRAINT\s+(CK_\w+)\s+CHECK\s*\((.*)\)\s*,?\s*$", line, flags=re.I)
            if c:
                t["checks"][c.group(1)] = c.group(2).strip()
        for cols in re.findall(r"PRIMARY\s+KEY\s*(?:CLUSTERED|NONCLUSTERED)?\s*\(([^)]*)\)", body, flags=re.I):
            t["pk"] |= split_columns(cols)
        fk_re = (r"CONSTRAINT\s+(FK_\w+)\s+FOREIGN\s+KEY\s*\(\s*\[?(\w+)\]?\s*\)\s*REFERENCES\s+"
                 r"(?:\[?dbo\]?\.)?\[?(\w+)\]?\s*\(\s*\[?(\w+)\]?\s*\)\s*(?:ON\s+DELETE\s+(CASCADE|NO\s+ACTION|SET\s+NULL))?")
        for fk_name, col, parent, parent_col, on_delete in re.findall(fk_re, body, flags=re.I):
            t["fk"][col] = {"name": fk_name, "parent": parent, "parent_col": parent_col,
                            "on_delete": re.sub(r"\s+", " ", (on_delete or "NO ACTION").upper())}
        for uq_name, cols in re.findall(r"CONSTRAINT\s+(UQ_\w+)\s+UNIQUE\s*(?:CLUSTERED|NONCLUSTERED)?\s*\(([^)]*)\)",
                                        body, flags=re.I):
            group = [c.strip().strip("[]") for c in cols.split(",") if c.strip()]
            t["uq_groups"].append((uq_name, group))
            if len(group) == 1:
                t["uk"].add(group[0])
        tables[name] = t

    constraints = set(re.findall(r"CONSTRAINT\s+([A-Z]{2}_\w+)", code, flags=re.I))
    indexes = {name: (tbl, cols) for name, tbl, cols in
               re.findall(r"CREATE\s+(?:NONCLUSTERED\s+)?INDEX\s+(\w+)\s+ON\s+(?:dbo\.)?(\w+)\s*\(([^)]*)\)", code, flags=re.I)}

    descriptions = {}
    block = code.split("INSERT INTO @Descriptions", 1)
    if len(block) == 2:
        block = block[1].split("DECLARE @i", 1)[0]
        row_re = r"\(\s*N'(\w+)'\s*,\s*(?:N'(\w+)'|NULL)\s*,\s*N'((?:[^']|'')*)'\s*\)"
        for tbl, col, desc in re.findall(row_re, block):
            descriptions[(tbl, col or None)] = desc.replace("''", "'")
    return tables, constraints, indexes, descriptions


# --------------------------------------------------------------------------- ERD
def normalise_left(symbol):
    """Turn a left-hand symbol ('}o', '|o', '||') into its right-hand form ('o{', 'o|', '||')."""
    return symbol[::-1].replace("}", "{")


def parse_erd(path):
    text = read_text(path)
    text = re.sub(r"\A\s*---.*?\n---[^\n]*", "", text, flags=re.S)   # front matter
    text = re.sub(r"%%[^\n]*", "", text)                             # comments
    text = re.sub(r'"[^"]*"', "", text)                              # quoted notes and labels
    entities = {}
    for name, body in re.findall(r"^\s*(\w+)\s*\{(.*?)^\s*\}", text, flags=re.S | re.M):
        e = {"cols": set(), "pk": set(), "fk": set(), "uk": set()}
        for line in body.splitlines():
            parts = line.split(None, 2)
            if len(parts) < 2:
                continue
            col, keys = parts[1], (parts[2] if len(parts) > 2 else "")
            e["cols"].add(col)
            for key in re.findall(r"\b(PK|FK|UK)\b", keys):
                e[key.lower()].add(col)
        entities[name] = e
    rel_re = r"^\s*(\w+)\s+([|}o][|o])(?:--|\.\.)([|o][|{o])\s+(\w+)\s*:"
    return entities, re.findall(rel_re, text, flags=re.M)


def check_erd(sql, erd, rels, errors):
    for t in sorted(set(sql) - set(erd)):
        errors.append(f"ERD: table {t} is in the SQL script but missing from the ERD")
    for t in sorted(set(erd) - set(sql)):
        errors.append(f"ERD: entity {t} is in the ERD but missing from the SQL script")
    for t in sorted(set(sql) & set(erd)):
        s, e = sql[t], erd[t]
        for label, in_sql, in_erd in (("columns", set(s["cols"]), e["cols"]), ("primary key", s["pk"], e["pk"]),
                                      ("foreign keys", set(s["fk"]), e["fk"]), ("unique keys", s["uk"], e["uk"])):
            if in_sql != in_erd:
                errors.append(f"ERD: {t} {label} differ - only in SQL: {sorted(in_sql - in_erd) or 'none'}; "
                              f"only in ERD: {sorted(in_erd - in_sql) or 'none'}")
        for col, fk in s["fk"].items():
            parent = fk["parent"]
            rel = next((r for r in rels if {r[0], r[3]} == {t, parent}), None)
            if rel is None:
                errors.append(f"ERD: {t}.{col} references {parent}, but there is no line between {parent} and {t}")
                continue
            left, left_sym, right_sym, _ = rel
            child_sym, parent_sym = ((normalise_left(left_sym), right_sym) if left == t
                                     else (right_sym, normalise_left(left_sym)))
            unique = col in s["uk"]
            if child_sym not in (ZERO_OR_ONE | EXACTLY_ONE if unique else MANY):
                wanted = "zero-or-one (o|)" if unique else "zero-or-many (o{)"
                errors.append(f"ERD: {parent} -> {t}: {t}.{col} should be drawn as {wanted}, ERD shows '{child_sym}'")
            if not s["cols"][col]["null"] and parent_sym not in EXACTLY_ONE:
                errors.append(f"ERD: {parent} -> {t}: {t}.{col} is NOT NULL, so the {parent} end must be exactly one (||)")
    fk_pairs = {frozenset((t, fk["parent"])) for t, d in sql.items() for fk in d["fk"].values()}
    for left, _, _, right in rels:
        if frozenset((left, right)) not in fk_pairs:
            errors.append(f"ERD: line {left} - {right} has no matching FOREIGN KEY in the SQL script")


# --------------------------------------------------------------------------- data dictionary
def parse_dictionary(path):
    text = read_text(path)
    tables = {}
    for m in re.finditer(r"^###\s+(\w+)\s*$(.*?)(?=^#{2,3}\s|\Z)", text, flags=re.S | re.M):
        name, section = m.group(1), m.group(2)
        lines = [l for l in section.splitlines() if l.strip().startswith("|")]
        if len(lines) < 3:
            continue
        header = [h.strip().lower() for h in lines[0].strip().strip("|").split("|")]
        if "column" not in header or "type" not in header:
            continue
        idx = {h: i for i, h in enumerate(header)}
        rows = {}
        for line in lines[2:]:
            cells = [c.strip().replace("`", "").replace("**", "") for c in line.strip().strip("|").split("|")]
            if len(cells) != len(header):
                continue
            rows[cells[idx["column"]]] = {"type": norm_type(cells[idx["type"]]),
                                          "null": cells[idx["null"]].lower().startswith("y"),
                                          "desc": cells[idx["description"]]}
        tables[name] = rows
    mentioned = set(re.findall(r"\b((?:PK|FK|UQ|CK|DF|IX)_\w+)", text))
    return tables, mentioned


def check_dictionary(sql, dictionary, mentioned, constraints, indexes, descriptions, errors):
    for t in sorted(set(sql) - set(dictionary)):
        errors.append(f"Data dictionary: table {t} is missing")
    for t in sorted(set(dictionary) - set(sql)):
        errors.append(f"Data dictionary: table {t} is not in the SQL script")
    for t in sorted(set(sql) & set(dictionary)):
        s_cols, d_cols = sql[t]["cols"], dictionary[t]
        for c in sorted(set(s_cols) - set(d_cols)):
            errors.append(f"Data dictionary: {t}.{c} is missing")
        for c in sorted(set(d_cols) - set(s_cols)):
            errors.append(f"Data dictionary: {t}.{c} is not in the SQL script")
        for c in sorted(set(s_cols) & set(d_cols)):
            if s_cols[c]["type"] != d_cols[c]["type"]:
                errors.append(f"Data dictionary: {t}.{c} type is {d_cols[c]['type']}, SQL says {s_cols[c]['type']}")
            if s_cols[c]["null"] != d_cols[c]["null"]:
                errors.append(f"Data dictionary: {t}.{c} nullability differs from the SQL script")
            stored = descriptions.get((t, c))
            if stored is not None and stored != d_cols[c]["desc"]:
                errors.append(f"Data dictionary: {t}.{c} description differs from the one stored in SQL Server")
    named = constraints | set(indexes)
    for n in sorted(named - mentioned):
        errors.append(f"Data dictionary: {n} is not documented")
    for n in sorted(mentioned - named):
        errors.append(f"Data dictionary: {n} is documented but does not exist in the SQL script")


def check_descriptions(sql, descriptions, errors):
    for t, d in sql.items():
        if (t, None) not in descriptions:
            errors.append(f"SQL: table {t} has no stored description (STEP 9)")
        for c in d["cols"]:
            if (t, c) not in descriptions:
                errors.append(f"SQL: column {t}.{c} has no stored description (STEP 9)")
    for (t, c) in descriptions:
        if t not in sql or (c is not None and c not in sql[t]["cols"]):
            errors.append(f"SQL: description for {t}.{c or '(table)'} does not match a real table/column")


def check_readme_links(path, warnings):
    if not os.path.exists(path):
        return 0
    links = re.findall(r"\]\((?!https?://|#|mailto:)([^)\s#]+)", read_text(path))
    for link in sorted(set(links)):
        if not os.path.exists(link):
            warnings.append(f"README links to {link}, which does not exist yet")
    return len(set(links))


def count_endpoints(path):
    if not os.path.exists(path):
        return 0
    return len(re.findall(r"^\|\s*\d+\s*\|\s*(?:GET|POST|PUT|PATCH|DELETE)\s", read_text(path), flags=re.M))


# --------------------------------------------------------------------------- main
def main():
    # Work from the repository root, wherever the script is started from
    os.chdir(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))
    errors, warnings = [], []
    sql, constraints, indexes, descriptions = parse_sql(SQL_PATH)
    erd, rels = parse_erd(ERD_PATH)
    dictionary, mentioned = parse_dictionary(DICT_PATH)
    if not sql:
        errors.append(f"No CREATE TABLE statements found in {SQL_PATH}")

    results = []
    before = len(errors)
    check_erd(sql, erd, rels, errors)
    n_cols = sum(len(t["cols"]) for t in sql.values())
    results.append(("ERD matches the SQL script (tables, columns, keys, relationships, cardinality)",
                    len(errors) == before, f"{len(sql)} tables, {n_cols} columns, {len(rels)} relationships"))
    before = len(errors)
    check_dictionary(sql, dictionary, mentioned, constraints, indexes, descriptions, errors)
    results.append(("Data dictionary matches the SQL script (types, nullability, descriptions)",
                    len(errors) == before, f"{sum(len(r) for r in dictionary.values())} columns, "
                    f"{len(constraints)} constraints and {len(indexes)} indexes documented"))
    before = len(errors)
    check_descriptions(sql, descriptions, errors)
    results.append(("Every table and column is described inside SQL Server",
                    len(errors) == before, f"{len(descriptions)} descriptions"))
    n_links = check_readme_links(README_PATH, warnings)
    results.append(("README links point at real files", True,
                    f"{n_links} links, {len(warnings)} not added yet" if warnings else f"{n_links} links"))
    results.append(("Endpoints planned in API_Endpoint_Plan.md", True, str(count_endpoints(PLAN_PATH))))

    for label, ok, detail in results:
        print(f"{'OK  ' if ok else 'FAIL'}  {label}: {detail}")
    for w in warnings:
        print(f"::warning::{w}")
    for e in errors:
        print(f"::error::{e}")

    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as f:
            f.write("## Documentation checks\n\n| Check | Result | Detail |\n|---|---|---|\n")
            for label, ok, detail in results:
                f.write(f"| {label} | {'✅ Pass' if ok else '❌ Fail'} | {detail} |\n")
            for w in warnings:
                f.write(f"\n> ⚠️ {w}\n")
            f.write("\n")

    if errors:
        print(f"\n{len(errors)} problem(s) found - the documents do not agree.")
        return 1
    print("\nAll Part 1 documents agree with the SQL script.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
