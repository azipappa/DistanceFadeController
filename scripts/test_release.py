"""Release checks independent of Unity and GitHub credentials."""

import hashlib
import json
from pathlib import Path
import tempfile
import unittest
import zipfile

from build_release import PACKAGE, ROOT, build

VERSION = json.loads((PACKAGE / "package.json").read_text(encoding="utf-8-sig"))["version"]
TEMP_ROOT = ROOT / "Releases"


class ReleaseTests(unittest.TestCase):
    def test_package_and_checksum_are_repeatable(self):
        TEMP_ROOT.mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(dir=TEMP_ROOT) as temporary:
            archive = build("v" + VERSION, temporary)
            first = archive.read_bytes()
            with zipfile.ZipFile(archive) as package:
                self.assertIn("package.json", package.namelist())
                self.assertIn("Runtime/DistanceFadeController.cs.meta", package.namelist())
                self.assertNotIn("Library", package.namelist())
                self.assertEqual(VERSION, json.loads(package.read("package.json"))["version"])
            checksum = Path(str(archive) + ".sha256").read_text(encoding="utf-8").split()[0]
            self.assertEqual(hashlib.sha256(first).hexdigest(), checksum)
            build("v" + VERSION, temporary)
            self.assertEqual(first, archive.read_bytes())
            self.assertIn(VERSION, (Path(temporary) / "release-notes.md").read_text(encoding="utf-8"))

    def test_wrong_tag_is_rejected_before_creating_files(self):
        TEMP_ROOT.mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(dir=TEMP_ROOT) as temporary:
            with self.assertRaises(ValueError):
                build("wrong-tag", temporary)
            self.assertEqual([], list(Path(temporary).iterdir()))


if __name__ == "__main__":
    unittest.main()
