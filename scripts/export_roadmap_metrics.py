#!/usr/bin/env python3
from __future__ import annotations

import csv
import re
from collections import defaultdict
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
INPUT = ROOT / "docs" / "metrics" / "git-commit-metrics.csv"
ROADMAP_INPUT = ROOT / "docs" / "project-roadmap.md"
OUTPUT_DIR = ROOT / "docs" / "metrics"
MARKDOWN_OUTPUT = OUTPUT_DIR / "roadmap-milestone-phase-metrics.md"
REPORT_CUTOFF_DATE = "2026-03-01"

CHANGE_PREFIXES = ("", "docs_", "code_", "src_", "test_", "other_", "decision_", "major_decision_")
CHANGE_SUFFIXES = (
    "changed_files",
    "added_files",
    "deleted_files",
    "modified_files",
    "renamed_files",
    "copied_files",
    "type_changed_files",
    "other_files",
    "lines_added",
    "lines_deleted",
    "lines_changed",
)
CHANGE_COLUMNS = [f"{prefix}{suffix}" for prefix in CHANGE_PREFIXES for suffix in CHANGE_SUFFIXES]
SNAPSHOT_COLUMNS = [
    "snapshot_tracked_files",
    "snapshot_text_files",
    "snapshot_total_text_lines",
    "snapshot_doc_files",
    "snapshot_doc_lines",
    "snapshot_code_files",
    "snapshot_code_lines",
    "snapshot_other_files",
    "snapshot_other_lines",
    "snapshot_decision_files",
    "snapshot_decision_lines",
    "snapshot_major_decision_files",
    "snapshot_major_decision_lines",
    "snapshot_src_files",
    "snapshot_src_lines",
    "snapshot_test_files",
    "snapshot_test_lines",
    "snapshot_csharp_files",
    "snapshot_csharp_lines",
]


