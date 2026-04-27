#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import re
import subprocess
from collections import Counter, defaultdict
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT = ROOT / "docs" / "metrics" / "git-commit-metrics.csv"

STATUS_COLUMNS = ("added", "deleted", "modified", "renamed", "copied", "type_changed", "other")
CORE_ABSTRACT_SOURCE_PREFIXES = (
    "src/AndrewDemo.NetConf2023.Abstract/",
    "src/AndrewDemo.NetConf2023.Core/",
)
MAJOR_CORE_DECISION_MARKERS = ("重大決策", "影響 .Core")
BACKTRACKING_DECISION_MARKERS = ("回頭修正 Phase 1", "影響 .Abstract / spec")

PUBLIC_INTERFACE_RE = re.compile(r"^\s*public\s+(?:\w+\s+)*interface\s+([A-Za-z_][A-Za-z0-9_]*)\b")
PUBLIC_TYPE_RE = re.compile(
    r"^\s*public\s+(?:\w+\s+)*(class|record|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)\b"
)
PUBLIC_METHOD_RE = re.compile(
    r"^\s*public\s+(?!class\b|record\b|struct\b|enum\b|interface\b)"
    r"(?:[A-Za-z_][A-Za-z0-9_<>,\[\].?]*\s+)+"
    r"([A-Za-z_][A-Za-z0-9_]*)\s*\("
)
TEST_CASE_RE = re.compile(r"\[(?:Fact|Theory)\b")
CLASS_RE = re.compile(r"\b(?:public\s+|internal\s+)?(?:partial\s+)?class\s+([A-Za-z_][A-Za-z0-9_]*)\b")


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


def is_doc_path(path: str) -> bool:
    return path.startswith(("docs/", "spec/"))


def is_decision_path(path: str) -> bool:
    return path.startswith("docs/decisions/")


def is_source_code_path(path: str) -> bool:
    return path.startswith("src/") and Path(path).suffix.lower() == ".cs"


def is_test_code_path(path: str) -> bool:
    return path.startswith("tests/") and Path(path).suffix.lower() == ".cs"


def is_core_abstract_source_code_path(path: str) -> bool:
    return path.startswith(CORE_ABSTRACT_SOURCE_PREFIXES) and Path(path).suffix.lower() == ".cs"


def classify_path(path: str) -> str:
    if is_doc_path(path):
        return "doc"
    if is_source_code_path(path) or is_test_code_path(path):
        return "code"
    return "other"


def is_decision_file(path: str) -> bool:
    return is_decision_path(path) and Path(path).name.lower() != "readme.md"


def status_bucket(status: str) -> str:
    if status == "A":
        return "added"
    if status == "D":
        return "deleted"
    if status == "M":
        return "modified"
    if status.startswith("R"):
        return "renamed"
    if status.startswith("C"):
        return "copied"
    if status == "T":
        return "type_changed"
    return "other"


def tag_map() -> dict[str, list[str]]:
    mapping: dict[str, list[str]] = defaultdict(list)
    tags = [line for line in run_git("tag", "--list").splitlines() if line]
    for tag in tags:
        commit = run_git("rev-list", "-n", "1", tag).strip()
        if commit:
            mapping[commit].append(tag)
    for values in mapping.values():
        values.sort()
    return mapping


def commit_list() -> list[str]:
    return [line for line in run_git("rev-list", "--reverse", "HEAD").splitlines() if line]


def commit_metadata(commit: str) -> dict[str, str]:
    raw = run_git(
        "show",
        "-s",
        "--format=%H%x00%h%x00%cI%x00%aI%x00%an%x00%ae%x00%P%x00%s",
        commit,
    )
    full_sha, short_sha, committed_at, authored_at, author_name, author_email, parents, subject = raw.rstrip(
        "\n"
    ).split("\x00", 7)
    return {
        "sha": full_sha,
        "short_sha": short_sha,
        "committed_at": committed_at,
        "authored_at": authored_at,
        "author_name": author_name,
        "author_email": author_email,
        "parent_count": str(len([p for p in parents.split() if p])),
        "subject": subject,
    }


