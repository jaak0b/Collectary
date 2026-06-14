"""Render the Collectary card-stack icon (see collectary-icon.svg) to every raster
target the apps need: Windows .ico, Avalonia and Android PNGs, and the web favicon.

Re-run after editing the design here so the SVG and the rasters stay in sync:
    python branding/generate_icons.py <output-dir>

The geometry mirrors collectary-icon.svg on a 120-unit canvas. Shapes are drawn on
transparent overlays and alpha-composited so the translucent cards blend over the blue,
then super-sampled 4x and downscaled with Lanczos for clean edges.
"""

import io
import struct
import sys
from pathlib import Path
from PIL import Image, ImageDraw

BLUE = (30, 102, 245)
ORANGE = (254, 100, 11)
WHITE = (255, 255, 255)
SUPERSAMPLE = 4


def draw_icon(size, detail):
    big = size * SUPERSAMPLE
    unit = big / 120.0
    canvas = Image.new("RGBA", (big, big), (0, 0, 0, 0))

    def rect(x, y, w, h, radius, color):
        nonlocal canvas
        overlay = Image.new("RGBA", (big, big), (0, 0, 0, 0))
        ImageDraw.Draw(overlay).rounded_rectangle(
            [x * unit, y * unit, (x + w) * unit, (y + h) * unit],
            radius=radius * unit, fill=color)
        canvas = Image.alpha_composite(canvas, overlay)

    rect(0, 0, 120, 120, 26, BLUE + (255,))
    rect(24, 32, 58, 42, 6, WHITE + (115,))
    rect(31, 39, 58, 42, 6, WHITE + (184,))
    rect(38, 46, 58, 42, 6, WHITE + (255,))

    if detail:
        rect(45, 53, 15, 15, 3, ORANGE + (235,))
        rect(64, 54, 24, 4, 2, BLUE + (51,))
        rect(64, 61, 18, 4, 2, BLUE + (51,))
        rect(45, 74, 44, 4, 2, BLUE + (36,))

    return canvas.resize((size, size), Image.LANCZOS)


def save_ico(path, sizes, detail_from):
    frames = []
    for size in sizes:
        buffer = io.BytesIO()
        draw_icon(size, size >= detail_from).save(buffer, format="PNG")
        frames.append((size, buffer.getvalue()))

    offset = 6 + 16 * len(frames)
    directory = b""
    payload = b""
    for size, png in frames:
        dimension = 0 if size >= 256 else size
        directory += struct.pack("<BBBBHHII", dimension, dimension, 0, 0, 1, 32, len(png), offset)
        payload += png
        offset += len(png)

    path.write_bytes(struct.pack("<HHH", 0, 1, len(frames)) + directory + payload)


def main(repo_root=None):
    root = Path(repo_root) if repo_root else Path(__file__).resolve().parent.parent
    ui_assets = root / "src" / "Collectary.UI" / "Assets"
    docs_assets = root / "docs-src" / "assets"
    docs_assets.mkdir(parents=True, exist_ok=True)

    save_ico(ui_assets / "collectary.ico", [16, 24, 32, 48, 64, 128, 256], detail_from=48)
    draw_icon(256, True).save(ui_assets / "Icon.png")
    draw_icon(512, True).save(root / "src" / "Collectary.UI.Android" / "Icon.png")
    save_ico(root / "src" / "Collectary.UI.Browser" / "wwwroot" / "favicon.ico", [16, 32, 48], detail_from=999)
    save_ico(docs_assets / "favicon.ico", [16, 32, 48], detail_from=999)

    print(f"Wrote Collectary icons under {root.resolve()}")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else None)
