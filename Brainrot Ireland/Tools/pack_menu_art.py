"""Package the three generated menu PNGs as bounded text payloads for repository transfer.

The Editor reconstructs these sources automatically; their pixels and dimensions are unchanged.
"""
from __future__ import annotations
import argparse
import base64
import hashlib
import json
from pathlib import Path
import uuid

PROJECT = Path(__file__).resolve().parents[1]
PAYLOAD = Path('Assets/Brainrot/Generated/Editor/MenuArtPayloads')
OUTPUT = Path('Assets/Brainrot/Generated/Resources')
NAMES = ('IslandMenuAtlas', 'IslandControlsAtlas', 'IslandMenuBackdrop', 'IslandUpgradeIcons')
CHUNK_SIZE = 32768


def meta(path: Path, folder: bool = False) -> None:
    target = Path(str(path) + '.meta')
    if target.exists():
        return
    target.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n' +
                      ('folderAsset: yes\n' if folder else '') +
                      'DefaultImporter:\n  externalObjects: {}\n  userData:\n'
                      '  assetBundleName:\n  assetBundleVariant:\n', encoding='utf-8')


def pack(project: Path) -> None:
    root = project / PAYLOAD
    root.mkdir(parents=True, exist_ok=True)
    for directory in (root.parent, root):
        meta(directory, True)
    entries = []
    for name in NAMES:
        raw = (project / OUTPUT / (name + '.png')).read_bytes()
        if raw[:8] != b'\x89PNG\r\n\x1a\n':
            raise ValueError('Not a PNG: ' + name)
        encoded = base64.b64encode(raw).decode('ascii')
        folder = root / name
        folder.mkdir(exist_ok=True)
        meta(folder, True)
        chunks = []
        for index, offset in enumerate(range(0, len(encoded), CHUNK_SIZE)):
            filename = f'{index:03d}.b64'
            part = folder / filename
            part.write_text(encoded[offset:offset + CHUNK_SIZE], encoding='ascii')
            meta(part)
            chunks.append(name + '/' + filename)
        # Remove only obsolete numbered generated parts; preserve any unrelated local files.
        keep = {Path(chunk).name for chunk in chunks}
        for part in folder.glob('[0-9][0-9][0-9].b64'):
            if part.name not in keep:
                part.unlink()
                Path(str(part) + '.meta').unlink(missing_ok=True)
        entries.append(dict(name=name, file=name + '.png', length=len(raw),
                            sha256=hashlib.sha256(raw).hexdigest(), chunks=chunks))
    manifest = root / 'manifest.json'
    manifest.write_text(json.dumps(dict(version=1, textures=entries), indent=2) + '\n', encoding='utf-8')
    meta(manifest)
    print(f'Packed {len(entries)} PNGs in {sum(len(e["chunks"]) for e in entries)} text parts; original pixels unchanged.')


def verify(project: Path) -> None:
    # Shared with the standalone validator, including verification when generated outputs are absent.
    from menu_art_payloads import read_payloads
    packed = read_payloads(project / PAYLOAD / 'manifest.json')
    for name, raw in packed.items():
        output = project / OUTPUT / (name + '.png')
        if output.exists() and output.read_bytes() != raw:
            raise ValueError('Generated output differs: ' + name)
    print('Verified all packed checksums, PNG headers and existing outputs.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, default=PROJECT)
    parser.add_argument('--verify', action='store_true')
    args = parser.parse_args()
    if args.verify:
        verify(args.project)
    else:
        pack(args.project)
