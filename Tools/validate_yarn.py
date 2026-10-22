#!/usr/bin/env python3
"""Check the game's Yarn dialogue for structural problems, without Unity.

    python3 Tools/validate_yarn.py            # checks Assets/**/*.yarn
    python3 Tools/validate_yarn.py path ...   # specific files or folders

Errors (exit code 1):
  - missing target   <<jump X>>, <<detour X>> or <<handoff ... X>> names a node that doesn't exist
  - duplicate title  two nodes share a title (unless every copy has a `when:` header,
                     which Yarn 3 allows for node groups)
  - stray header     a body line that is exactly `---` (starts a second header inside a node)
  - unclosed node    a node has no `===`

Warnings (exit code 0):
  - orphan           no jump reaches the node and nothing in a scene/prefab starts it
  - dash line        a body line starting with "-" but not "->": Yarn reads it as plain
                     text, and the earlier "colon + dash" parser trouble came from lines
                     like this. Use "->" for an option or "•" for a bullet.
  - speaker dash     "Name: - text"; same trouble, after a speaker name

Entry points (never orphans): titles listed in --entry, any node title that appears as a
value in a .unity/.prefab/.asset file under Assets (startNode, joinNode, walkingNodes list
items and so on, so NPCs placed in the scene count), and nodes titled Start.
"""
import argparse
import pathlib
import re
import sys
from collections import defaultdict

ROOT = pathlib.Path(__file__).resolve().parent.parent
JUMP_RE = re.compile(r"<<\s*(jump|detour)\s+([A-Za-z_][\w.]*)\s*>>")
HANDOFF_RE = re.compile(r"<<\s*handoff\s+([^>]*?)\s*>>")
# Any YAML scalar or list item in a scene/prefab/asset; later intersected with node titles,
# so fields like startNode, joinNode and list fields like walkingNodes all count.
SCENE_VALUE_RE = re.compile(r"^\s*(?:-|[A-Za-z_]\w*:)\s*([A-Za-z_][\w.]*)\s*$", re.M)


def parse_yarn(path):
    """Return a list of nodes: dict(title, headers, body=[(lineno, text)], file, line)."""
    nodes, problems = [], []
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    i, n = 0, len(lines)
    while i < n:
        # skip blank lines and comments between nodes
        if not lines[i].strip() or lines[i].lstrip().startswith("//"):
            i += 1
            continue
        start = i
        headers = {}
        while i < n and lines[i].strip() != "---":
            m = re.match(r"\s*([A-Za-z_][\w]*)\s*:\s*(.*)$", lines[i])
            if m:
                headers.setdefault(m.group(1), m.group(2).strip())
            i += 1
        if i >= n:
            problems.append(("error", path, start + 1, "header with no `---` (node never starts)"))
            break
        i += 1  # past ---
        body = []
        while i < n and lines[i].strip() != "===":
            body.append((i + 1, lines[i]))
            i += 1
        title = headers.get("title")
        node = dict(title=title, headers=headers, body=body, file=path, line=start + 1)
        if i >= n:
            problems.append(("error", path, start + 1, f"node '{title}' has no closing `===`"))
        else:
            i += 1  # past ===
        if not title:
            problems.append(("error", path, start + 1, "node without a title: header"))
        else:
            nodes.append(node)
    return nodes, problems


def scene_entry_points():
    found = set()
    for ext in ("*.unity", "*.prefab", "*.asset"):
        for p in (ROOT / "Assets").rglob(ext):
            try:
                txt = p.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue
            found.update(SCENE_VALUE_RE.findall(txt))
    return found


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("paths", nargs="*", help="files or folders (default: Assets)")
    ap.add_argument("--entry", action="append", default=[], help="extra entry-point node titles")
    ap.add_argument("--no-scene-scan", action="store_true", help="don't read startNode values from scenes")
    args = ap.parse_args()

    targets = [pathlib.Path(p) for p in args.paths] or [ROOT / "Assets"]
    files = []
    for t in targets:
        files += sorted(t.rglob("*.yarn")) if t.is_dir() else [t]
    if not files:
        print("No .yarn files found.")
        return 1

    problems, nodes = [], []
    for f in files:
        ns, ps = parse_yarn(f)
        nodes += ns
        problems += ps

    by_title = defaultdict(list)
    for nd in nodes:
        by_title[nd["title"]].append(nd)
    for title, group in by_title.items():
        if len(group) > 1 and not all("when" in g["headers"] for g in group):
            where = ", ".join(f"{g['file'].name}:{g['line']}" for g in group)
            problems.append(("error", group[0]["file"], group[0]["line"], f"duplicate title '{title}' ({where})"))

    referenced = set()
    for nd in nodes:
        for lineno, text in nd["body"]:
            if text.lstrip().startswith("//"):
                continue  # whole-line comment
            code = text
            for _, target in JUMP_RE.findall(code):
                referenced.add(target)
                if target not in by_title:
                    problems.append(("error", nd["file"], lineno, f"'{nd['title']}' jumps to missing node '{target}'"))
            for args_ in HANDOFF_RE.findall(code):
                for target in re.findall(r"[A-Za-z_][\w.]*", args_):
                    if target in by_title:
                        referenced.add(target)
            stripped = text.strip()
            if stripped == "---":
                problems.append(("error", nd["file"], lineno, f"stray `---` inside node '{nd['title']}'"))
            elif stripped.startswith("-") and not stripped.startswith("->"):
                problems.append(("warning", nd["file"], lineno, f"dash line in '{nd['title']}': {stripped[:60]!r} (use -> or •)"))
            elif re.match(r"^[A-Za-z][\w .'\-]{0,40}:\s+-(?!>)\s", stripped):
                problems.append(("warning", nd["file"], lineno, f"speaker followed by dash in '{nd['title']}': {stripped[:60]!r}"))

    entries = set(args.entry) | {"Start"}
    if not args.no_scene_scan:
        entries |= scene_entry_points()
    for title, group in by_title.items():
        if title not in referenced and title not in entries:
            g = group[0]
            problems.append(("warning", g["file"], g["line"], f"orphan node '{title}' (nothing jumps to it or starts it)"))

    order = {"error": 0, "warning": 1}
    problems.sort(key=lambda p: (order[p[0]], str(p[1]), p[2]))
    for kind, f, line, msg in problems:
        try:
            rel = f.resolve().relative_to(ROOT)
        except ValueError:
            rel = f
        print(f"{kind.upper():7} {rel}:{line}  {msg}")
    errs = sum(1 for p in problems if p[0] == "error")
    warns = len(problems) - errs
    print(f"\n{len(files)} file(s), {len(nodes)} node(s): {errs} error(s), {warns} warning(s).")
    return 1 if errs else 0


if __name__ == "__main__":
    sys.exit(main())
