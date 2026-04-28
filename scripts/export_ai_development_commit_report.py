#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import subprocess
from collections import Counter, defaultdict
from dataclasses import dataclass, field
from datetime import date, datetime
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_AI_START = date(2026, 3, 1)
DEFAULT_CSV_OUTPUT = ROOT / "docs" / "metrics" / "ai-development-commit-report.csv"
DEFAULT_MARKDOWN_OUTPUT = ROOT / "docs" / "metrics" / "ai-development-commit-report.md"
TRACKED_DIRS = ("docs", "src", "tests", "spec")
COMMIT_SECTION_MARKERS = (" milestones:", " changes:", " fixes:", " comments:")


@dataclass
class DirChange:
    paths: set[str] = field(default_factory=set)
    lines: int = 0

    @property
    def files(self) -> int:
        return len(self.paths)

    def add(self, path: str, lines: int) -> None:
        self.paths.add(path)
        self.lines += lines

    def merge(self, other: "DirChange") -> None:
        self.paths.update(other.paths)
        self.lines += other.lines


@dataclass
class ChangeMetric:
    files: int = 0
    lines: int = 0

    def add(self, files: int, lines: int) -> None:
        self.files += files
        self.lines += lines


@dataclass
class CommitRow:
    commit_index: int
    committed_at: str
    date_text: str
    sha: str
    short_sha: str
    parent_count: int
    subject: str
    total: DirChange
    dirs: dict[str, DirChange]
    other: DirChange
    granularity: str


def run_git(*args: str, text: bool = True) -> str | bytes:
    result = subprocess.run(
        ["git", *args],
        cwd=ROOT,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if result.returncode != 0:
        raise RuntimeError(
            f"git {' '.join(args)} failed with exit code {result.returncode}: "
            f"{result.stderr.decode('utf-8', errors='replace')}"
        )
    if text:
        return result.stdout.decode("utf-8", errors="replace")
    return result.stdout


def commit_metadata() -> list[dict[str, str]]:
    raw = run_git(
        "log",
        "--reverse",
        "--date=iso-strict",
        "--pretty=format:%H%x00%h%x00%cI%x00%P%x00%s",
    )
    commits = []
    for line in raw.splitlines():
        if not line:
            continue
        sha, short_sha, committed_at, parents, subject = line.split("\x00", 4)
        commits.append(
            {
                "sha": sha,
                "short_sha": short_sha,
                "committed_at": committed_at,
                "parents": parents,
                "subject": subject,
            }
        )
    return commits


def numstat_bytes(commit: dict[str, str]) -> bytes:
    parents = [parent for parent in commit["parents"].split() if parent]
    if parents:
        return run_git(
            "diff",
            "--numstat",
            "--find-renames",
            "-z",
            parents[0],
            commit["sha"],
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
        commit["sha"],
        "--",
        text=False,
    )


def int_lines(value: bytes) -> int:
    text = value.decode("utf-8", errors="replace")
    if text == "-":
        return 0
    return int(text)


def parse_numstat(commit: dict[str, str]) -> list[tuple[str, int]]:
    parts = numstat_bytes(commit).split(b"\x00")
    changes: list[tuple[str, int]] = []
    index = 0
    while index < len(parts):
        token = parts[index]
        index += 1
        if not token:
            continue

        fields = token.split(b"\t")
        if len(fields) < 3:
            continue

        added = int_lines(fields[0])
        deleted = int_lines(fields[1])
        path_bytes = fields[2]
        if path_bytes:
            path = path_bytes.decode("utf-8", errors="replace")
        else:
            # With -z, rename records are: "added<TAB>deleted<TAB>\0old\0new\0".
            if index + 1 >= len(parts):
                break
            index += 1
            path = parts[index].decode("utf-8", errors="replace")
            index += 1

        changes.append((path, added + deleted))
    return changes


def top_level_dir(path: str) -> str:
    first = path.split("/", 1)[0]
    return first if first in TRACKED_DIRS else "other"


def classify_granularity(changed_files: int, changed_lines: int, dirs: dict[str, DirChange]) -> str:
    touched_dirs = sum(1 for name in TRACKED_DIRS if dirs[name].files > 0)
    if touched_dirs >= 3 and changed_files > 8:
        return "混合"
    if changed_files > 12 or changed_lines > 400:
        return "大"
    if changed_files > 3 or changed_lines > 80:
        return "中"
    return "小"


def display_title(subject: str) -> str:
    title = subject
    for marker in COMMIT_SECTION_MARKERS:
        marker_index = title.find(marker)
        if marker_index >= 0:
            title = title[:marker_index]
    return title.strip()


def build_rows() -> list[CommitRow]:
    rows: list[CommitRow] = []
    for commit_index, commit in enumerate(commit_metadata(), start=1):
        dirs = {name: DirChange() for name in TRACKED_DIRS}
        other = DirChange()
        total = DirChange()

        for path, lines in parse_numstat(commit):
            total.add(path, lines)
            bucket = top_level_dir(path)
            if bucket == "other":
                other.add(path, lines)
            else:
                dirs[bucket].add(path, lines)

        granularity = classify_granularity(total.files, total.lines, dirs)
        rows.append(
            CommitRow(
                commit_index=commit_index,
                committed_at=commit["committed_at"],
                date_text=commit["committed_at"][:10],
                sha=commit["sha"],
                short_sha=commit["short_sha"],
                parent_count=len([parent for parent in commit["parents"].split() if parent]),
                subject=commit["subject"],
                total=total,
                dirs=dirs,
                other=other,
                granularity=granularity,
            )
        )
    return rows


def change_text(change: DirChange) -> str:
    return f"{change.files}/{change.lines}"


def row_to_csv(row: CommitRow) -> dict[str, str | int]:
    data: dict[str, str | int] = {
        "commit_index": row.commit_index,
        "date": row.date_text,
        "committed_at": row.committed_at,
        "sha": row.sha,
        "short_sha": row.short_sha,
        "parent_count": row.parent_count,
        "subject": row.subject,
        "title": display_title(row.subject),
        "changed_files": row.total.files,
        "lines_changed": row.total.lines,
        "granularity": row.granularity,
    }
    for name in TRACKED_DIRS:
        data[f"{name}_files"] = row.dirs[name].files
        data[f"{name}_lines"] = row.dirs[name].lines
    data["other_files"] = row.other.files
    data["other_lines"] = row.other.lines
    return data


def write_csv(rows: list[CommitRow], output: Path) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    csv_rows = [row_to_csv(row) for row in rows]
    with output.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(csv_rows[0].keys()))
        writer.writeheader()
        writer.writerows(csv_rows)


