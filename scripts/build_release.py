"""Validate and package the Unity extension using only the Python standard library."""

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import sys
import zipfile

ROOT = Path(__file__).resolve().parent.parent
PACKAGE = ROOT / "Packages" / "com.azipa.distance-fade-controller"
VERSION_PATTERN = r"(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([0-9A-Za-z.-]+))?"


def build(tag=None, output=None):
    manifest = json.loads((PACKAGE / "package.json").read_text(encoding="utf-8-sig"))
    version = manifest["version"]
    match = re.fullmatch(VERSION_PATTERN, version)
    if not match:
        raise ValueError(f"Invalid package version: {version}")
    if tag is not None and tag != "v" + version:
        raise ValueError(f"Tag {tag} does not match package version v{version}")
    prerelease = match.group(1) == "0" or match.group(4) is not None
    required = ["README.md", "CHANGELOG.md", "LICENSE", "Runtime/DistanceFadeController.cs",
                "Editor/DistanceFadeBuildPlugin.cs", "Prefabs/DistanceFadeController.prefab"]
    for relative in required:
        if not (PACKAGE / relative).is_file():
            raise ValueError(f"Required file missing: {relative}")

    files = sorted(p for p in PACKAGE.rglob("*") if p.is_file())
    guids = set()
    for path in files:
        relative = path.relative_to(PACKAGE)
        if path.is_symlink():
            raise ValueError(f"Symlink not allowed in package: {relative}")
        if any(part.startswith(".") or part == "__pycache__" for part in relative.parts):
            raise ValueError(f"Unexpected hidden/generated file: {relative}")
        ignored_by_unity = any(part.endswith("~") for part in relative.parts[:-1])
        if path.suffix != ".meta" and not ignored_by_unity:
            if not Path(str(path) + ".meta").is_file():
                raise ValueError(f"Unity .meta missing: {relative}")
        if path.suffix == ".meta":
            guid = re.search(r"^guid: ([0-9a-f]{32})$", path.read_text(encoding="utf-8-sig"), re.M)
            if not guid or guid.group(1) in guids:
                raise ValueError(f"Missing or duplicated GUID: {relative}")
            guids.add(guid.group(1))
    for directory in (p for p in PACKAGE.rglob("*") if p.is_dir()):
        if not any(part.endswith("~") for part in directory.relative_to(PACKAGE).parts):
            if not Path(str(directory) + ".meta").is_file():
                raise ValueError(f"Unity folder .meta missing: {directory.relative_to(PACKAGE)}")
    prefab = (PACKAGE / "Prefabs/DistanceFadeController.prefab").read_text(encoding="utf-8-sig")
    script_meta = (PACKAGE / "Runtime/DistanceFadeController.cs.meta").read_text(encoding="utf-8-sig")
    script_guid = re.search(r"^guid: ([0-9a-f]{32})$", script_meta, re.M).group(1)
    if f"guid: {script_guid}" not in prefab:
        raise ValueError("Prefab does not reference DistanceFadeController.cs")

    changelog = (PACKAGE / "CHANGELOG.md").read_text(encoding="utf-8-sig")
    section = re.search(r"^## " + re.escape(version) + r"(?:[^\n]*)\n(.*?)(?=^## |\Z)", changelog, re.M | re.S)
    if not section:
        raise ValueError(f"CHANGELOG entry missing for {version}")
    destination = Path(output) if output else ROOT / "Releases"
    destination.mkdir(parents=True, exist_ok=True)
    archive = destination / f"{manifest['name']}-{version}.zip"
    # Stable timestamps and file modes produce identical ZIPs on Windows and Linux.
    with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED) as package_zip:
        for path in files:
            info = zipfile.ZipInfo(path.relative_to(PACKAGE).as_posix(), date_time=(2020, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.create_system = 3
            info.external_attr = 0o100644 << 16
            if path.suffix in {".cs", ".asmdef", ".meta", ".prefab", ".shader", ".json", ".md"} or path.name == "LICENSE":
                content = path.read_text(encoding="utf-8-sig").encode("utf-8")
            else:
                content = path.read_bytes()
            package_zip.writestr(info, content)
    digest = hashlib.sha256(archive.read_bytes()).hexdigest()
    checksum = Path(str(archive) + ".sha256")
    checksum.write_text(f"{digest}  {archive.name}\n", encoding="utf-8")
    notes = destination / "release-notes.md"
    title = f"DistanceFadeController {version}" + (" (Beta)" if prerelease else "")
    notes.write_text(title + "\n\n" + section.group(1).strip() + "\n", encoding="utf-8")
    values = {"version": version, "title": title, "prerelease": str(prerelease).lower(),
              "zip": archive.as_posix(), "checksum": checksum.as_posix(), "notes": notes.as_posix()}
    if os.environ.get("GITHUB_OUTPUT"):
        with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output_file:
            for key, value in values.items():
                output_file.write(f"{key}={value}\n")
    print(json.dumps(values, ensure_ascii=False, indent=2))
    return archive


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--tag", help="Require exact vVERSION match before publishing")
    parser.add_argument("--output", type=Path, help="Output folder (default: Releases)")
    args = parser.parse_args()
    try:
        build(args.tag, args.output)
    except (ValueError, KeyError, OSError, json.JSONDecodeError) as error:
        print(f"Release validation failed: {error}", file=sys.stderr)
        sys.exit(1)
