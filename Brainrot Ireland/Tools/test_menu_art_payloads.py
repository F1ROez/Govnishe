"""Delivery regressions for the exact bytes that Unity's editor installer materializes."""
import base64
import binascii
import copy
import hashlib
import json
from pathlib import Path
import random
import struct
import tempfile
import unittest
import zlib

from menu_art_payloads import PART_CHARS, PNG_SIGNATURE, TEXTURES, png_size, read_payloads


def png(width=2, height=3, noisy=False):
    def chunk(kind, body):
        return struct.pack(">I", len(body)) + kind + body + struct.pack(">I", binascii.crc32(kind + body) & 0xffffffff)
    pixels = random.Random(941).randbytes(width * height * 4) if noisy else bytes((12, 175, 220, 255)) * width * height
    scanlines = b"".join(b"\0" + pixels[y * width * 4:(y + 1) * width * 4] for y in range(height))
    header = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    return PNG_SIGNATURE + chunk(b"IHDR", header) + chunk(b"IDAT", zlib.compress(scanlines)) + chunk(b"IEND", b"")


class MenuArtPayloadTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="brainrot-packed-ui-")
        self.root = Path(self.temp.name)
        self.manifest = self.root / "manifest.json"
        self.raw = png(128, 128, noisy=True)
        self.data = {"version": 1, "textures": []}
        for name in TEXTURES:
            encoded = base64.b64encode(self.raw).decode("ascii")
            chunks = []
            (self.root / name).mkdir()
            for index, start in enumerate(range(0, len(encoded), PART_CHARS)):
                relative = name + "/" + format(index, "03d") + ".b64"
                (self.root / relative).write_text(encoded[start:start + PART_CHARS], encoding="ascii")
                chunks.append(relative)
            self.data["textures"].append({"name": name, "file": name + ".png", "length": len(self.raw), "sha256": hashlib.sha256(self.raw).hexdigest(), "chunks": chunks})
        self.write()

    def tearDown(self):
        self.temp.cleanup()

    def write(self):
        self.manifest.write_text(json.dumps(self.data), encoding="utf-8")

    def test_roundtrip_preserves_every_byte_and_npot_dimensions(self):
        result = read_payloads(self.manifest)
        self.assertEqual(set(TEXTURES), set(result))
        self.assertGreater(len(self.data["textures"][0]["chunks"]), 1)
        for name in TEXTURES:
            self.assertEqual(self.raw, result[name])
        self.assertEqual((127, 93), png_size(png(127, 93)))

    def test_missing_or_reordered_part_cannot_be_accepted(self):
        part = self.root / self.data["textures"][0]["chunks"][0]
        part.unlink()
        with self.assertRaisesRegex(ValueError, "Cannot read"):
            read_payloads(self.manifest)
        self.data["textures"][1]["chunks"].reverse()
        self.write()
        # Missing first texture is independently invalid; test sequence on the intact one.
        self.data["textures"] = self.data["textures"][1:] + self.data["textures"][:1]
        self.write()
        with self.assertRaisesRegex(ValueError, "unordered"):
            read_payloads(self.manifest)

    def test_base64_corruption_is_detected_even_when_it_still_decodes(self):
        part = self.root / self.data["textures"][0]["chunks"][1]
        text = part.read_text()
        part.write_text(("A" if text[0] != "A" else "B") + text[1:])
        with self.assertRaisesRegex(ValueError, "SHA256"):
            read_payloads(self.manifest)

    def test_declared_length_must_match_decoded_png(self):
        self.data["textures"][0]["length"] += 1
        self.write()
        with self.assertRaisesRegex(ValueError, "length or SHA256"):
            read_payloads(self.manifest)

    def test_invalid_base64_or_padding_is_rejected(self):
        part = self.root / self.data["textures"][0]["chunks"][0]
        for marker in ["!", "=", "\n"]:
            with self.subTest(marker=repr(marker)):
                original = base64.b64encode(self.raw).decode("ascii")[:PART_CHARS]
                part.write_text(original[:10] + marker + original[11:])
                with self.assertRaises(ValueError):
                    read_payloads(self.manifest)

    def test_duplicate_texture_or_duplicate_json_key_is_rejected(self):
        original = copy.deepcopy(self.data)
        self.data["textures"][2] = self.data["textures"][0]
        self.write()
        with self.assertRaisesRegex(ValueError, "duplicate UI texture"):
            read_payloads(self.manifest)
        text = json.dumps(original)
        self.manifest.write_text(text.replace('"version": 1', '"version": 1, "version": 1'))
        with self.assertRaisesRegex(ValueError, "Duplicate JSON"):
            read_payloads(self.manifest)

    def test_output_filename_and_chunk_path_cannot_traverse(self):
        original = copy.deepcopy(self.data)
        for field, value in [("file", "../IslandMenuAtlas.png"), ("chunks", ["../../outside.b64"])]:
            with self.subTest(field=field):
                self.data = copy.deepcopy(original)
                self.data["textures"][0][field] = value
                self.write()
                with self.assertRaises(ValueError):
                    read_payloads(self.manifest)

    def test_symlinked_part_cannot_leave_payload_folder(self):
        with tempfile.TemporaryDirectory(prefix="outside-ui-art-") as outside:
            source = Path(outside) / "000.b64"
            part = self.root / self.data["textures"][0]["chunks"][0]
            source.write_bytes(part.read_bytes())
            part.unlink()
            try:
                part.symlink_to(source)
            except OSError:
                self.skipTest("Symlinks require platform permission")
            with self.assertRaisesRegex(ValueError, "leaves source folder"):
                read_payloads(self.manifest)

    def test_png_crc_truncation_and_trailing_bytes_are_rejected(self):
        valid = png()
        damaged = bytearray(valid)
        damaged[20] ^= 1
        for data in [bytes(damaged), valid[:-1], valid + b"unexpected"]:
            with self.subTest(length=len(data)):
                with self.assertRaises(ValueError):
                    png_size(data)

    def test_known_textures_are_required_with_exact_version(self):
        original = copy.deepcopy(self.data)
        for mutation in [lambda d: d.update(version=True), lambda d: d["textures"].pop(), lambda d: d["textures"][0].update(name="Unknown")]:
            self.data = copy.deepcopy(original)
            mutation(self.data)
            self.write()
            with self.assertRaises(ValueError):
                read_payloads(self.manifest)

    def test_chunk_size_and_png_integrity_are_checked_after_hash(self):
        first = self.data["textures"][0]
        part = self.root / first["chunks"][0]
        part.write_text("A" * (PART_CHARS + 4))
        with self.assertRaises(ValueError):
            read_payloads(self.manifest)
        # A delivery hash alone must not validate an arbitrary file disguised as a PNG.
        invalid = b"not a PNG"
        part.write_text(base64.b64encode(invalid).decode("ascii"))
        first.update(chunks=first["chunks"][:1], length=len(invalid), sha256=hashlib.sha256(invalid).hexdigest())
        self.write()
        with self.assertRaisesRegex(ValueError, "PNG"):
            read_payloads(self.manifest)


if __name__ == "__main__":
    unittest.main()