def daily_summary(rows: list[CommitRow]) -> list[dict[str, object]]:
    by_date: dict[str, list[CommitRow]] = defaultdict(list)
    for row in rows:
        by_date[row.date_text].append(row)

    summaries = []
    for date_text in sorted(by_date):
        date_rows = by_date[date_text]
        dirs = {name: ChangeMetric() for name in TRACKED_DIRS}
        total = ChangeMetric()
        granularity = Counter(row.granularity for row in date_rows)
        for row in date_rows:
            total.add(row.total.files, row.total.lines)
            for name in TRACKED_DIRS:
                dirs[name].add(row.dirs[name].files, row.dirs[name].lines)
        summaries.append(
            {
                "date": date_text,
                "rows": date_rows,
                "commits": len(date_rows),
                "total": total,
                "dirs": dirs,
                "granularity": granularity,
            }
        )
    return summaries


def period_summary(rows: list[CommitRow], ai_start: date) -> list[dict[str, object]]:
    periods = [
        ("AI 前", [row for row in rows if datetime.fromisoformat(row.committed_at).date() < ai_start]),
        ("AI 後", [row for row in rows if datetime.fromisoformat(row.committed_at).date() >= ai_start]),
    ]
    summaries = []
    for label, period_rows in periods:
        total = ChangeMetric()
        granularity = Counter(row.granularity for row in period_rows)
        active_days = len({row.date_text for row in period_rows})
        if period_rows:
            first_date = datetime.fromisoformat(period_rows[0].committed_at).date()
            last_date = datetime.fromisoformat(period_rows[-1].committed_at).date()
            calendar_days = (last_date - first_date).days + 1
            range_text = f"{first_date:%Y/%m/%d} - {last_date:%Y/%m/%d}"
        else:
            calendar_days = 0
            range_text = "-"
        commits = len(period_rows)
        for row in period_rows:
            total.add(row.total.files, row.total.lines)
        summaries.append(
            {
                "label": label,
                "range": range_text,
                "commits": commits,
                "active_days": active_days,
                "calendar_days": calendar_days,
                "commits_per_active_day": commits / active_days if active_days else 0,
                "commits_per_calendar_day": commits / calendar_days if calendar_days else 0,
                "total": total,
                "avg_files_per_commit": total.files / commits if commits else 0,
                "avg_lines_per_commit": total.lines / commits if commits else 0,
                "granularity": granularity,
            }
        )
    return summaries


def granularity_text(counter: Counter[str]) -> str:
    return f"小 {counter['小']} / 中 {counter['中']} / 大 {counter['大']} / 混合 {counter['混合']}"


def format_float(value: float) -> str:
    return f"{value:.2f}"


