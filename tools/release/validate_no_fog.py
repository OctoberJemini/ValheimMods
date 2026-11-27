"""Validate the linked No Fog release without extracting or executing package files."""
import hashlib
import json
import os
from pathlib import Path
import re
import struct
import subprocess
import zipfile

version = os.environ['VERSION']
expected = os.environ['SHA256']
assert re.fullmatch(r'\d+\.\d+\.\d+', version), 'Invalid version'
assert re.fullmatch(r'[a-fA-F0-9]{64}', expected), 'Invalid checksum'
package = Path('release-work') / f'Epic_Loot_No_Fog_Destinations-{version}.zip'
assert hashlib.sha256(package.read_bytes()).hexdigest() == expected.lower(), 'ZIP checksum mismatch'
source = Path('linked-source')
commit = subprocess.check_output(['git', '-C', str(source), 'rev-parse', 'HEAD'], text=True).strip()
files = {'manifest.json': 'thunderstore/manifest.json', 'icon.png': 'thunderstore/icon.png',
         'README.md': 'README.md', 'CHANGELOG.md': 'CHANGELOG.md', 'LICENSE': 'LICENSE',
         'THIRD_PARTY_NOTICES.md': 'THIRD_PARTY_NOTICES.md'}
dll = 'plugins/EpicLootNoFogDestinations/Valheim.EpicLootNoFogDestinations.dll'
with zipfile.ZipFile(package) as z:
    names = [i.filename for i in z.infolist() if not i.is_dir()]
    assert len(names) == len(set(names)), 'Duplicate entries'
    assert set(names) == set(files) | {dll}, 'Unexpected or missing files'
    assert sum(i.file_size for i in z.infolist()) < 10 * 1024 * 1024, 'Unexpected package size'
    for name, path in files.items():
        actual, wanted = z.read(name), (source / path).read_bytes()
        if name != 'icon.png':
            actual = actual.decode('utf-8-sig').replace('\r\n', '\n').rstrip()
            wanted = wanted.decode('utf-8-sig').replace('\r\n', '\n').rstrip()
        assert actual == wanted, f'{name} differs from tagged source'
    manifest = json.loads(z.read('manifest.json'))
    assert manifest['name'] == 'Epic_Loot_No_Fog_Destinations'
    assert manifest['version_number'] == version
    assert f'PluginVersion = "{version}"' in (source / 'src/EpicLootNoFogDestinationsPlugin.cs').read_text()
    icon = z.read('icon.png')
    assert icon[:8] == b'\x89PNG\r\n\x1a\n' and struct.unpack('>II', icon[16:24]) == (256, 256)
    binary = z.read(dll)
    assert binary.startswith(b'MZ'), 'Invalid DLL'
    dll_hash = hashlib.sha256(binary).hexdigest()
q = json.dumps
Path('release-work/thunderstore.toml').write_text(
    '[config]\nschemaVersion = "0.0.1"\n[package]\nnamespace = "OctoberJemini"\n'
    + 'name = ' + q(manifest['name']) + '\nversionNumber = ' + q(version)
    + '\ndescription = ' + q(manifest['description'])
    + '\nwebsiteUrl = ' + q(manifest['website_url']) + '\ncontainsNsfwContent = false\n'
    + '[publish]\nrepository = "https://thunderstore.io"\ncommunities = ["valheim"]\n'
    + '[publish.categories]\nvalheim = ["ai-generated", "client-side", "mods", "tweaks", "utility"]\n', encoding='utf-8')
summary = f'No Fog Destinations {version}\nSource: {commit}\nZIP SHA256: {expected}\nDLL SHA256: {dll_hash}\n'
print(summary)
if os.getenv('GITHUB_STEP_SUMMARY'):
    with open(os.environ['GITHUB_STEP_SUMMARY'], 'a') as f: f.write(summary)