def line_counts(commit: str) -> dict[str, int]:
    output = run_git("grep", "-I", "-c", "^", commit, check=False)
    counts: dict[str, int] = {}
    prefix = f"{commit}:"
    for line in output.splitlines():
        if not line.startswith(prefix):
            continue
        rest = line[len(prefix) :]
        if ":" not in rest:
            continue
        path, count_text = rest.rsplit(":", 1)
        try:
            counts[path] = int(count_text)
        except ValueError:
            continue
    return counts


def tree_files(commit: str) -> list[str]:
    raw = run_git("ls-tree", "-r", "--name-only", "-z", commit, text=False)
    return [part.decode("utf-8", errors="replace") for part in raw.split(b"\x00") if part]


def major_decision_paths(commit: str) -> set[str]:
    return decision_paths_with_any_marker(commit, ("重大決策",))


def decision_paths_with_any_marker(commit: str, markers: tuple[str, ...]) -> set[str]:
    grep_args = ["grep", "-I", "-l"]
    for marker in markers:
        grep_args.extend(["-e", marker])
    output = run_git(*grep_args, commit, "--", "docs/decisions", check=False)
    prefix = f"{commit}:"
    paths = set()
    for line in output.splitlines():
        if not line.startswith(prefix):
            continue
        path = line[len(prefix) :]
        if is_decision_file(path):
            paths.add(path)
    return paths


def tree_lines(commit: str, pathspec: str) -> dict[str, list[str]]:
    output = run_git("grep", "-I", "-n", "^", commit, "--", pathspec, check=False)
    prefix = f"{commit}:"
    files: dict[str, list[str]] = defaultdict(list)
    for line in output.splitlines():
        if not line.startswith(prefix):
            continue
        rest = line[len(prefix) :]
        parts = rest.split(":", 2)
        if len(parts) != 3:
            continue
        path, _, text = parts
        files[path].append(text)
    return dict(files)


def interface_method_signatures(path: str, lines: list[str]) -> set[str]:
    signatures: set[str] = set()
    inside_interface = False
    current_interface = ""
    brace_depth = 0
    saw_open_brace = False

    for raw_line in lines:
        line = raw_line.strip()
        if not inside_interface:
            match = PUBLIC_INTERFACE_RE.match(raw_line)
            if not match:
                continue
            inside_interface = True
            current_interface = match.group(1)
            brace_depth = 0
            saw_open_brace = False

        if inside_interface:
            if "(" in line and ")" in line and line.endswith(";") and not line.startswith(("[", "//")):
                normalized = " ".join(line.split())
                signatures.add(f"interface-method:{current_interface}:{normalized}")

            brace_delta = raw_line.count("{") - raw_line.count("}")
            if raw_line.count("{"):
                saw_open_brace = True
            brace_depth += brace_delta
            if saw_open_brace and brace_depth <= 0:
                inside_interface = False
                current_interface = ""

    return signatures


def design_snapshot_metrics(commit: str) -> tuple[dict[str, int], set[str]]:
    src_files = tree_lines(commit, ":(glob)src/**/*.cs")
    test_files = tree_lines(commit, ":(glob)tests/**/*.cs")

    public_interfaces: set[str] = set()
    public_types: set[str] = set()
    public_methods: set[str] = set()
    contract_signatures: set[str] = set()
    test_fixtures: set[str] = set()
    test_cases = 0

    for path, lines in src_files.items():
        for raw_line in lines:
            normalized = " ".join(raw_line.strip().split())
            interface_match = PUBLIC_INTERFACE_RE.match(raw_line)
            if interface_match:
                signature = f"interface:{interface_match.group(1)}"
                public_interfaces.add(signature)
                contract_signatures.add(signature)
                continue

            type_match = PUBLIC_TYPE_RE.match(raw_line)
            if type_match:
                signature = f"type:{type_match.group(1)}:{type_match.group(2)}"
                public_types.add(signature)
                contract_signatures.add(signature)
                continue

            method_match = PUBLIC_METHOD_RE.match(raw_line)
            if method_match:
                signature = f"public-method:{normalized}"
                public_methods.add(signature)
                contract_signatures.add(signature)

        interface_methods = interface_method_signatures(path, lines)
        public_methods.update(interface_methods)
        contract_signatures.update(interface_methods)

    for path, lines in test_files.items():
        file_test_cases = sum(1 for line in lines if TEST_CASE_RE.search(line))
        test_cases += file_test_cases
        if file_test_cases <= 0:
            continue
        class_names = [match.group(1) for line in lines for match in [CLASS_RE.search(line)] if match]
        if class_names:
            test_fixtures.update(f"{path}:{class_name}" for class_name in class_names)
        else:
            test_fixtures.add(path)

    metrics = Counter()
    metrics["snapshot_public_interfaces"] = len(public_interfaces)
    metrics["snapshot_public_methods"] = len(public_methods)
    metrics["snapshot_public_types"] = len(public_types)
    metrics["snapshot_public_contract_surface"] = (
        metrics["snapshot_public_interfaces"]
        + metrics["snapshot_public_methods"]
        + metrics["snapshot_public_types"]
    )
    metrics["snapshot_test_fixtures"] = len(test_fixtures)
    metrics["snapshot_test_cases"] = test_cases
    return dict(metrics), contract_signatures