def write_markdown(rows: list[CommitRow], output: Path, csv_output: Path, ai_start: date) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    generated_at = datetime.now().strftime("%Y/%m/%d %H:%M")
    daily_rows = daily_summary(rows)
    periods = period_summary(rows, ai_start)

    lines = [
        "# AI 開發前後 Commit 生產速度與顆粒度分析",
        "",
        f"- 產生時間: {generated_at}",
        f"- Commit 範圍: `{rows[0].short_sha}` - `{rows[-1].short_sha}`",
        f"- Commit 數: {len(rows)}",
        f"- AI 開發切分日: {ai_start:%Y/%m/%d}",
        f"- Raw CSV: `{csv_output.relative_to(ROOT)}`",
        "- 統計口徑: `lines change = added + deleted`；binary 檔案行數以 0 計。",
        "- 表格中的 `docs` / `src` / `tests` / `spec` 欄位格式為 `異動檔案數/異動行數`。",
        "- 日期與區間彙總的檔案數為 commit file change 加總；同一檔案同日多次提交會重複計入。",
        "- Commit 清單的 title 取自 git subject；若 subject 同行包含 `milestones` / `changes` / `fixes` / `comments` 區段，清單只顯示區段前的目標文字。",
        "",
        "## AI 前後速度摘要",
        "",
        "| 區間 | 日期範圍 | commits | 有 commit 天數 | calendar days | commits/有 commit 日 | commits/calendar day | total | avg files/commit | avg lines/commit | 顆粒度分布 |",
        "|---|---|---:|---:|---:|---:|---:|---|---:|---:|---|",
    ]

    for period in periods:
        lines.append(
            "| {label} | {range} | {commits} | {active_days} | {calendar_days} | {commits_per_active_day} | {commits_per_calendar_day} | {total} | {avg_files_per_commit} | {avg_lines_per_commit} | {granularity} |".format(
                label=period["label"],
                range=period["range"],
                commits=period["commits"],
                active_days=period["active_days"],
                calendar_days=period["calendar_days"],
                commits_per_active_day=format_float(period["commits_per_active_day"]),
                commits_per_calendar_day=format_float(period["commits_per_calendar_day"]),
                total=change_text(period["total"]),
                avg_files_per_commit=format_float(period["avg_files_per_commit"]),
                avg_lines_per_commit=format_float(period["avg_lines_per_commit"]),
                granularity=granularity_text(period["granularity"]),
            )
        )

    lines.extend(
        [
            "",
            "## 日期統計表",
            "",
            "| 日期 | commits | total | docs | src | tests | spec | 顆粒度分布 |",
            "|---|---:|---|---|---|---|---|---|",
        ]
    )

    for summary in daily_rows:
        dirs = summary["dirs"]
        lines.append(
            "| {date} | {commits} | {total} | {docs} | {src} | {tests} | {spec} | {granularity} |".format(
                date=summary["date"].replace("-", "/"),
                commits=summary["commits"],
                total=change_text(summary["total"]),
                docs=change_text(dirs["docs"]),
                src=change_text(dirs["src"]),
                tests=change_text(dirs["tests"]),
                spec=change_text(dirs["spec"]),
                granularity=granularity_text(summary["granularity"]),
            )
        )

    lines.extend(["", "## 日期清單", ""])

    for summary in daily_rows:
        dirs = summary["dirs"]
        lines.extend(
            [
                f"### {summary['date'].replace('-', '/')}",
                "",
                "> 摘要: "
                f"commits {summary['commits']}, "
                f"total {change_text(summary['total'])}, "
                f"docs {change_text(dirs['docs'])}, "
                f"src {change_text(dirs['src'])}, "
                f"tests {change_text(dirs['tests'])}, "
                f"spec {change_text(dirs['spec'])}",
                "",
            ]
        )
        for row in summary["rows"]:
            lines.append(
                f"- `{row.short_sha}` - {display_title(row.subject)} "
                f"(total {change_text(row.total)}, docs {change_text(row.dirs['docs'])}, "
                f"src {change_text(row.dirs['src'])}, tests {change_text(row.dirs['tests'])}, "
                f"spec {change_text(row.dirs['spec'])}, 顆粒度 {row.granularity})"
            )
        lines.append("")

    output.write_text("\n".join(lines).rstrip() + "\n", encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Export AI development commit report.")
    parser.add_argument("--ai-start", default=DEFAULT_AI_START.isoformat())
    parser.add_argument("--csv-output", type=Path, default=DEFAULT_CSV_OUTPUT)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_OUTPUT)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    ai_start = date.fromisoformat(args.ai_start)
    rows = build_rows()
    write_csv(rows, args.csv_output)
    write_markdown(rows, args.markdown_output, args.csv_output, ai_start)
    print(f"Wrote {args.csv_output.relative_to(ROOT)}")
    print(f"Wrote {args.markdown_output.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
