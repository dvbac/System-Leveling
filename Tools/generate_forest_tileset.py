"""Run with: uv run --with pillow python Tools/generate_forest_tileset.py.

Writes a separate trial atlas and a map preview, never edits scenes or old assets.
"""
import json
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/MyAssets/Tilesets/ForestTrial"
PREVIEW = ROOT / "Captures/ForestTileset"
PALETTES = {
    "grass": ["#617d42", "#658247", "#58733c", "#6e894a", "#77934f"],
    "dirt": ["#997451", "#9e7955", "#8d684a", "#ad8660", "#ba956b"],
    "stone": ["#7e8580", "#858c85", "#68736e", "#959a8f", "#a3a696"],
    "moss": ["#405f40", "#466747", "#385439", "#50724b", "#608252"],
}
RGB = {name: [tuple(bytes.fromhex(c[1:])) for c in palette]
       for name, palette in PALETTES.items()}


def texture(material, variant):
    rng = random.Random(4400 + variant)
    tile = Image.new("RGB", (32, 32), RGB[material][0])
    draw = ImageDraw.Draw(tile)
    for _ in range(48):
        x, y = rng.randrange(2, 29), rng.randrange(2, 29)
        color = RGB[material][rng.randrange(1, 5)]
        if material in ("grass", "moss"):
            draw.line((x, y, x, y-1), fill=color)
            if rng.random() < 0.35:
                draw.point((x+1, y-2), fill=color)
        else:
            draw.line((x, y, x+rng.randrange(1, 3), y), fill=color)
    if material == "stone":
        # A staggered masonry pattern with consistent boundaries between tiles.
        draw.line((0, 0, 31, 0), fill=RGB[material][2])
        draw.line((0, 16, 31, 16), fill=RGB[material][2])
        draw.line((12, 0, 12, 15), fill=RGB[material][2])
        draw.line((28, 16, 28, 31), fill=RGB[material][2])
        draw.line((0, 1, 31, 1), fill=RGB[material][3])
        draw.line((0, 17, 31, 17), fill=RGB[material][3])
    return tile


def terrain_mask(bits):
    """Corner bits: NW=1 NE=2 SE=4 SW=8; edge samples match exactly."""
    mask = Image.new("L", (32, 32))
    nw, ne, se, sw = [bool(bits & b) for b in (1, 2, 4, 8)]
    for y in range(32):
        v = y/31
        for x in range(32):
            u = x/31
            value = nw*(1-u)*(1-v) + ne*u*(1-v) + se*u*v + sw*(1-u)*v
            # Interior-only perturbation cannot change shared edges.
            value += 0.06*math.sin(u*math.pi)*math.sin(v*math.pi)*math.sin((u+v)*12)
            mask.putpixel((x, y), 255 if value >= 0.5 else 0)
    return mask


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    bases = {name: [texture(name, i) for i in range(16)] for name in RGB}
    atlas = Image.new("RGB", (512, 512))
    for y in range(16):
        for x in range(16):
            atlas.paste(bases["grass"][(x+y)%16], (x*32, y*32))
    catalog = []
    for row, material in enumerate(RGB):
        for variant, tile in enumerate(bases[material]):
            atlas.paste(tile, (variant*32, row*32))
            catalog.append({"material": material, "variant": variant,
                            "cell_top_left": [variant, row]})
    masks = [terrain_mask(bits) for bits in range(16)]
    transitions = {}
    for row, material in enumerate(("dirt", "stone", "moss"), start=4):
        for bits, mask in enumerate(masks):
            tile = Image.composite(bases[material][0], bases["grass"][0], mask)
            transitions[material, bits] = tile
            atlas.paste(tile, (bits*32, row*32))
            catalog.append({"material": material, "corner_mask": bits,
                            "cell_top_left": [bits, row]})
    atlas.save(OUT/"Forest_32x32.png")
    (OUT/"tile-index.json").write_text(json.dumps({
        "tile_size": 32, "sheet_size": [512, 512], "columns": 16,
        "corner_bits": {"NW": 1, "NE": 2, "SE": 4, "SW": 8},
        "note": "Rows 0-3: 16 variants each of grass/dirt/stone/moss. Rows 4-6: 16 corner transitions each. Rows 7-15: grass filler. Coordinates start at top-left. Stone/moss transition to grass only.",
        "tiles": catalog}, indent=2)+"\n", encoding="utf-8")

    # Verify every possible neighbor edge against the matching corner codes.
    for a in range(16):
        for b in range(16):
            if bool(a & 2) == bool(b & 1) and bool(a & 4) == bool(b & 8):
                assert [masks[a].getpixel((31,y)) for y in range(32)] == [masks[b].getpixel((0,y)) for y in range(32)]
            if bool(a & 8) == bool(b & 1) and bool(a & 4) == bool(b & 2):
                assert [masks[a].getpixel((x,31)) for x in range(32)] == [masks[b].getpixel((x,0)) for x in range(32)]

    def vertex(x, y):
        if 15 <= x <= 22 and 5 <= y <= 12:
            return "stone"
        if ((x-5)**2/14 + (y-5)**2/6 < 1 or
                (x-9)**2/10 + (y-13)**2/4 < 1):
            return "moss"
        if abs(y-(9+1.5*math.sin(x/4))) < 1.5 or (17 <= x <= 19 and 0 <= y <= 5):
            return "dirt"
        return "grass"

    demo = Image.new("RGB", (26*32, 18*32))
    rng = random.Random(42)
    for y in range(18):
        for x in range(26):
            corners = [vertex(x,y), vertex(x+1,y), vertex(x+1,y+1), vertex(x,y+1)]
            material = next((m for m in ("stone", "moss", "dirt") if m in corners), "grass")
            if material == "grass":
                tile = bases[material][rng.randrange(16)]
            else:
                bits = sum(1 << i for i,c in enumerate(corners) if c == material)
                tile = bases[material][rng.randrange(16)] if bits == 15 else transitions[material,bits]
            demo.paste(tile, (x*32, y*32))
    demo.save(PREVIEW/"forest-map-preview.png")
    atlas.resize((1024,1024), Image.Resampling.NEAREST).save(PREVIEW/"tileset-x2.png")
    check = Image.open(OUT/"Forest_32x32.png")
    assert check.size == (512,512) and len(catalog) == 112
    print("PASS: 512x512 atlas; 32x32 tiles; 64 base variants + 48 corner transitions; all compatible mask edges match.")


if __name__ == "__main__":
    main()