def snapshot_metrics(commit: str) -> tuple[dict[str, int], dict[str, set[str]], set[str]]:
    files = tree_files(commit)
    counts = line_counts(commit)
    major_paths = major_decision_paths(commit)
    major_core_paths = decision_paths_with_any_marker(commit, MAJOR_CORE_DECISION_MARKERS)
    backtracking_paths = decision_paths_with_any_marker(commit, BACKTRACKING_DECISION_MARKERS)
    design_metrics, contract_signatures = design_snapshot_metrics(commit)

    metrics = Counter()
    metrics["snapshot_tracked_files"] = len(files)
    metrics["snapshot_text_files"] = len(counts)
    metrics["snapshot_total_text_lines"] = sum(counts.values())

    for path in files:
        category = classify_path(path)
        metrics[f"snapshot_{category}_files"] += 1
        metrics[f"snapshot_{category}_lines"] += counts.get(path, 0)

        if is_decision_file(path):
            metrics["snapshot_decision_files"] += 1
            metrics["snapshot_decision_lines"] += counts.get(path, 0)
        if path in major_paths:
            metrics["snapshot_major_decision_files"] += 1
            metrics["snapshot_major_decision_lines"] += counts.get(path, 0)
        if path in major_core_paths:
            metrics["snapshot_major_core_decision_files"] += 1
            metrics["snapshot_major_core_decision_lines"] += counts.get(path, 0)
        if path in backtracking_paths:
            metrics["snapshot_backtracking_decision_files"] += 1
            metrics["snapshot_backtracking_decision_lines"] += counts.get(path, 0)
        if is_source_code_path(path):
            metrics["snapshot_src_files"] += 1
            metrics["snapshot_src_lines"] += counts.get(path, 0)
        if is_core_abstract_source_code_path(path):
            metrics["snapshot_core_abstract_src_files"] += 1
            metrics["snapshot_core_abstract_src_lines"] += counts.get(path, 0)
        if is_test_code_path(path):
            metrics["snapshot_test_files"] += 1
            metrics["snapshot_test_lines"] += counts.get(path, 0)
        if path.endswith(".cs"):
            metrics["snapshot_csharp_files"] += 1
            metrics["snapshot_csharp_lines"] += counts.get(path, 0)

    for category in ("doc", "code", "other"):
        metrics.setdefault(f"snapshot_{category}_files", 0)
        metrics.setdefault(f"snapshot_{category}_lines", 0)

    metrics.update(design_metrics)

    marker_paths = {
        "major": major_paths,
        "major_core": major_core_paths,
        "backtracking": backtracking_paths,
    }
    return dict(metrics), marker_paths, contract_signatures


def parse_numstat_line(line: str) -> tuple[int, int]:
    parts = line.split("\t")
    if len(parts) < 2:
        return 0, 0
    added = 0 if parts[0] == "-" else int(parts[0])
    deleted = 0 if parts[1] == "-" else int(parts[1])
    return added, deleted


