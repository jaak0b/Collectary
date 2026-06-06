"""Adds the Stop and Pause media glyphs to the embedded CollectaryIcons icon font.

The font is a frozen subset of Microsoft's FluentSystemIcons whose original
subsetting step is not reproducible in-repo (the source Fluent version has since
shifted its codepoints). Stop and Pause are simple geometric shapes, so rather
than re-subset the whole font we draw them directly at Fluent's filled codepoints
(Pause U+ED26, Stop U+F146) to match the surrounding iconography.

Idempotent: re-running overwrites the two glyphs and leaves everything else intact.

    python tools/add_media_glyphs.py
"""

from fontTools.ttLib import TTFont
from fontTools.pens.ttGlyphPen import TTGlyphPen

FONT = "src/Collectary.UI/Assets/Fonts/CollectaryIcons.ttf"
ADVANCE = 500

GLYPHS = {
    0xF146: ("uniF146", [[(55, 430), (445, 430), (445, 40), (55, 40)]]),
    0xED26: ("uniED26", [
        [(105, 430), (205, 430), (205, 40), (105, 40)],
        [(295, 430), (395, 430), (395, 40), (295, 40)],
    ]),
}


def build_glyph(contours, glyf):
    pen = TTGlyphPen(None)
    for contour in contours:
        pen.moveTo(contour[0])
        for point in contour[1:]:
            pen.lineTo(point)
        pen.closePath()
    glyph = pen.glyph()
    glyph.recalcBounds(glyf)
    return glyph


def main():
    font = TTFont(FONT)
    glyf = font["glyf"]
    hmtx = font["hmtx"]

    order = font.getGlyphOrder()
    for codepoint, (name, contours) in GLYPHS.items():
        if name not in order:
            order = order + [name]
        glyf[name] = build_glyph(contours, glyf)
        hmtx[name] = (ADVANCE, glyf[name].xMin)
        for table in font["cmap"].tables:
            table.cmap[codepoint] = name

    font.setGlyphOrder(order)
    font["maxp"].numGlyphs = len(order)
    font.save(FONT)
    print(f"Wrote {len(GLYPHS)} glyphs; font now has {len(order)} glyphs.")


if __name__ == "__main__":
    main()
