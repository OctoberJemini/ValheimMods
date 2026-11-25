"""Validate a tested package against committed source before distributing it.

Never extracts a ZIP or executes its contents. No credentials needed.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import struct
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[2]


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)


def normalized(data):
    return data.decode('utf-8-sig').replace('\r\n', '\n').rstrip()


def validate(package, mod, version, sha256, ref):
    catalog = json.loads((ROOT / 'tools/release/catalog.json').read_text())
    if mod not in catalog or not re.fullmatch(r'\d+\.\d+\.\d+', version):
        raise ValueError('Unknown mod or invalid version')
    if not re.fullmatch(r'[a-fA-F0-9]{64}', sha256):
        raise ValueError('A full SHA256 checksum is required')
    if hashlib.sha256(Path(package).read_bytes()).hexdigest() != sha256.lower():
        raise ValueError('Package SHA256 mismatch')
    entry = catalog[mod]
    directory = 'mods/' + entry['directory']
    commit = git('rev-parse', '--verify', ref + '^{commit}').decode().strip()
    source = lambda name: git('show', commit + ':' + directory + '/' + name)
    with zipfile.ZipFile(package) as archive:
        members = [m for m in archive.infolist() if not m.is_dir()]
        names = [m.filename for m in members]
        if len(names) != len(set(n.lower() for n in names)):
            raise ValueError('Duplicate archive paths')
        if sum(m.file_size for m in members) > 100 * 1024 * 1024:
            raise ValueError('Unexpectedly large uncompressed package')
        dlls = []
        for name in names:
            path = PurePosixPath(name)
            if path.is_absolute() or '..' in path.parts or '\\' in name or ':' in name:
                raise ValueError('Unsafe archive path: ' + name)
            if path.suffix.lower() == '.dll':
                if path.name != entry['dll'] or not name.startswith('BepInEx/plugins/'):
                    raise ValueError('Unexpected binary: ' + name)
                dlls.append(name)
            elif name not in {'manifest.json', 'README.md', 'CHANGELOG.md', 'icon.png', 'LICENSE', 'LICENSE.md'}:
                raise ValueError('Unexpected package file: ' + name)
        if len(dlls) != 1:
            raise ValueError('Expected exactly one plugin DLL')
        manifest = json.loads(archive.read('manifest.json'))
        if manifest != json.loads(source('manifest.json')):
            raise ValueError('Manifest differs from committed source')
        if manifest['name'] != mod or manifest['version_number'] != version:
            raise ValueError('Mod/version does not match request')
        for name in ['README.md', 'CHANGELOG.md']:
            if normalized(archive.read(name)) != normalized(source(name)):
                raise ValueError(name + ' differs from committed source')
        icon = archive.read('icon.png')
        if icon != source('icon.png') or icon[:8] != b'\x89PNG\r\n\x1a\n' or struct.unpack('>II', icon[16:24]) != (256, 256):
            raise ValueError('Icon differs from source or is not 256x256 PNG')
        if not archive.read(dlls[0]).startswith(b'MZ'):
            raise ValueError('Invalid plugin binary')
        binary_hash = hashlib.sha256(archive.read(dlls[0])).hexdigest()
    return {**entry, 'mod': mod, 'version': version, 'commit': commit,
            'sha256': sha256.lower(), 'dll_sha256': binary_hash,
            'description': manifest['description']}


def main():
    parser = argparse.ArgumentParser()
    for name in ['package', 'mod', 'version', 'sha256', 'ref']:
        parser.add_argument('--' + name, required=True)
    args = parser.parse_args()
    result = validate(**vars(args))
    out = ROOT / 'release-work'
    out.mkdir(exist_ok=True)
    (out / 'validated.json').write_text(json.dumps(result, indent=2) + '\n')
    # JSON string/list syntax is compatible with the TOML values used here.
    q = json.dumps
    (out / 'thunderstore.toml').write_text(
        '[config]\nschemaVersion = "0.0.1"\n[package]\nnamespace = "OctoberJemini"\n'
        + 'name = ' + q(result['mod']) + '\nversionNumber = ' + q(result['version'])
        + '\ndescription = ' + q(result['description'])
        + '\nwebsiteUrl = "https://github.com/OctoberJemini/ValheimMods"\ncontainsNsfwContent = false\n'
        + '[publish]\nrepository = "https://thunderstore.io"\ncommunities = ["valheim"]\n'
        + '[publish.categories]\nvalheim = ' + q(result['categories']) + '\n', encoding='utf-8')
    if os.getenv('GITHUB_OUTPUT'):
        with open(os.environ['GITHUB_OUTPUT'], 'a', encoding='utf-8') as f:
            for key in ['description', 'nexus_mod_id', 'nexus_file_id']:
                value = str(result[key] or '')
                if '\n' in value or '\r' in value:
                    raise ValueError('Invalid multiline metadata')
                f.write(f'{key}={value}\n')
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    main()