def diff_records(commit: str) -> list[dict[str, str | int]]:
    status_lines = [
        line
        for line in run_git(
            "diff-tree", "--root", "--no-commit-id", "-r", "-M", "--name-status", commit
        ).splitlines()
        if line
    ]
    numstat_lines = [
        line
        for line in run_git(
            "diff-tree", "--root", "--no-commit-id", "-r", "-M", "--numstat", commit
        ).splitlines()
        if line
    ]

    records = []
    for index, status_line in enumerate(status_lines):
        parts = status_line.split("\t")
        status = parts[0]
        old_path = ""
        path = parts[-1] if len(parts) > 1 else ""
        if status.startswith(("R", "C")) and len(parts) >= 3:
            old_path = parts[1]
            path = parts[2]
        added, deleted = parse_numstat_line(numstat_lines[index]) if index < len(numstat_lines) else (0, 0)
        records.append(
            {
                "status": status,
                "status_bucket": status_bucket(status),
                "path": path,
                "old_path": old_path,
                "added_lines": added,
                "deleted_lines": deleted,
            }
        )
    return records


def empty_change_metrics() -> Counter:
    metrics = Counter()
    prefixes = (
        "",
        "docs_",
        "code_",
        "src_",
        "core_abstract_",
        "test_",
        "other_",
        "decision_",
        "major_decision_",
        "major_core_decision_",
        "backtracking_decision_",
    )
    for prefix in prefixes:
        metrics[f"{prefix}changed_files"] = 0
        metrics[f"{prefix}lines_added"] = 0
        metrics[f"{prefix}lines_deleted"] = 0
        metrics[f"{prefix}lines_changed"] = 0
        for status in STATUS_COLUMNS:
            metrics[f"{prefix}{status}_files"] = 0
    return metrics


def add_change(metrics: Counter, prefix: str, record: dict[str, str | int]) -> None:
    added = int(record["added_lines"])
    deleted = int(record["deleted_lines"])
    bucket = str(record["status_bucket"])
    metrics[f"{prefix}changed_files"] += 1
    metrics[f"{prefix}{bucket}_files"] += 1
    metrics[f"{prefix}lines_added"] += added
    metrics[f"{prefix}lines_deleted"] += deleted
    metrics[f"{prefix}lines_changed"] += added + deleted


def change_metrics(
    records: list[dict[str, str | int]],
    current_marker_paths: dict[str, set[str]],
    previous_marker_paths: dict[str, set[str]],
) -> dict[str, int]:
    metrics = empty_change_metrics()
    major_paths = current_marker_paths["major"] | previous_marker_paths["major"]
    major_core_paths = current_marker_paths["major_core"] | previous_marker_paths["major_core"]
    backtracking_paths = current_marker_paths["backtracking"] | previous_marker_paths["backtracking"]

    for record in records:
        path = str(record["path"])
        category = classify_path(path)
        add_change(metrics, "", record)
        add_change(metrics, f"{category}s_" if category == "doc" else f"{category}_", record)
        if is_source_code_path(path):
            add_change(metrics, "src_", record)
        if is_core_abstract_source_code_path(path):
            add_change(metrics, "core_abstract_", record)
        if is_test_code_path(path):
            add_change(metrics, "test_", record)

        if is_decision_file(path):
            add_change(metrics, "decision_", record)
        if path in major_paths:
            add_change(metrics, "major_decision_", record)
        if path in major_core_paths:
            add_change(metrics, "major_core_decision_", record)
        if path in backtracking_paths:
            add_change(metrics, "backtracking_decision_", record)

    return dict(metrics)


def design_delta_metrics(
    current_snapshot: dict[str, int],
    previous_snapshot: dict[str, int],
    current_contract_signatures: set[str],
    previous_contract_signatures: set[str],
) -> dict[str, int]:
    metrics = Counter()
    for key in (
        "public_interfaces",
        "public_methods",
        "public_types",
        "public_contract_surface",
        "test_fixtures",
        "test_cases",
    ):
        metrics[f"delta_{key}"] = current_snapshot.get(f"snapshot_{key}", 0) - previous_snapshot.get(
            f"snapshot_{key}", 0
        )

    metrics["contract_churn"] = len(current_contract_signatures - previous_contract_signatures) + len(
        previous_contract_signatures - current_contract_signatures
    )
    return dict(metrics)


