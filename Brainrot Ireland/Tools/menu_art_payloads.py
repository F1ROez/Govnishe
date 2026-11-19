"""Strict reader for the three illustrated UI PNGs packed as small ASCII source parts.

This validates delivery bytes only. It does not compile Unity or verify its rendered UI.
"""
from pathlib import Path
import base64
import binascii
import hashlib
import json
import re
import struct

TEXTURES = ("IslandMenuAtlas", "IslandControlsAtlas", "IslandMenuBackdrop", "IslandUpgradeIcons")
PART_CHARS = 32768
MAX_PARTS = 128
MAX_PNG_BYTES = 8 * 1024 * 1024
PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"


def _unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON key: " + key)
        result[key] = value
    return result


def png_size(data):
    """Validate PNG structure and CRCs, then return its source dimensions."""
    if not isinstance(data, bytes) or not data.startswith(PNG_SIGNATURE):
        raise ValueError("Invalid PNG signature")
    offset, dimensions, image_data, ended = 8, None, False, False
    while offset < len(data):
        if offset + 12 > len(data):
            raise ValueError("Truncated PNG chunk")
        length = struct.unpack_from(">I", data, offset)[0]
        end = offset + 12 + length
        if end > len(data):
            raise ValueError("PNG chunk exceeds source bytes")
        kind = data[offset + 4:offset + 8]
        body = data[offset + 8:offset + 8 + length]
        crc = struct.unpack_from(">I", data, offset + 8 + length)[0]
        if binascii.crc32(kind + body) & 0xffffffff != crc:
            raise ValueError("Invalid PNG chunk CRC")
        if dimensions is None:
            if kind != b"IHDR" or length != 13:
                raise ValueError("PNG must start with a 13-byte IHDR")
            width, height = struct.unpack_from(">II", body)
            if not 0 < width <= 4096 or not 0 < height <= 4096:
                raise ValueError("PNG source dimensions exceed UI import limits")
            dimensions = width, height
        elif kind == b"IHDR":
            raise ValueError("Duplicate PNG IHDR")
        if kind == b"IDAT":
            image_data = True
        if kind == b"IEND":
            if length != 0 or end != len(data):
                raise ValueError("Invalid PNG IEND or trailing bytes")
            ended = True
            break
        offset = end
    if dimensions is None or not image_data or not ended:
        raise ValueError("Incomplete PNG source")
    return dimensions


def read_payloads(manifest_path):
    """Return {resource_name: verified PNG bytes}, rejecting unsafe or corrupt delivery."""
    path = Path(manifest_path)
    try:
        manifest = json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=_unique_object)
    except (OSError, UnicodeError, ValueError) as exc:
        raise ValueError("Cannot read UI art manifest: " + str(exc)) from exc
    if not isinstance(manifest, dict) or type(manifest.get("version")) is not int or manifest["version"] != 1:
        raise ValueError("Unsupported UI art manifest version")
    entries = manifest.get("textures")
    if not isinstance(entries, list) or len(entries) != len(TEXTURES):
        raise ValueError("UI art manifest must contain exactly three textures")
    root, result, names = path.parent.resolve(), {}, set()
    for entry in entries:
        if not isinstance(entry, dict):
            raise ValueError("Invalid UI texture manifest entry")
        name = entry.get("name")
        if name not in TEXTURES or name in names:
            raise ValueError("Unknown or duplicate UI texture: " + str(name))
        names.add(name)
        if entry.get("file") != name + ".png":
            raise ValueError("Invalid UI texture output filename: " + str(entry.get("file")))
        length, digest, parts = entry.get("length"), entry.get("sha256"), entry.get("chunks")
        if type(length) is not int or not 0 < length <= MAX_PNG_BYTES:
            raise ValueError("Invalid UI texture byte length: " + name)
        if not isinstance(digest, str) or not re.fullmatch("[0-9a-f]{64}", digest):
            raise ValueError("Invalid UI texture SHA256: " + name)
        if not isinstance(parts, list) or not 1 <= len(parts) <= MAX_PARTS:
            raise ValueError("Invalid UI texture chunk list: " + name)
        encoded = []
        for index, relative in enumerate(parts):
            expected = name + "/" + format(index, "03d") + ".b64"
            if relative != expected:
                raise ValueError("Invalid or unordered UI texture chunk: " + str(relative))
            source = (root / relative).resolve()
            try:
                source.relative_to(root)
            except ValueError as exc:
                raise ValueError("UI texture chunk leaves source folder") from exc
            try:
                chunk = source.read_bytes().decode("ascii")
            except (OSError, UnicodeError) as exc:
                raise ValueError("Cannot read UI texture chunk: " + relative) from exc
            # The packer writes no newline. Accept one terminal LF for standard text tooling.
            if chunk.endswith("\n"):
                chunk = chunk[:-1]
            if not chunk or len(chunk) > PART_CHARS or len(chunk) % 4 or not re.fullmatch("[A-Za-z0-9+/]*={0,2}", chunk):
                raise ValueError("Invalid UI texture base64 chunk: " + relative)
            if index < len(parts) - 1 and (len(chunk) != PART_CHARS or "=" in chunk):
                raise ValueError("Nonfinal UI chunk has invalid length or padding: " + relative)
            encoded.append(chunk)
        try:
            data = base64.b64decode("".join(encoded), validate=True)
        except (ValueError, binascii.Error) as exc:
            raise ValueError("Invalid UI texture base64: " + name) from exc
        if len(data) != length or hashlib.sha256(data).hexdigest() != digest:
            raise ValueError("UI texture byte length or SHA256 mismatch: " + name)
        png_size(data)
        result[name] = data
    return result
