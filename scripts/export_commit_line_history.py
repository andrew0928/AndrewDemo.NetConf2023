#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import html
import math
import subprocess
from dataclasses import dataclass
from datetime import date, datetime
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CSV_OUTPUT = ROOT / "docs" / "metrics" / "commit-line-history.csv"
DEFAULT_SVG_OUTPUT = ROOT / "docs" / "metrics" / "commit-line-history.svg"
AI_START = date(2026, 3, 1)
SECTION_MARKERS = (" milestones:", " changes:", " fixes:", " comments:")

CATEGORIES = {
    "docs_spec": {
        "label": "docs + spec",
        "prefixes": ("docs/", "spec/", "specs/"),
        "color": "#2563eb",
    },
    "src": {
        "label": "src",
        "prefixes": ("src/",),
        "color": "#dc2626",
    },
    "tests": {
        "label": "tests",
        "prefixes": ("tests/",),
        "color": "#16a34a",
    },
}


@dataclass
class CommitMeta:
    commit_index: int
    sha: str
    short_sha: str
    committed_at: str
    parent_count: int
    subject: str

    @property
    def committed_date(self) -> str:
        return self.committed_at[:10]

    @property
    def title(self) -> str:
        title = self.subject
        for marker in SECTION_MARKERS:
            marker_index = title.find(marker)
            if marker_index >= 0:
                title = title[:marker_index]
        return title.strip()


def run_git(*args: str, check: bool = True, text: bool = True) -> str | bytes:
    result = subprocess.run(
        ["git", *args],
        cwd=ROOT,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if check and result.returncode != 0:
        raise RuntimeError(
            f"git {' '.join(args)} failed with exit code {result.returncode}: "
            f"{result.stderr.decode('utf-8', errors='replace')}"
        )
    if text:
        return result.stdout.decode("utf-8", errors="replace")
    return result.stdout


def commit_metadata() -> list[CommitMeta]:
    raw = run_git(
        "log",
        "--reverse",
        "--date=iso-strict",
        "--pretty=format:%H%x00%h%x00%cI%x00%P%x00%s",
    )
    commits: list[CommitMeta] = []
    for commit_index, line in enumerate(raw.splitlines(), start=1):
        if not line:
            continue
        sha, short_sha, committed_at, parents, subject = line.split("\x00", 4)
        commits.append(
            CommitMeta(
                commit_index=commit_index,
                sha=sha,
                short_sha=short_sha,
                committed_at=committed_at,
                parent_count=len([parent for parent in parents.split() if parent]),
                subject=subject,
            )
        )
    return commits


def category_for(path: str) -> str | None:
    for name, spec in CATEGORIES.items():
        if path.startswith(spec["prefixes"]):
            return name
    return None


def line_counts(commit: CommitMeta) -> dict[str, int]:
    output = run_git("grep", "-I", "-c", "^", commit.sha, check=False)
    counts = {name: 0 for name in CATEGORIES}
    prefix = f"{commit.sha}:"
    for line in output.splitlines():
        if not line.startswith(prefix) or ":" not in line[len(prefix) :]:
            continue
        path, count_text = line[len(prefix) :].rsplit(":", 1)
        category = category_for(path)
        if not category:
            continue
        try:
            counts[category] += int(count_text)
        except ValueError:
            continue
    return counts


def numstat_bytes(commit: CommitMeta) -> bytes:
    parents = run_git("show", "-s", "--format=%P", commit.sha).split()
    if parents:
        return run_git(
            "diff",
            "--numstat",
            "--find-renames",
            "-z",
            parents[0],
            commit.sha,
            "--",
            text=False,
        )
    return run_git(
        "diff-tree",
        "--root",
        "--numstat",
        "--find-renames",
        "-r",
        "--no-commit-id",
        "-z",
        commit.sha,
        "--",
        text=False,
    )


def parse_line_count(value: bytes) -> int:
    text = value.decode("utf-8", errors="replace")
    return 0 if text == "-" else int(text)


def changed_lines(commit: CommitMeta) -> dict[str, int]:
    parts = numstat_bytes(commit).split(b"\x00")
    counts = {name: 0 for name in CATEGORIES}
    index = 0
    while index < len(parts):
        token = parts[index]
        index += 1
        if not token:
            continue

        fields = token.split(b"\t")
        if len(fields) < 3:
            continue

        added = parse_line_count(fields[0])
        deleted = parse_line_count(fields[1])
        path_bytes = fields[2]
        if path_bytes:
            path = path_bytes.decode("utf-8", errors="replace")
        else:
            if index + 1 >= len(parts):
                break
            index += 1
            path = parts[index].decode("utf-8", errors="replace")
            index += 1

        category = category_for(path)
        if category:
            counts[category] += added + deleted
    return counts


def build_rows() -> list[dict[str, str | int]]:
    rows: list[dict[str, str | int]] = []
    for commit in commit_metadata():
        totals = line_counts(commit)
        changes = changed_lines(commit)
        rows.append(
            {
                "commit_index": commit.commit_index,
                "committed_date": commit.committed_date,
                "committed_at": commit.committed_at,
                "short_sha": commit.short_sha,
                "sha": commit.sha,
                "parent_count": commit.parent_count,
                "title": commit.title,
                "docs_spec_total_lines": totals["docs_spec"],
                "docs_spec_changed_lines": changes["docs_spec"],
                "src_total_lines": totals["src"],
                "src_changed_lines": changes["src"],
                "tests_total_lines": totals["tests"],
                "tests_changed_lines": changes["tests"],
            }
        )
    return rows


def write_csv(rows: list[dict[str, str | int]], output: Path) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)


