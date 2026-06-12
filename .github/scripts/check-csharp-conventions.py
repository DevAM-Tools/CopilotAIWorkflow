#!/usr/bin/env python3
"""Heuristic C# review helper. Plain-text output: one problem per line. Silent when clean.

Line-oriented checks (no full parser — expect some false pos/neg):
  line-length, var, private _PascalCase naming, optional Coverlet config.

Usage:
  python .github/scripts/check-csharp-conventions.py [path ...]
  python .github/scripts/check-csharp-conventions.py --coverage [path ...]
  python .github/scripts/check-csharp-conventions.py --run-coverage Proj.Tests.csproj
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path
from typing import Iterator

MAX_LINE = 160
EXT = {".cs"}
SKIP = {".git", ".vs", "bin", "obj", "node_modules", "packages", "TestResults", "artifacts"}
PRIVATE_ACCESS = r"(?:protected|{mods})".format(
    mods=r"|".join(("static", "readonly", "volatile", "const", "required", "unsafe", "async", "new", "partial"))
)
TYPE = r"[\w<>\[\],\.\?\s]+"

THRESHOLD_BRANCH = re.compile(
    r"(?:thresholdtype|threshold-type)\s*[=>\s\"']*\s*branch\b", re.IGNORECASE
)
THRESHOLD_100 = re.compile(
    r"(?:threshold|coverletoutputthreshold)\s*[=>\s\"']*\s*100\b", re.IGNORECASE
)


def emit(file: str, line: int, rule: str, detail: str = "", col: int | None = None) -> str:
    loc = f"{file}:{line}" if line else file
    if col:
        loc += f":{col}"
    return f"{loc} {rule}" + (f" {detail}" if detail else "")


def mask_line(line: str, in_block: bool) -> tuple[str, bool]:
    """Mask strings and comments with spaces; preserve length for column positions."""
    out: list[str] = []
    i = 0
    in_str: str | None = None
    verbatim = False

    while i < len(line):
        if in_block:
            if i + 1 < len(line) and line[i : i + 2] == "*/":
                out.append("  ")
                i += 2
                in_block = False
            else:
                out.append(" ")
                i += 1
            continue

        if in_str:
            ch = line[i]
            if verbatim:
                if ch == '"' and i + 1 < len(line) and line[i + 1] == '"':
                    out.append('""')
                    i += 2
                    continue
                if ch == '"':
                    out.append('"')
                    in_str = None
                    verbatim = False
                    i += 1
                    continue
                out.append(" ")
                i += 1
                continue
            if ch == "\\":
                out.append("  " if i + 1 < len(line) else " ")
                i += 2 if i + 1 < len(line) else 1
                continue
            out.append(ch if ch == in_str else " ")
            if ch == in_str:
                in_str = None
            i += 1
            continue

        if i + 1 < len(line) and line[i : i + 2] == "//":
            out.append(" " * (len(line) - i))
            break

        if i + 1 < len(line) and line[i : i + 2] == "/*":
            out.append("  ")
            i += 2
            in_block = True
            continue

        ch = line[i]
        if ch == "@":
            nxt = line[i + 1] if i + 1 < len(line) else ""
            if nxt == '"':
                in_str = '"'
                verbatim = True
                out.append('@"')
                i += 2
                continue
            if nxt == "$" and i + 2 < len(line) and line[i + 2] == '"':
                in_str = '"'
                verbatim = True
                out.append('@$"')
                i += 3
                continue

        if ch == '"':
            in_str = '"'
            out.append('"')
            i += 1
            continue
        if ch == "'":
            in_str = "'"
            out.append("'")
            i += 1
            continue

        out.append(ch)
        i += 1

    masked = "".join(out)
    if len(masked) < len(line):
        masked += " " * (len(line) - len(masked))
    return masked, in_block


def read_lines(path: Path) -> tuple[list[str] | None, str | None]:
    try:
        return path.read_text(encoding="utf-8").splitlines(), None
    except UnicodeDecodeError:
        try:
            return path.read_text(encoding="utf-8-sig").splitlines(), None
        except (UnicodeDecodeError, OSError) as ex:
            return None, str(ex)
    except OSError as ex:
        return None, str(ex)


def iter_cs(paths: list[Path]) -> Iterator[Path]:
    for root in paths:
        resolved = root.resolve()
        if resolved.is_file():
            if resolved.suffix.lower() in EXT:
                yield resolved
            continue
        if not resolved.is_dir():
            continue
        for p in resolved.rglob("*"):
            if p.is_file() and p.suffix.lower() in EXT and not any(x in SKIP for x in p.parts):
                yield p.resolve()


def find_repo_root(paths: list[Path]) -> Path:
    for raw in paths:
        start = raw.resolve()
        start = start.parent if start.is_file() else start
        cur = start
        while True:
            if (cur / ".git").is_dir() or any(cur.glob("*.sln")):
                return cur
            if cur.parent == cur:
                break
            cur = cur.parent
    dirs = [(p.resolve().parent if p.resolve().is_file() else p.resolve()) for p in paths]
    if not dirs:
        return Path(".").resolve()
    common = dirs[0]
    for d in dirs[1:]:
        a, b = common.parts, d.parts
        shared = []
        for x, y in zip(a, b, strict=False):
            if x == y:
                shared.append(x)
            else:
                break
        common = Path(*shared) if shared else Path(".")
    return common.resolve()


def class_name(line: str) -> str | None:
    m = re.search(
        r"\b(?:public|internal|private|protected|file)?\s*"
        r"(?:partial\s+|sealed\s+|static\s+|abstract\s+|record\s+)*"
        r"(?:class|struct|record)\s+(\w+)",
        line,
    )
    return m.group(1) if m else None


def valid_private(name: str) -> bool:
    return bool(re.fullmatch(r"_[A-Z][\w]*", name))


def scan_file(path: Path, max_len: int) -> list[str]:
    out: list[str] = []
    rel = str(path)
    lines, err = read_lines(path)
    if err is not None:
        out.append(emit(rel, 0, "read-failed", err))
        return out
    assert lines is not None

    cls: str | None = None
    in_block = False
    for n, raw in enumerate(lines, 1):
        cn = class_name(raw)
        if cn:
            cls = cn
        content = raw.rstrip("\n\r")
        visible = content.rstrip()
        if len(visible) > max_len:
            out.append(emit(rel, n, "line-length", f"{len(visible)}>{max_len}"))
        s = visible.strip()
        if not s or s.startswith(("//", "*")):
            masked, in_block = mask_line(raw, in_block)
            continue
        masked, in_block = mask_line(raw, in_block)
        for m in re.finditer(r"\bvar\b", masked):
            out.append(emit(rel, n, "var", col=m.start() + 1))
        if not re.search(r"\b(class|struct|interface|enum|record)\b", s):
            fm = re.search(
                rf"^\s*private(?:\s+{PRIVATE_ACCESS})*\s+{TYPE}?(\w+)\s*[=;]",
                masked,
            )
            if fm and fm.group(1) not in {"get", "set", "init", "add", "remove"}:
                if not valid_private(fm.group(1)):
                    out.append(emit(rel, n, "private-naming", f"field {fm.group(1)}"))
                continue
            mm = re.search(
                rf"^\s*private(?:\s+{PRIVATE_ACCESS})*\s+{TYPE}?(\w+)\s*\(",
                masked,
            )
            if mm and not (cls and mm.group(1) == cls) and not valid_private(mm.group(1)):
                out.append(emit(rel, n, "private-naming", f"method {mm.group(1)}"))
                continue
            pm = re.search(
                rf"^\s*private(?:\s+{PRIVATE_ACCESS})*\s+{TYPE}?(\w+)\s*\{{",
                masked,
            )
            if pm and not valid_private(pm.group(1)):
                out.append(emit(rel, n, "private-naming", f"property {pm.group(1)}"))
    return out


def scan_coverage(root: Path) -> list[str]:
    out: list[str] = []
    has_coverlet = False
    has_branch = False
    has_100 = False
    for pat in ("*.csproj", "*.props", "*.targets"):
        for f in root.rglob(pat):
            if any(x in SKIP for x in f.parts):
                continue
            try:
                t = f.read_text(encoding="utf-8")
            except OSError:
                continue
            tl = t.lower()
            if "coverlet" in tl:
                has_coverlet = True
            if THRESHOLD_BRANCH.search(t):
                has_branch = True
            if THRESHOLD_100.search(t):
                has_100 = True
    if not has_coverlet:
        out.append(emit("build", 0, "coverlet-missing"))
    elif not (has_branch and has_100):
        out.append(emit("build", 0, "branch-threshold-missing", "need Threshold=100 ThresholdType=branch"))
    return out


def run_coverage(proj: Path) -> list[str]:
    if not proj.is_file():
        return [emit(str(proj), 0, "coverage-failed", "project file not found")]
    cmd = [
        "dotnet", "test", str(proj), "-c", "Release",
        "--collect:XPlat Code Coverage", "--",
        "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Threshold=100",
        "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ThresholdType=branch",
    ]
    try:
        r = subprocess.run(cmd, capture_output=True, text=True, timeout=600)
    except FileNotFoundError:
        return [emit(str(proj), 0, "coverage-failed", "dotnet not found")]
    except subprocess.TimeoutExpired:
        return [emit(str(proj), 0, "coverage-failed", "timeout")]
    if r.returncode != 0:
        return [emit(str(proj), 0, "coverage-failed", f"exit={r.returncode}")]
    return []


def validate_inputs(paths: list[Path], files: list[Path], explicit: bool) -> list[str]:
    out: list[str] = []
    for p in paths:
        if not p.exists():
            out.append(emit(str(p), 0, "path-missing"))
    if out:
        return out
    if explicit and not files:
        out.append(emit("scan", 0, "no-cs-files"))
    return out


def main() -> int:
    p = argparse.ArgumentParser(description="C# convention checks; problems only.")
    p.add_argument("paths", nargs="*", default=["."])
    p.add_argument("--max-line-length", type=int, default=MAX_LINE)
    p.add_argument("--coverage", action="store_true", help="Check Coverlet branch-threshold config.")
    p.add_argument("--run-coverage", metavar="CSPROJ", help="Run dotnet test branch gate.")
    args = p.parse_args()

    raw_paths = args.paths
    explicit = not (len(raw_paths) == 1 and raw_paths[0] == ".")
    paths = [Path(x) for x in raw_paths]
    problems: list[str] = []

    files = list(iter_cs(paths))
    problems.extend(validate_inputs(paths, files, explicit))

    if not any("path-missing" in x for x in problems):
        for f in files:
            problems.extend(scan_file(f, args.max_line_length))

    if args.coverage and not any("path-missing" in x for x in problems):
        problems.extend(scan_coverage(find_repo_root(paths)))

    if args.run_coverage:
        problems.extend(run_coverage(Path(args.run_coverage).resolve()))

    if problems:
        print("\n".join(problems))
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