def read_rows() -> list[dict[str, str]]:
    with INPUT.open(encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def filter_report_rows(rows: list[dict[str, str]]) -> list[dict[str, str]]:
    return [row for row in rows if row.get("committed_at", "")[:10] >= REPORT_CUTOFF_DATE]


def load_roadmap_titles() -> tuple[dict[str, str], dict[str, str]]:
    milestone_titles: dict[str, str] = {}
    phase_titles: dict[str, str] = {}

    for raw_line in ROADMAP_INPUT.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        milestone_match = re.match(r"^##\s+Milestone\s+(\d+)\s*-\s*(.+)$", line)
        if milestone_match:
            milestone_titles[f"M{milestone_match.group(1)}"] = milestone_match.group(2).strip()
            continue

        phase_match = re.match(r"^#{3,4}\s+(M\d+-P\d+[A-Z]?)\s+(.+)$", line)
        if phase_match:
            phase_titles[phase_match.group(1).upper()] = phase_match.group(2).strip()

    return milestone_titles, phase_titles


def int_value(row: dict[str, str], column: str) -> int:
    value = row.get(column, "")
    return int(value) if value else 0


def roadmap_tags(row: dict[str, str]) -> list[str]:
    return [tag for tag in row.get("roadmap_tags", "").split(";") if tag]


def unit_type(tag: str) -> str:
    if re.fullmatch(r"roadmap/m\d+", tag):
        return "milestone"
    if re.fullmatch(r"roadmap/m\d+-p.+", tag):
        return "phase"
    return "other"


def milestone_key(tag: str) -> str:
    match = re.match(r"roadmap/(m\d+)", tag)
    return match.group(1) if match else ""


def phase_key(tag: str) -> str:
    match = re.match(r"roadmap/m\d+-(p.+)", tag)
    return match.group(1) if match else ""


def completion_groups(rows: list[dict[str, str]]) -> list[dict[str, object]]:
    groups = []
    previous_end = 0
    for row in rows:
        tags = roadmap_tags(row)
        if not tags:
            continue
        end_index = int_value(row, "commit_index")
        start_index = previous_end + 1
        range_rows = [
            candidate
            for candidate in rows
            if start_index <= int_value(candidate, "commit_index") <= end_index
        ]
        groups.append(
            {
                "completion_group_index": len(groups) + 1,
                "range_start_commit_index": start_index,
                "range_end_commit_index": end_index,
                "range_rows": range_rows,
                "completion_row": row,
                "roadmap_tags": tags,
            }
        )
        previous_end = end_index
    return groups


def aggregate_range(range_rows: list[dict[str, str]]) -> dict[str, str | int]:
    result: dict[str, str | int] = {
        "range_commit_count": len(range_rows),
        "range_short_shas": ";".join(row["short_sha"] for row in range_rows),
    }
    if range_rows:
        result.update(
            {
                "range_first_commit_index": range_rows[0]["commit_index"],
                "range_first_short_sha": range_rows[0]["short_sha"],
                "range_first_committed_at": range_rows[0]["committed_at"],
                "range_last_commit_index": range_rows[-1]["commit_index"],
                "range_last_short_sha": range_rows[-1]["short_sha"],
                "range_last_committed_at": range_rows[-1]["committed_at"],
            }
        )
    else:
        result.update(
            {
                "range_first_commit_index": "",
                "range_first_short_sha": "",
                "range_first_committed_at": "",
                "range_last_commit_index": "",
                "range_last_short_sha": "",
                "range_last_committed_at": "",
            }
        )

    for column in CHANGE_COLUMNS:
        result[f"range_{column}"] = sum(int_value(row, column) for row in range_rows)

    result["range_commits_with_any_tags"] = sum(1 for row in range_rows if row.get("tags"))
    result["range_commits_with_roadmap_tags"] = sum(1 for row in range_rows if row.get("roadmap_tags"))
    result["range_commits_with_decision_tags"] = sum(1 for row in range_rows if row.get("decision_tags"))
    result["range_decision_tag_count"] = sum(
        len([tag for tag in row.get("decision_tags", "").split(";") if tag]) for row in range_rows
    )
    result["range_roadmap_tag_count"] = sum(
        len([tag for tag in row.get("roadmap_tags", "").split(";") if tag]) for row in range_rows
    )
    return result


def snapshot_at(row: dict[str, str], prefix: str = "completion") -> dict[str, int]:
    return {f"{prefix}_{column}": int_value(row, column) for column in SNAPSHOT_COLUMNS}


def snapshot_delta(current: dict[str, str], previous: dict[str, str] | None) -> dict[str, int]:
    result = {}
    for column in SNAPSHOT_COLUMNS:
        baseline = int_value(previous, column) if previous else 0
        result[f"delta_{column}"] = int_value(current, column) - baseline
    return result


def group_summary_rows(rows: list[dict[str, str]], groups: list[dict[str, object]]) -> list[dict[str, str | int]]:
    output = []
    previous_row: dict[str, str] | None = None
    for group in groups:
        completion_row = group["completion_row"]
        assert isinstance(completion_row, dict)
        range_rows = group["range_rows"]
        assert isinstance(range_rows, list)
        tags = group["roadmap_tags"]
        assert isinstance(tags, list)

        row: dict[str, str | int] = {
            "completion_group_index": group["completion_group_index"],
            "roadmap_tags": ";".join(tags),
            "milestones": ";".join(sorted({milestone_key(tag) for tag in tags if milestone_key(tag)})),
            "phase_tags": ";".join(tag for tag in tags if unit_type(tag) == "phase"),
            "milestone_tags": ";".join(tag for tag in tags if unit_type(tag) == "milestone"),
            "completion_commit_index": completion_row["commit_index"],
            "completion_committed_at": completion_row["committed_at"],
            "completion_short_sha": completion_row["short_sha"],
            "completion_sha": completion_row["sha"],
            "completion_subject": completion_row["subject"],
            "range_strategy": "since_previous_distinct_roadmap_completion",
            "range_start_commit_index": group["range_start_commit_index"],
            "range_end_commit_index": group["range_end_commit_index"],
        }
        row.update(aggregate_range(range_rows))
        row.update(snapshot_at(completion_row))
        row.update(snapshot_delta(completion_row, previous_row))
        output.append(row)
        previous_row = completion_row
    return output


def unit_rows(group_rows: list[dict[str, str | int]]) -> list[dict[str, str | int]]:
    output = []
    for group in group_rows:
        tags = str(group["roadmap_tags"]).split(";")
        for tag in tags:
            if not tag:
                continue
            row = {
                "roadmap_tag": tag,
                "unit_type": unit_type(tag),
                "milestone": milestone_key(tag),
                "phase": phase_key(tag),
                "shares_completion_group_with": group["roadmap_tags"],
                "range_strategy": "same_as_completion_group",
            }
            row.update(group)
            output.append(row)
    output.sort(key=lambda row: (int(row["completion_group_index"]), str(row["roadmap_tag"])))
    return output


def milestone_rollup_rows(group_rows: list[dict[str, str | int]]) -> list[dict[str, str | int]]:
    by_milestone: dict[str, list[dict[str, str | int]]] = defaultdict(list)
    for group in group_rows:
        tags = str(group["roadmap_tags"]).split(";")
        phase_milestones = sorted({milestone_key(tag) for tag in tags if unit_type(tag) == "phase"})
        for milestone in phase_milestones:
            by_milestone[milestone].append(group)

    output = []
    for milestone in sorted(by_milestone, key=lambda key: int(key[1:])):
        groups = by_milestone[milestone]
        first = groups[0]
        last = groups[-1]
        row: dict[str, str | int] = {
            "milestone": milestone,
            "unit_type": "milestone_rollup",
            "range_strategy": "sum_unique_completion_groups_with_child_phase_tags",
            "completion_group_count": len(groups),
            "completion_groups": ";".join(str(group["completion_group_index"]) for group in groups),
            "phase_tags": ";".join(
                tag
                for group in groups
                for tag in str(group["phase_tags"]).split(";")
                if tag.startswith(f"roadmap/{milestone}-")
            ),
            "range_first_commit_index": first["range_first_commit_index"],
            "range_first_short_sha": first["range_first_short_sha"],
            "range_first_committed_at": first["range_first_committed_at"],
            "range_last_commit_index": last["range_last_commit_index"],
            "range_last_short_sha": last["range_last_short_sha"],
            "range_last_committed_at": last["range_last_committed_at"],
            "completion_commit_index": last["completion_commit_index"],
            "completion_committed_at": last["completion_committed_at"],
            "completion_short_sha": last["completion_short_sha"],
            "completion_subject": last["completion_subject"],
        }
        for column in ["range_commit_count", *[f"range_{col}" for col in CHANGE_COLUMNS]]:
            row[column] = sum(int(group[column]) for group in groups)
        row["range_decision_tag_count"] = sum(int(group["range_decision_tag_count"]) for group in groups)
        row["range_roadmap_tag_count"] = sum(int(group["range_roadmap_tag_count"]) for group in groups)
        for column in SNAPSHOT_COLUMNS:
            row[f"completion_{column}"] = last[f"completion_{column}"]
        output.append(row)
    return output


def write_csv(path: Path, rows: list[dict[str, str | int]]) -> None:
    fieldnames = []
    seen = set()
    for row in rows:
        for key in row:
            if key not in seen:
                fieldnames.append(key)
                seen.add(key)
    with path.open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(rows)


def fmt(value: str | int) -> str:
    if isinstance(value, int):
        return f"{value:,}"
    text = str(value)
    if text == "":
        return ""
    try:
        return f"{int(text):,}"
    except ValueError:
        return text


def fmt_percent(numerator: int, denominator: int) -> str:
    if denominator <= 0:
        return "-"
    return f"{numerator * 100 / denominator:.0f}%"


def fmt_ratio(numerator: int, denominator: int) -> str:
    if denominator <= 0:
        return "-"
    return f"{numerator / denominator:.2f}"


def short_range(row: dict[str, str | int]) -> str:
    first_index = fmt(row.get("range_first_commit_index", ""))
    first_sha = row.get("range_first_short_sha", "")
    last_index = fmt(row.get("range_last_commit_index", ""))
    last_sha = row.get("range_last_short_sha", "")
    if first_index == last_index:
        return f"{first_index} `{first_sha}`"
    return f"{first_index} `{first_sha}` - {last_index} `{last_sha}`"


def snapshot_pair(row: dict[str, str | int], prefix: str, kind: str) -> str:
    files = fmt(row.get(f"{prefix}_snapshot_{kind}_files", 0))
    lines = fmt(row.get(f"{prefix}_snapshot_{kind}_lines", 0))
    return f"{files} / {lines}"


def table(headers: list[str], rows: list[list[str]]) -> str:
    def escape(value: str) -> str:
        return value.replace("|", "\\|")

    output = [
        "| " + " | ".join(escape(header) for header in headers) + " |",
        "| " + " | ".join("---" for _ in headers) + " |",
    ]
    for row in rows:
        output.append("| " + " | ".join(escape(cell) for cell in row) + " |")
    return "\n".join(output)


def roadmap_sort_key(tag: str) -> tuple[int, int, str, int]:
    match = re.fullmatch(r"roadmap/m(\d+)-p(\d+)([a-z]*)", tag)
    if match:
        milestone_number = int(match.group(1))
        phase_number = int(match.group(2))
        suffix = match.group(3)
        suffix_rank = -1 if suffix == "" else ord(suffix[0]) - ord("a")
        return (milestone_number, phase_number, suffix, suffix_rank)
    milestone_match = re.fullmatch(r"roadmap/m(\d+)", tag)
    if milestone_match:
        return (int(milestone_match.group(1)), 999, "", -1)
    return (999, 999, tag, -1)


def ordered_unique(values: list[str]) -> list[str]:
    output = []
    seen = set()
    for value in values:
        if not value or value in seen:
            continue
        seen.add(value)
        output.append(value)
    return output


def related_tags_by_commit_index(
    rows: list[dict[str, str]],
    groups: list[dict[str, object]],
) -> dict[int, dict[str, list[str]]]:
    related = {
        int_value(row, "commit_index"): {"milestones": [], "phases": []}
        for row in rows
    }

    for group in groups:
        tags = group["roadmap_tags"]
        assert isinstance(tags, list)
        range_rows = group["range_rows"]
        assert isinstance(range_rows, list)

        milestones = ordered_unique(
            [milestone_key(tag).upper() for tag in tags if milestone_key(tag)]
        )
        phases = ordered_unique(
            [tag.replace("roadmap/", "") for tag in tags if unit_type(tag) == "phase"]
        )

        for row in range_rows:
            commit_index = int_value(row, "commit_index")
            related[commit_index] = {
                "milestones": milestones,
                "phases": phases,
            }

    return related


def format_related(values: list[str]) -> str:
    return ", ".join(values) if values else "-"


def format_date(value: str) -> str:
    if not value or len(value) < 10:
        return "-"
    return f"{value[5:7]}/{value[8:10]}"


def format_commit_index_ranges(indexes: list[int]) -> str:
    if not indexes:
        return "-"

    ordered = sorted(indexes)
    ranges = []
    start = ordered[0]
    end = ordered[0]

    for value in ordered[1:]:
        if value == end + 1:
            end = value
            continue
        ranges.append(f"{start}" if start == end else f"{start}-{end}")
        start = value
        end = value

    ranges.append(f"{start}" if start == end else f"{start}-{end}")
    return ", ".join(ranges)


def format_date_interval(group_rows: list[dict[str, str]]) -> str:
    if not group_rows:
        return "-"
    if len(group_rows) == 1:
        return format_date(group_rows[0]["committed_at"])
    return f"{format_date(group_rows[0]['committed_at'])} ~ {format_date(group_rows[-1]['committed_at'])}"


def format_milestone_label(code: str, milestone_titles: dict[str, str]) -> str:
    title = milestone_titles.get(code.upper(), "")
    return f"{code.upper()} - {title}" if title else code.upper()


def format_phase_label(code: str, phase_titles: dict[str, str]) -> str:
    title = phase_titles.get(code.upper(), "")
    return f"{code.upper()} - {title}" if title else code.upper()


def commit_report_rows(
    rows: list[dict[str, str]],
    groups: list[dict[str, object]],
    milestone_titles: dict[str, str],
    phase_titles: dict[str, str],
) -> list[dict[str, str | int]]:
    related = related_tags_by_commit_index(rows, groups)
    output: list[dict[str, str | int]] = []

    for row in rows:
        commit_index = int_value(row, "commit_index")
        relation = related.get(commit_index, {"milestones": [], "phases": []})
        output.append(
            {
                "sequence": commit_index,
                "sha": row["sha"],
                "short_sha": row["short_sha"],
                "committed_at": format_date(row["committed_at"]),
                "milestones": format_related(
                    [format_milestone_label(code, milestone_titles) for code in relation["milestones"]]
                ),
                "phases": format_related(
                    [format_phase_label(code, phase_titles) for code in relation["phases"]]
                ),
                "docs_changed_files": int_value(row, "docs_changed_files"),
                "docs_lines_changed": int_value(row, "docs_lines_changed"),
                "decision_changed_files": int_value(row, "decision_changed_files"),
                "major_core_decision_changed_files": int_value(row, "major_core_decision_changed_files"),
                "backtracking_decision_changed_files": int_value(row, "backtracking_decision_changed_files"),
                "src_changed_files": int_value(row, "src_changed_files"),
                "src_lines_changed": int_value(row, "src_lines_changed"),
                "core_abstract_changed_files": int_value(row, "core_abstract_changed_files"),
                "core_abstract_lines_changed": int_value(row, "core_abstract_lines_changed"),
                "test_changed_files": int_value(row, "test_changed_files"),
                "test_lines_changed": int_value(row, "test_lines_changed"),
                "delta_public_interfaces": int_value(row, "delta_public_interfaces"),
                "delta_public_methods": int_value(row, "delta_public_methods"),
                "delta_public_types": int_value(row, "delta_public_types"),
                "delta_public_contract_surface": int_value(row, "delta_public_contract_surface"),
                "contract_churn": int_value(row, "contract_churn"),
                "delta_test_fixtures": int_value(row, "delta_test_fixtures"),
                "delta_test_cases": int_value(row, "delta_test_cases"),
            }
        )

    return output


def grouped_report_rows(
    rows: list[dict[str, str]],
    groups: list[dict[str, object]],
    key: str,
    milestone_titles: dict[str, str],
    phase_titles: dict[str, str],
) -> list[dict[str, str | int]]:
    related = related_tags_by_commit_index(rows, groups)
    buckets: dict[str, list[dict[str, str]]] = defaultdict(list)

    for row in rows:
        commit_index = int_value(row, "commit_index")
        labels = related.get(commit_index, {"milestones": [], "phases": []})[key]
        for label in labels:
            buckets[label].append(row)

    def sort_key(item: tuple[str, list[dict[str, str]]]) -> tuple[int, str]:
        _, group_rows = item
        first_index = int_value(group_rows[0], "commit_index") if group_rows else 999999
        return (first_index, item[0])

    output: list[dict[str, str | int]] = []
    for label, group_rows in sorted(buckets.items(), key=sort_key):
        indexes = [int_value(row, "commit_index") for row in group_rows]
        display_label = (
            format_milestone_label(label, milestone_titles)
            if key == "milestones"
            else format_phase_label(label, phase_titles)
        )
        output.append(
            {
                "label": display_label,
                "sequence_range": format_commit_index_ranges(indexes),
                "date_interval": format_date_interval(group_rows),
                "docs_changed_files": sum(int_value(row, "docs_changed_files") for row in group_rows),
                "docs_lines_changed": sum(int_value(row, "docs_lines_changed") for row in group_rows),
                "decision_changed_files": sum(int_value(row, "decision_changed_files") for row in group_rows),
                "major_core_decision_changed_files": sum(
                    int_value(row, "major_core_decision_changed_files") for row in group_rows
                ),
                "backtracking_decision_changed_files": sum(
                    int_value(row, "backtracking_decision_changed_files") for row in group_rows
                ),
                "src_changed_files": sum(int_value(row, "src_changed_files") for row in group_rows),
                "src_lines_changed": sum(int_value(row, "src_lines_changed") for row in group_rows),
                "core_abstract_changed_files": sum(
                    int_value(row, "core_abstract_changed_files") for row in group_rows
                ),
                "core_abstract_lines_changed": sum(
                    int_value(row, "core_abstract_lines_changed") for row in group_rows
                ),
                "test_changed_files": sum(int_value(row, "test_changed_files") for row in group_rows),
                "test_lines_changed": sum(int_value(row, "test_lines_changed") for row in group_rows),
                "delta_public_interfaces": sum(int_value(row, "delta_public_interfaces") for row in group_rows),
                "delta_public_methods": sum(int_value(row, "delta_public_methods") for row in group_rows),
                "delta_public_types": sum(int_value(row, "delta_public_types") for row in group_rows),
                "delta_public_contract_surface": sum(
                    int_value(row, "delta_public_contract_surface") for row in group_rows
                ),
                "contract_churn": sum(int_value(row, "contract_churn") for row in group_rows),
                "delta_test_fixtures": sum(int_value(row, "delta_test_fixtures") for row in group_rows),
                "delta_test_cases": sum(int_value(row, "delta_test_cases") for row in group_rows),
            }
        )

    return output


def commit_markdown_rows(rows: list[dict[str, str | int]]) -> list[list[str]]:
    output = []
    for row in rows:
        output.append(
            [
                fmt(row["sequence"]),
                f"`{row['sha']}` / {row['committed_at']}",
                f"{row['milestones']} / {row['phases']}",
                f"{fmt(row['docs_changed_files'])} / {fmt(row['docs_lines_changed'])}",
                fmt(row["decision_changed_files"]),
                fmt(row["major_core_decision_changed_files"]),
                fmt(row["backtracking_decision_changed_files"]),
                f"{fmt(row['core_abstract_changed_files'])} / {fmt(row['core_abstract_lines_changed'])}",
                f"{fmt(row['src_changed_files'])} / {fmt(row['src_lines_changed'])}",
                f"{fmt(row['test_changed_files'])} / {fmt(row['test_lines_changed'])}",
                f"{fmt(row['delta_public_interfaces'])} / {fmt(row['delta_public_methods'])} / {fmt(row['delta_public_types'])}",
                fmt(row["contract_churn"]),
                f"{fmt(row['delta_test_fixtures'])} / {fmt(row['delta_test_cases'])}",
                fmt_percent(int(row["core_abstract_lines_changed"]), int(row["src_lines_changed"])),
                fmt_ratio(int(row["delta_test_cases"]), int(row["delta_public_contract_surface"])),
            ]
        )
    return output


def grouped_markdown_rows(rows: list[dict[str, str | int]]) -> list[list[str]]:
    output = []
    for row in rows:
        output.append(
            [
                str(row["label"]),
                str(row["sequence_range"]),
                str(row["date_interval"]),
                f"{fmt(row['docs_changed_files'])} / {fmt(row['docs_lines_changed'])}",
                fmt(row["decision_changed_files"]),
                fmt(row["major_core_decision_changed_files"]),
                fmt(row["backtracking_decision_changed_files"]),
                f"{fmt(row['core_abstract_changed_files'])} / {fmt(row['core_abstract_lines_changed'])}",
                f"{fmt(row['src_changed_files'])} / {fmt(row['src_lines_changed'])}",
                f"{fmt(row['test_changed_files'])} / {fmt(row['test_lines_changed'])}",
                f"{fmt(row['delta_public_interfaces'])} / {fmt(row['delta_public_methods'])} / {fmt(row['delta_public_types'])}",
                fmt(row["contract_churn"]),
                f"{fmt(row['delta_test_fixtures'])} / {fmt(row['delta_test_cases'])}",
                fmt_percent(int(row["core_abstract_lines_changed"]), int(row["src_lines_changed"])),
                fmt_ratio(int(row["delta_test_cases"]), int(row["delta_public_contract_surface"])),
            ]
        )
    return output


def row_by_label(rows: list[dict[str, str | int]], prefix: str) -> dict[str, str | int]:
    for row in rows:
        if str(row["label"]).startswith(prefix):
            return row
    raise ValueError(f"missing report row: {prefix}")


def phase_lines(phase_rows: list[dict[str, str | int]]) -> list[str]:
    phase = {str(row["label"]).split(" - ", 1)[0]: row for row in phase_rows}

    def p(key: str, summary: str) -> str:
        row = phase[key]
        return (
            f"- `{row['label']}`：{summary}"
            f"本階段統計為 docs `{fmt(row['docs_changed_files'])} / {fmt(row['docs_lines_changed'])}`、"
            f"decisions `{fmt(row['decision_changed_files'])}`、"
            f"Major/Core `{fmt(row['major_core_decision_changed_files'])}`、"
            f"Backtracking `{fmt(row['backtracking_decision_changed_files'])}`、"
            f"Core/Abstract `{fmt(row['core_abstract_changed_files'])} / {fmt(row['core_abstract_lines_changed'])}`、"
            f"src `{fmt(row['src_changed_files'])} / {fmt(row['src_lines_changed'])}`、"
            f"tests `{fmt(row['test_changed_files'])} / {fmt(row['test_lines_changed'])}`。"
        )

    return [
        "## Phase Summary",
        "",
        "### M1 Phase Pattern",
        "",
        p(
            "M1-P1",
            "建立 shop runtime 與 discount rule contract，先把折扣擴充點從既有實作抽出來，讓後續 shop 差異可以透過 manifest 與 rule composition 接入。",
        ),
        "",
        p(
            "M1-P2",
            "建立 product service 與 order event 的第一版邊界，把商品查詢、hidden product 與 checkout 後副作用從固定資料結構中拆開。",
        ),
        "",
        p(
            "M1-P3",
            "將 checkout orchestration 從 API controller 移入 `.Core`，讓交易一致性、buyer authorization 與 checkout lifecycle 成為可測試的核心能力。",
        ),
        "",
        p(
            "M1-P4",
            "針對後續 AppleBTS 暴露出的 cart line、SKU 與 inventory correctness 缺口，回補為通用 Core 能力，而不是讓第一個客戶案例自行承擔。",
        ),
        "",
        p(
            "M1-P5",
            "把時間來源、buyer satisfaction 語意與 cart line 操作收斂為可測試的共同能力，避免 storefront 或 vertical flow 以臨時方式繞過主流程。",
        ),
        "",
        p(
            "M1-P6",
            "將 checkout 後副作用從 product service callback 拆成 order event dispatcher，讓 `IProductService` 回歸商品查詢邊界，並為 PetShop reservation confirmed transition 預留穩定接點。",
        ),
        "",
        "M1 的 phase pattern 顯示，架構工作不是一次性畫圖，而是在具體案例暴露缺口時，持續判斷哪些能力應回到 `.Core` / `.Abstract`，哪些應留在 extension。這也是後續能交給 AI coding agent 大量實作的前提：agent 需要穩定邊界，否則只會把案例需求直接寫進主流程。",
        "",
        "### M2 Phase Pattern",
        "",
        p(
            "M2-P1",
            "建立 storefront family、BFF、auth/session 與 browser 驗收原則，讓後續 vertical storefront 不必重新發明 UI / BFF 架構。",
        ),
        "",
        p(
            "M2-P2",
            "實作 CommonStorefront 與本機驗證拓樸，把 M1 的 Core 能力落成可被重用的標準商店 flow。",
        ),
        "",
        "M2 的 phase pattern 顯示，當 shared storefront grammar 與本機驗證拓樸定義完成後，實作工作可以明顯轉向可交付的 API / UI code。這一層把後續 AI coding agent 的工作從「理解整個系統」降低為「沿用既有 family pattern 實作新的 vertical」。",
        "",
        "### M3 Phase Pattern",
        "",
        p(
            "M3-P1",
            "先用 AppleBTS campaign 檢查 business / technical boundary，決定 BTS 是同一 shop 的 campaign，而不是另一個 shop 或另一套 checkout。",
        ),
        "",
        p(
            "M3-P2",
            "凍結 AppleBTS spec、testcase 與 extension skeleton，讓 campaign、offer、qualification 與 gift subsidy 成為 extension 內的明確模型。",
        ),
        "",
        p(
            "M3-P3",
            "實作 AppleBTS API、seed、module registration 與 local topology，驗證 extension 能被獨立啟動並透過標準 cart / checkout 接入主流程。",
        ),
        "",
        p(
            "M3-P4",
            "建立 AppleBTS Storefront，沿用 CommonStorefront 的 auth/session/BFF/UI grammar，只補 BTS catalog、qualification 與 gift flow orchestration。",
        ),
        "",
        p(
            "M3-P5",
            "把折扣輸出與部署文件收斂在 AppleBTS extension / topology 層，證明業務語意修正可以留在 vertical 層，不必回頭改 checkout 主流程。",
        ),
        "",
        "M3 的 phase pattern 是第一個擴充案例的驗證：初期仍會暴露少量通用缺口，需要架構師判斷是否回補 Core；但大部分產出已轉向 extension API、storefront、seed、tests 與部署。這代表設計方法開始把人為介入集中在邊界判斷，而不是日常 coding。",
        "",
        "### M4 Phase Pattern",
        "",
        p(
            "M4-P1A",
            "先固定 reservation 與 hidden standard `Product` projection 的核心模型，讓 PetShop 這種服務預約案例也能接入既有 cart / checkout / order event flow。",
        ),
        "",
        p(
            "M4-P2A",
            "實作 PetShop extension domain、repository、product service decorator 與 order event dispatcher，直接在 extension boundary 內完成 domain 行為。",
        ),
        "",
        p(
            "M4-P4",
            "補上 PetShop reservation 搭配一般商品滿額折扣，將促銷規則保留在 extension discount rule，而不是放進 checkout 主流程。",
        ),
        "",
        p(
            "M4-P1B",
            "定義 reservation lifecycle 與 API contract，讓 storefront 或測試流程能透過 PetShop API 建立 hold、查詢狀態與取得 checkout product id。",
        ),
        "",
        p(
            "M4-P2B",
            "實作 `/petshop-api/*`，涵蓋 service catalog、availability、reservation hold、owner isolation、cancel hold 與 reservation status。",
        ),
        "",
        p(
            "M4-P2C",
            "完成 PetShop host、seed、config 與 compose topology，讓 PetShop 可以用獨立 shop runtime 做 API-level E2E 驗證。",
        ),
        "",
        p(
            "M4-P3A",
            "固定 PetShop Storefront 的 route、BFF client、testcase 與最小 skeleton，讓後續頁面實作遵循已定義的 storefront family pattern。",
        ),
        "",
        p(
            "M4-P3",
            "完成 PetShop consumer-facing reservation、cart、checkout、member reservation/order flow 與 browser smoke，讓完整 vertical storefront 在既有主流程上運作。",
        ),
        "",
        p(
            "M4-P3B",
            "實作 reservation flow pages，讓使用者可以建立 hold、加入標準 cart，並在 checkout 前取消 hold。",
        ),
        "",
        p(
            "M4-P3C",
            "補齊 member/order integration 與 browser smoke，確認 checkout completed 後 reservation 可以轉為 confirmed，並可在 storefront 看到狀態。",
        ),
        "",
        "M4 的 phase pattern 是整份報告最強的成效訊號：PetShop 是與 AppleBTS 結構差異很大的第二個 vertical case，但所有 phase 的 Major/Core Decisions、Backtracking Decisions 與 Core/Abstract 異動都維持為 0。這代表架構師不需要重新打開主流程，AI coding agent 可以直接在既有 extension、API、storefront 與 test pattern 中完成高工作量輸出。",
    ]


def analysis_lines(
    milestone_rows: list[dict[str, str | int]],
    phase_rows: list[dict[str, str | int]],
) -> list[str]:
    m1 = row_by_label(milestone_rows, "M1 - ")
    m2 = row_by_label(milestone_rows, "M2 - ")
    m3 = row_by_label(milestone_rows, "M3 - ")
    m4 = row_by_label(milestone_rows, "M4 - ")

    return [
        "## Interpretation Summary",
        "",
        "這個 side project 的設計目標，不只是做出幾個 demo shop，而是驗證一種架構工作方法：先把主流程、系統層級邊界、contract 與 `.Core` orchestration 設計到穩定，讓大型客戶的高度差異化需求可以透過 extension、sidecar data、vertical API 與 storefront 擴充，而不是修改既有 binary code 或重寫主流程。",
        "",
        "本報告的解讀前提是：此 side project 的一般 coding 主要由 AI coding agent 完成；人為介入集中在 `.Core` / `.Abstract` 的 interface code review、架構邊界判斷與 decision 收斂。因此，src / tests 異動量可視為 AI coding agent 承接的實作 workload proxy；Major/Core Decisions、Backtracking Decisions 與 Core/Abstract 異動則反映架構師需要介入主流程或核心邊界的程度。",
        "",
        "較精準的結論不是「後期沒有 decision」，而是「後期仍有局部 decision 記錄，但 Major/Core Decisions 與 Backtracking Decisions 下降到 0，且 Core/Abstract 異動下降到 0」。這表示後期仍保留設計紀錄與 review trace，但不再需要回頭修改系統主幹。",
        "",
        "## Milestone Summary",
        "",
        f"### {m1['label']}",
        "",
        f"M1 的目的，是把原本由 API controller、固定 product collection 與單一折扣邏輯主導的系統，整理成可支援模組化商店的共同基礎。成果包含 `.Abstract` contract、`.Core` orchestration、shop runtime、discount rule、product service、checkout service、line-based cart、SKU / inventory、TimeProvider 與 order event dispatcher。統計上，M1 有 decisions `{fmt(m1['decision_changed_files'])}`、Major/Core `{fmt(m1['major_core_decision_changed_files'])}`、Backtracking `{fmt(m1['backtracking_decision_changed_files'])}`、Core/Abstract `{fmt(m1['core_abstract_changed_files'])} / {fmt(m1['core_abstract_lines_changed'])}`，Core Touch `{fmt_percent(int(m1['core_abstract_lines_changed']), int(m1['src_lines_changed']))}`。這是架構投入最集中的階段，反映前期確實把不穩定性集中在核心邊界與主流程設計上處理。",
        "",
        f"### {m2['label']}",
        "",
        f"M2 的目的，是建立標準商店 baseline，讓後續 vertical extension 可以重用 storefront family、BFF、auth/session、cart、checkout、member order 與本機驗證拓樸。統計上，M2 有 decisions `{fmt(m2['decision_changed_files'])}`、Major/Core `{fmt(m2['major_core_decision_changed_files'])}`、Backtracking `{fmt(m2['backtracking_decision_changed_files'])}`、Core/Abstract `{fmt(m2['core_abstract_changed_files'])} / {fmt(m2['core_abstract_lines_changed'])}`，src `{fmt(m2['src_changed_files'])} / {fmt(m2['src_lines_changed'])}`。這一段的重點是把 M1 的 Core 能力落地成可複製的標準應用骨架，讓後續 AI coding agent 可以沿用固定的 API / Storefront pattern。",
        "",
        f"### {m3['label']}",
        "",
        f"M3 的目的，是以 AppleBTS 驗證第一個大型客戶式 vertical extension：campaign、offer、qualification、gift subsidy、BTS discount rule、AppleBTS API、AppleBTS Storefront 與 deployment topology。統計上，M3 有 decisions `{fmt(m3['decision_changed_files'])}`、Major/Core `{fmt(m3['major_core_decision_changed_files'])}`、Backtracking `{fmt(m3['backtracking_decision_changed_files'])}`，但 Core/Abstract 只剩 `{fmt(m3['core_abstract_changed_files'])} / {fmt(m3['core_abstract_lines_changed'])}`，Core Touch `{fmt_percent(int(m3['core_abstract_lines_changed']), int(m3['src_lines_changed']))}`。這表示第一個 vertical case 仍會暴露少量通用缺口，需要架構師判斷哪些能力應回補 Core；但主要 workload 已經轉向 extension API、storefront、seed、tests 與部署。",
        "",
        f"### {m4['label']}",
        "",
        f"M4 的目的，是以 PetShop 驗證第二個、且結構差異更大的 vertical extension：服務預約不是一般商品，但可透過 hidden standard `Product` 接入既有 cart / checkout / order event flow。統計上，M4 有 decisions `{fmt(m4['decision_changed_files'])}`、src `{fmt(m4['src_changed_files'])} / {fmt(m4['src_lines_changed'])}`、tests `{fmt(m4['test_changed_files'])} / {fmt(m4['test_lines_changed'])}`，但 Major/Core `{fmt(m4['major_core_decision_changed_files'])}`、Backtracking `{fmt(m4['backtracking_decision_changed_files'])}`、Core/Abstract `{fmt(m4['core_abstract_changed_files'])} / {fmt(m4['core_abstract_lines_changed'])}`，Core Touch `{fmt_percent(int(m4['core_abstract_lines_changed']), int(m4['src_lines_changed']))}`。這是最強的證明點：後期仍有大量程式碼與測試產出，但不需要重新打開 `.Core` / `.Abstract`，代表前期架構設計已足以支撐 AI coding agent 在穩定邊界內完成高 workload 擴充。",
        "",
        *phase_lines(phase_rows),
    ]


def write_markdown(
    path: Path,
    commit_rows: list[dict[str, str | int]],
    milestone_rows: list[dict[str, str | int]],
    phase_rows: list[dict[str, str | int]],
) -> None:
    commit_headers = [
        "序號",
        "Commit SHA / Date",
        "Related Milestone / Phase",
        "Docs Files / Lines",
        "Decision Files",
        "Major/Core Decisions",
        "Backtracking Decisions",
        "Core/Abstract Src Files / Lines",
        "Src Files / Lines",
        "Tests Files / Lines",
        "Public Contract Δ I/M/T",
        "Contract Churn",
        "Test Δ Fixtures/Cases",
        "Core Touch %",
        "Test/Contract Ratio",
    ]
    grouped_headers = [
        "Milestone / Phase",
        "序號區間",
        "日期區間",
        "Docs Files / Lines",
        "Decision Files",
        "Major/Core Decisions",
        "Backtracking Decisions",
        "Core/Abstract Src Files / Lines",
        "Src Files / Lines",
        "Tests Files / Lines",
        "Public Contract Δ I/M/T",
        "Contract Churn",
        "Test Δ Fixtures/Cases",
        "Core Touch %",
        "Test/Contract Ratio",
    ]
    lines = [
        "# Roadmap Commit Metrics",
        "",
        "## 統計口徑",
        "",
        "- 來源資料：`docs/metrics/git-commit-metrics.csv`，一列一個 git commit。",
        f"- 本報告只保留 `{REPORT_CUTOFF_DATE}` 之後的 commit；原始序號 1-64 不列入。",
        "- 依 commit 順序完整列出，每列代表單一 commit 的異動量，不是 snapshot 累計量。",
        "- `Commit SHA / Date` 使用完整 commit SHA 與 `MM/DD`。",
        "- `Related Milestone / Phase` 依目前 roadmap completion group 歸屬計算，並載入 `docs/project-roadmap.md` 的標題；同一段 commit range 會對應到同一組 milestone / phase。",
        "- `Docs Files / Lines` 統計 `/docs + /spec` 的異動檔案事件數與異動行數。",
        "- `Decision Files` 只統計 `/docs/decisions/`，並排除 `README.md`。",
        "- `Major/Core Decisions` 統計 decision 內容含 `重大決策` 或 `影響 .Core` 的異動檔案事件數。",
        "- `Backtracking Decisions` 統計 decision 內容含 `回頭修正 Phase 1` 或 `影響 .Abstract / spec` 的異動檔案事件數。",
        "- `Core/Abstract Src Files / Lines` 只統計 `.Core` 與 `.Abstract` 專案下的 `/src/*.cs` 異動。",
        "- `Src Files / Lines` 只統計 `/src/*.cs` 的異動檔案事件數與異動行數。",
        "- `Tests Files / Lines` 只統計 `/tests/*.cs` 的異動檔案事件數與異動行數。",
        "- `Public Contract Δ I/M/T` 代表 public interface / method / type 的 snapshot delta；`Contract Churn` 是 public contract signature added + removed。",
        "- `Test Δ Fixtures/Cases` 代表 test fixture / `[Fact]` + `[Theory]` 的 snapshot delta；`Test/Contract Ratio` 是 test case delta 除以 public contract surface delta。",
        "- `Core Touch %` 是 Core/Abstract src changed lines 除以所有 src changed lines。",
        "- milestone / phase 統計表的數字一律用 `Sum()`；若某 commit 同時關聯多個 milestone / phase，會分別計入各自的 group。",
        "- `序號區間` 會壓縮成連續區間列表，例如 `65-81, 91, 94`；`日期區間` 只保留 `MM/DD`。",
        "",
        "## Commit Details",
        "",
        table(commit_headers, commit_markdown_rows(commit_rows)),
        "",
        "## Group By Milestone",
        "",
        table(grouped_headers, grouped_markdown_rows(milestone_rows)),
        "",
        "## Group By Phase",
        "",
        table(grouped_headers, grouped_markdown_rows(phase_rows)),
        "",
        *analysis_lines(milestone_rows, phase_rows),
        "",
    ]
    path.write_text("\n".join(lines), encoding="utf-8")


def main() -> int:
    rows = read_rows()
    filtered_rows = filter_report_rows(rows)
    groups = completion_groups(rows)
    milestone_titles, phase_titles = load_roadmap_titles()
    commit_rows = commit_report_rows(filtered_rows, groups, milestone_titles, phase_titles)
    milestone_rows = grouped_report_rows(filtered_rows, groups, "milestones", milestone_titles, phase_titles)
    phase_rows = grouped_report_rows(filtered_rows, groups, "phases", milestone_titles, phase_titles)

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    write_markdown(MARKDOWN_OUTPUT, commit_rows, milestone_rows, phase_rows)
    print(f"wrote {MARKDOWN_OUTPUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