def build_rows() -> list[dict[str, str | int]]:
    tags_by_commit = tag_map()
    commits = commit_list()
    rows: list[dict[str, str | int]] = []
    previous_marker_paths: dict[str, set[str]] = {
        "major": set(),
        "major_core": set(),
        "backtracking": set(),
    }
    previous_contract_signatures: set[str] = set()
    previous_snapshot: dict[str, int] = {}

    for sequence, commit in enumerate(commits, start=1):
        metadata = commit_metadata(commit)
        snapshot, current_marker_paths, current_contract_signatures = snapshot_metrics(commit)
        changes = change_metrics(diff_records(commit), current_marker_paths, previous_marker_paths)
        design_changes = design_delta_metrics(
            snapshot,
            previous_snapshot,
            current_contract_signatures,
            previous_contract_signatures,
        )
        tags = tags_by_commit.get(commit, [])
        row: dict[str, str | int] = {
            "commit_index": sequence,
            **metadata,
            "tags": ";".join(tags),
            "roadmap_tags": ";".join(tag for tag in tags if tag.startswith("roadmap/")),
            "decision_tags": ";".join(tag for tag in tags if tag.startswith("decision/")),
            **changes,
            **design_changes,
            **snapshot,
        }
        rows.append(row)
        previous_marker_paths = current_marker_paths
        previous_contract_signatures = current_contract_signatures
        previous_snapshot = snapshot
    return rows


def fieldnames() -> list[str]:
    metadata = [
        "commit_index",
        "committed_at",
        "authored_at",
        "sha",
        "short_sha",
        "parent_count",
        "author_name",
        "author_email",
        "subject",
        "tags",
        "roadmap_tags",
        "decision_tags",
    ]
    change_prefixes = [
        "",
        "docs_",
        "code_",
        "src_",
        "core_abstract_",
        "test_",
        "other_",
        "decision_",
        "major_decision_",
        "major_core_decision_",
        "backtracking_decision_",
    ]
    change_fields: list[str] = []
    for prefix in change_prefixes:
        change_fields.extend(
            [
                f"{prefix}changed_files",
                f"{prefix}added_files",
                f"{prefix}deleted_files",
                f"{prefix}modified_files",
                f"{prefix}renamed_files",
                f"{prefix}copied_files",
                f"{prefix}type_changed_files",
                f"{prefix}other_files",
                f"{prefix}lines_added",
                f"{prefix}lines_deleted",
                f"{prefix}lines_changed",
            ]
        )
    design_change = [
        "delta_public_interfaces",
        "delta_public_methods",
        "delta_public_types",
        "delta_public_contract_surface",
        "delta_test_fixtures",
        "delta_test_cases",
        "contract_churn",
    ]
    snapshot = [
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
        "snapshot_major_core_decision_files",
        "snapshot_major_core_decision_lines",
        "snapshot_backtracking_decision_files",
        "snapshot_backtracking_decision_lines",
        "snapshot_src_files",
        "snapshot_src_lines",
        "snapshot_core_abstract_src_files",
        "snapshot_core_abstract_src_lines",
        "snapshot_test_files",
        "snapshot_test_lines",
        "snapshot_csharp_files",
        "snapshot_csharp_lines",
        "snapshot_public_interfaces",
        "snapshot_public_methods",
        "snapshot_public_types",
        "snapshot_public_contract_surface",
        "snapshot_test_fixtures",
        "snapshot_test_cases",
    ]
    return metadata + change_fields + design_change + snapshot


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    args = parser.parse_args()

    rows = build_rows()
    output = args.output if args.output.is_absolute() else ROOT / args.output
    output.parent.mkdir(parents=True, exist_ok=True)

    with output.open("w", newline="", encoding="utf-8-sig") as handle:
        fields = fieldnames()
        writer = csv.DictWriter(handle, fieldnames=fields, extrasaction="ignore")
        writer.writeheader()
        writer.writerows({field: row.get(field, 0) for field in fields} for row in rows)

    print(f"wrote {len(rows)} rows to {output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