def nice_max(value: int) -> int:
    if value <= 0:
        return 1
    magnitude = 10 ** math.floor(math.log10(value))
    normalized = value / magnitude
    if normalized <= 2:
        nice = 2
    elif normalized <= 5:
        nice = 5
    else:
        nice = 10
    return int(nice * magnitude)


def ticks(max_value: int, count: int = 5) -> list[int]:
    top = nice_max(max_value)
    step = top / count
    return [round(step * i) for i in range(count + 1)]


def polyline(values: list[int], x_for: callable, y_for: callable) -> str:
    return " ".join(f"{x_for(index + 1):.2f},{y_for(value):.2f}" for index, value in enumerate(values))


def x_ticks(commit_count: int) -> list[int]:
    if commit_count <= 12:
        return list(range(1, commit_count + 1))
    values = list(range(1, commit_count + 1, 10))
    if values[-1] != commit_count:
        values.append(commit_count)
    return values


def render_panel(
    rows: list[dict[str, str | int]],
    *,
    x: int,
    y: int,
    width: int,
    height: int,
    title: str,
    value_suffix: str,
    series_columns: dict[str, str],
) -> list[str]:
    commit_count = len(rows)
    max_value = max(int(row[column]) for row in rows for column in series_columns.values())
    y_axis_max = nice_max(max_value)

    def x_for(commit_index: int) -> float:
        if commit_count == 1:
            return x
        return x + (commit_index - 1) * width / (commit_count - 1)

    def y_for(value: int) -> float:
        return y + height - (value / y_axis_max) * height

    parts = [
        f'<text x="{x}" y="{y - 18}" class="panel-title">{html.escape(title)}</text>',
        f'<rect x="{x}" y="{y}" width="{width}" height="{height}" class="plot-bg"/>',
    ]

    for tick in ticks(max_value):
        ty = y_for(tick)
        parts.append(f'<line x1="{x}" y1="{ty:.2f}" x2="{x + width}" y2="{ty:.2f}" class="grid"/>')
        parts.append(
            f'<text x="{x - 10}" y="{ty + 4:.2f}" class="axis-label" text-anchor="end">{tick:,}</text>'
        )

    for tick in x_ticks(commit_count):
        tx = x_for(tick)
        original_commit_index = rows[tick - 1]["commit_index"]
        parts.append(f'<line x1="{tx:.2f}" y1="{y + height}" x2="{tx:.2f}" y2="{y + height + 6}" class="axis"/>')
        parts.append(
            f'<text x="{tx:.2f}" y="{y + height + 22}" class="axis-label" text-anchor="middle">{original_commit_index}</text>'
        )

    ai_index = next(
        (
            position
            for position, row in enumerate(rows, start=1)
            if date.fromisoformat(str(row["committed_date"])) >= AI_START
        ),
        None,
    )
    if ai_index:
        ax = x_for(ai_index)
        parts.append(f'<line x1="{ax:.2f}" y1="{y}" x2="{ax:.2f}" y2="{y + height}" class="ai-line"/>')
        parts.append(
            f'<text x="{ax + 6:.2f}" y="{y + 14}" class="ai-label">AI start {AI_START:%Y/%m/%d}</text>'
        )

    for name, column in series_columns.items():
        values = [int(row[column]) for row in rows]
        color = CATEGORIES[name]["color"]
        points = polyline(values, x_for, y_for)
        parts.append(f'<polyline points="{points}" fill="none" stroke="{color}" class="series-line"/>')

    parts.append(f'<text x="{x + width}" y="{y + height + 42}" class="axis-title" text-anchor="end">commit index</text>')
    parts.append(f'<text x="{x}" y="{y - 2}" class="axis-title">{html.escape(value_suffix)}</text>')
    return parts


