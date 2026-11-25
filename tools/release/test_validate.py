import hashlib
import json
from pathlib import Path
import struct
import tempfile
import unittest
from unittest.mock import patch
import zipfile

import validate


class PackageValidationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.package = Path(self.temp.name) / 'mod.zip'
        self.files = {
            'manifest.json': json.dumps({'name': 'Armor_Inscriptions', 'version_number': '0.5.2', 'description': 'Test'}).encode(),
            'README.md': b'Tested description\n',
            'CHANGELOG.md': b'Change notes\n',
            'icon.png': b'\x89PNG\r\n\x1a\n' + b'\0' * 8 + struct.pack('>II', 256, 256),
            'BepInEx/plugins/OctoberJemini-Armor_Inscriptions/ArmorInscriptions.dll': b'MZfixture',
        }
        self.source = dict(self.files)

    def run_check(self, checksum=None):
        with zipfile.ZipFile(self.package, 'w') as archive:
            for name, value in self.files.items():
                archive.writestr(name, value)
        digest = checksum or hashlib.sha256(self.package.read_bytes()).hexdigest()

        def fake_git(*args):
            if args[0] == 'rev-parse':
                return b'a' * 40
            return self.source[args[1].split('/')[-1]]

        with patch.object(validate, 'git', fake_git):
            return validate.validate(self.package, 'Armor_Inscriptions', '0.5.2', digest, 'HEAD')

    def test_valid_package_and_line_endings(self):
        self.files['README.md'] = b'Tested description\r\n\r\n'
        self.assertEqual(self.run_check()['version'], '0.5.2')

    def test_wrong_checksum(self):
        with self.assertRaisesRegex(ValueError, 'SHA256 mismatch'):
            self.run_check('0' * 64)

    def test_stale_source(self):
        self.files['README.md'] = b'Old README'
        with self.assertRaisesRegex(ValueError, 'differs from committed'):
            self.run_check()

    def test_path_traversal(self):
        self.files['../secret.txt'] = b'bad'
        with self.assertRaisesRegex(ValueError, 'Unsafe archive'):
            self.run_check()

    def test_dependency_binary_rejected(self):
        self.files['BepInEx/plugins/assembly_valheim.dll'] = b'MZ'
        with self.assertRaisesRegex(ValueError, 'Unexpected binary'):
            self.run_check()

    def test_extra_configuration_rejected(self):
        self.files['BepInEx/config/private.cfg'] = b'secret'
        with self.assertRaisesRegex(ValueError, 'Unexpected package file'):
            self.run_check()

    def test_duplicate_case_rejected(self):
        self.files['readme.md'] = b'other'
        with self.assertRaisesRegex(ValueError, 'Duplicate archive'):
            self.run_check()

    def test_missing_dll(self):
        self.files = {k: v for k, v in self.files.items() if not k.endswith('.dll')}
        with self.assertRaisesRegex(ValueError, 'exactly one'):
            self.run_check()


if __name__ == '__main__':
    unittest.main()