def write_svg(rows: list[dict[str, str | int]], output: Path) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    width = 1500
    height = 920
    margin_left = 110
    plot_width = width - margin_left - 70
    panel_height = 300
    top_y = 160
    bottom_y = 560
    generated_at = datetime.now().strftime("%Y/%m/%d %H:%M")
    first_date = rows[0]["committed_date"]
    last_date = rows[-1]["committed_date"]

    total_columns = {
        "docs_spec": "docs_spec_total_lines",
        "src": "src_total_lines",
        "tests": "tests_total_lines",
    }
    changed_columns = {
        "docs_spec": "docs_spec_changed_lines",
        "src": "src_changed_lines",
        "tests": "tests_changed_lines",
    }

    svg: list[str] = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">',
        "<style>",
        "text { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; fill: #111827; }",
        ".title { font-size: 28px; font-weight: 700; }",
        ".subtitle { font-size: 14px; fill: #4b5563; }",
        ".panel-title { font-size: 18px; font-weight: 700; }",
        ".axis-label { font-size: 11px; fill: #4b5563; }",
        ".axis-title { font-size: 12px; fill: #4b5563; }",
        ".plot-bg { fill: #ffffff; stroke: #d1d5db; }",
        ".grid { stroke: #e5e7eb; stroke-width: 1; }",
        ".axis { stroke: #6b7280; stroke-width: 1; }",
        ".series-line { stroke-width: 2.4; stroke-linejoin: round; stroke-linecap: round; }",
        ".ai-line { stroke: #6b7280; stroke-width: 1.5; stroke-dasharray: 6 6; }",
        ".ai-label { font-size: 12px; fill: #374151; }",
        ".legend-label { font-size: 14px; }",
        "</style>",
        '<rect width="100%" height="100%" fill="#f9fafb"/>',
        '<text x="40" y="48" class="title">Commit line history</text>',
        (
            f'<text x="40" y="75" class="subtitle">Generated {generated_at}; '
            f'{len(rows)} commits from {first_date} to {last_date}; docs + spec, src, tests; '
            'changed lines = added + deleted.</text>'
        ),
        (
            '<text x="40" y="98" class="subtitle">Path scope: docs/ + spec/ or specs/, src/, tests/. '
            'Historical files outside these paths are excluded.</text>'
        ),
    ]

    legend_x = 1080
    for offset, name in enumerate(("docs_spec", "src", "tests")):
        y = 45 + offset * 24
        svg.append(
            f'<line x1="{legend_x}" y1="{y}" x2="{legend_x + 36}" y2="{y}" '
            f'stroke="{CATEGORIES[name]["color"]}" class="series-line"/>'
        )
        svg.append(
            f'<text x="{legend_x + 46}" y="{y + 5}" class="legend-label">{html.escape(CATEGORIES[name]["label"])}</text>'
        )

    svg.extend(
        render_panel(
            rows,
            x=margin_left,
            y=top_y,
            width=plot_width,
            height=panel_height,
            title="Total lines after each commit",
            value_suffix="total lines",
            series_columns=total_columns,
        )
    )
    svg.extend(
        render_panel(
            rows,
            x=margin_left,
            y=bottom_y,
            width=plot_width,
            height=panel_height,
            title="Changed lines in each commit",
            value_suffix="changed lines",
            series_columns=changed_columns,
        )
    )

    svg.append("</svg>")
    output.write_text("\n".join(svg), encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Export per-commit line history CSV and SVG chart.")
    parser.add_argument("--since", help="Filter rows to commits on or after YYYY-MM-DD before chart/export.")
    parser.add_argument("--skip-csv", action="store_true", help="Only write the SVG chart.")
    parser.add_argument("--csv-output", type=Path, default=DEFAULT_CSV_OUTPUT)
    parser.add_argument("--svg-output", type=Path, default=DEFAULT_SVG_OUTPUT)
    return parser.parse_args()


def output_path(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def display_path(path: Path) -> str:
    return str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else str(path)


def main() -> None:
    args = parse_args()
    csv_output = output_path(args.csv_output)
    svg_output = output_path(args.svg_output)
    rows = build_rows()
    if args.since:
        since_date = date.fromisoformat(args.since)
        rows = [row for row in rows if date.fromisoformat(str(row["committed_date"])) >= since_date]
    if not rows:
        raise RuntimeError("No commits matched the requested filter.")
    if not args.skip_csv:
        write_csv(rows, csv_output)
    write_svg(rows, svg_output)
    if not args.skip_csv:
        print(f"Wrote {display_path(csv_output)}")
    print(f"Wrote {display_path(svg_output)}")


if __name__ == "__main__":
    main()
