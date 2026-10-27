"""Limited pixel animation from the user's PNGs; never modifies the references.

Run: uv run --with pillow python Tools/generate_reference_monsters.py
Output: isolated Assets/MyAssets/Monster/ReferenceAnimated + GIF previews.
Transforms use nearest-neighbour sampling, not AI-generated replacement art.
"""
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/MyAssets/Monster/References"
OUTPUT = ROOT / "Assets/MyAssets/Monster/ReferenceAnimated"
PREVIEW = ROOT / "Captures/ReferenceAnimation"
SIZE = 64
NEAREST = Image.Resampling.NEAREST
STATES = {"Idle": (8, 8, True), "Move": (8, 12, True),
          "Hit": (4, 12, False), "Die": (8, 10, False)}
FILES = {"Slime": "Slime.png", "Skeleton": "Skeleton.png",
         "Wolf": "wolf.png", "Spider": "Spider.png"}


def blank():
    return Image.new("RGBA", (SIZE, SIZE))


def shift(image, x=0, y=0):
    out = blank()
    out.alpha_composite(image, (round(x), round(y)))
    return out


def resize_ground(image, sx=1, sy=1, floor=54):
    """Scale about a fixed ground anchor with whole pixels only."""
    w, h = max(1, round(SIZE * sx)), max(1, round(SIZE * sy))
    scaled = image.resize((w, h), NEAREST)
    out = blank()
    out.alpha_composite(scaled, (round((SIZE-w)/2), round(floor*(1-sy))))
    return out


def partition(source, definitions):
    """Exclusive masks preserve every reference pixel exactly at rest."""
    remaining = source.copy()
    parts = []
    for name, polygon, pivot in definitions:
        mask = Image.new("L", source.size)
        ImageDraw.Draw(mask).polygon(polygon, fill=255)
        part = remaining.copy()
        part.putalpha(ImageChops.multiply(remaining.getchannel("A"), mask))
        remaining.putalpha(ImageChops.subtract(remaining.getchannel("A"),
                                             part.getchannel("A")))
        layer = blank()
        layer.alpha_composite(part, (8, 8))
        parts.append((name, layer, (pivot[0]+8, pivot[1]+8)))
    body = blank()
    body.alpha_composite(remaining, (8, 8))
    # Disjoint masks must reproduce the exact original, including alpha.
    reconstruction = blank()
    for _, layer, _ in parts:
        reconstruction.alpha_composite(layer)
    reconstruction.alpha_composite(body)
    expected = blank()
    expected.alpha_composite(source, (8, 8))
    assert reconstruction.tobytes() == expected.tobytes(), "Lost reference pixels"
    return body, parts


def definitions(name):
    if name == "Skeleton":
        return [
            ("left_leg", [(18,31),(26,29),(27,39),(26,47),(17,47)], (25,31)),
            ("right_leg", [(28,30),(34,30),(41,47),(32,47),(28,38)], (30,31)),
            ("sword", [(0,12),(20,22),(26,23),(26,30),(19,36),(0,30)], (24,23)),
            ("hand", [(34,18),(44,23),(44,39),(34,39)], (34,20)),
            ("head", [(16,0),(36,0),(36,17),(19,18)], (27,17)),
        ]
    if name == "Wolf":
        return [
            ("rear_near", [(6,27),(15,23),(17,30),(11,38),(11,47),(3,47),(5,37)], (13,27)),
            ("rear_far", [(16,28),(24,28),(24,35),(28,45),(21,46),(16,38)], (21,29)),
            ("front_far", [(32,23),(38,24),(41,34),(39,39),(33,39),(35,34)], (34,26)),
            ("front_near", [(25,26),(32,26),(33,36),(37,44),(37,48),(28,48),(26,35)], (29,28)),
            ("tail", [(0,21),(8,20),(15,21),(12,27),(6,31),(0,31)], (13,23)),
        ]
    if name == "Spider":
        return [
            ("left_back", [(0,14),(17,14),(16,22),(10,27),(6,36),(0,36)], (15,20)),
            ("left_front", [(0,27),(12,24),(14,29),(8,34),(4,44),(0,44)], (12,28)),
            ("right_back", [(33,18),(47,23),(48,37),(44,37),(39,28),(32,24)], (33,23)),
            ("right_mid", [(29,25),(36,25),(43,33),(46,43),(42,43),(37,34),(29,29)], (30,27)),
            ("right_front", [(25,29),(31,28),(34,35),(34,47),(28,47),(28,36)], (28,31)),
        ]
    return []


def articulated(name, body, parts, phase, amplitude, death=0):
    out = blank()
    for i, (part_name, layer, pivot) in enumerate(parts):
        wave = math.sin(phase + (i % 2)*math.pi)
        angle, dx, dy = 0, 0, 0
        if name == "Skeleton":
            if "leg" in part_name:
                angle = wave * 10 * amplitude
            elif part_name == "sword":
                angle = math.sin(phase) * 4 * amplitude
            elif part_name == "hand":
                angle = -math.sin(phase) * 5 * amplitude
            elif part_name == "head":
                dy = -round(math.sin(phase)*amplitude*0.7)
            if death:
                angle += (-1 if i % 2 else 1)*death*48
                dx += (i-2)*death*3
                dy += death * (9 if part_name == "head" else 3)
        elif name == "Wolf":
            if part_name == "tail":
                angle = math.sin(phase)*8*amplitude
            else:
                angle = wave*13*amplitude
                dy = -max(0, round(math.cos(phase + i*math.pi)*amplitude))
        else:
            angle = wave*7*amplitude
            if death:
                angle += (-1 if part_name.startswith("left") else 1)*death*28
                dx = (-2 if part_name.startswith("right") else 2)*death
        moved = layer.rotate(angle, NEAREST, center=pivot)
        out.alpha_composite(shift(moved, dx, dy))
    out.alpha_composite(body)
    return out


def make_frames(name, source):
    original = blank()
    original.alpha_composite(source, (8, 8))
    body, parts = partition(source, definitions(name))
    result = {}
    for state, (count, _, _) in STATES.items():
        frames = []
        for index in range(count):
            phase = index/count*math.tau
            t = index/(count-1)
            if state in ("Idle", "Move"):
                moving = state == "Move"
                if name == "Slime":
                    wave = math.sin(phase)
                    amount = 0.10 if moving else 0.025
                    frame = resize_ground(original, 1+wave*amount, 1-wave*amount)
                    if moving:
                        frame = shift(frame, 0, -max(0, round(-wave*3)))
                else:
                    amount = 1 if moving else 0.4
                    frame = articulated(name, body, parts, phase, amount)
                    if moving:
                        frame = shift(frame, 0, -round((1-math.cos(phase*2))/2))
            elif state == "Hit":
                frame = shift(original, [-2, -1, 1, 0][index], [0, -1, 0, 0][index])
                if index == 0:
                    # Tinted impact frame; retains linework instead of a solid silhouette.
                    tint = Image.new("RGBA", frame.size, (255, 210, 210, 255))
                    tint.putalpha(frame.getchannel("A"))
                    frame = Image.blend(frame, tint, 0.45)
            else:
                if name == "Slime":
                    frame = resize_ground(original, 1+0.2*t, 1-0.84*t)
                elif name == "Skeleton":
                    frame = articulated(name, body, parts, 0, 0, t)
                    frame = resize_ground(frame, 1, 1-0.42*t)
                elif name == "Spider":
                    frame = articulated(name, body, parts, 0, 0, t)
                    frame = resize_ground(frame, 1, 1-0.3*t)
                else:
                    frame = original.rotate(-65*t, NEAREST, center=(32,32))
                    frame = shift(frame, 0, 2*t)
                # Last pose remains visible. Gameplay decides when to despawn.
                frame.putalpha(frame.getchannel("A").point(lambda a: round(a*(1-0.65*t))))
            assert frame.getbbox(), f"Empty frame: {name}/{state}/{index}"
            alpha = frame.getchannel("A")
            assert not alpha.crop((0,0,SIZE,1)).getbbox(), "Clipped top edge"
            assert not alpha.crop((0,SIZE-1,SIZE,SIZE)).getbbox(), "Clipped bottom edge"
            assert not alpha.crop((0,0,1,SIZE)).getbbox(), "Clipped left edge"
            assert not alpha.crop((SIZE-1,0,SIZE,SIZE)).getbbox(), "Clipped right edge"
            frames.append(frame)
        assert len({f.tobytes() for f in frames}) > 1, f"Static animation: {name}/{state}"
        result[state] = frames
    return result


def preview(all_frames):
    PREVIEW.mkdir(parents=True, exist_ok=True)
    # Rows = states, columns = monsters; plays finite Hit/Die then pauses/resets.
    cell = 64*3
    rendered = []
    for tick in range(48):
        canvas = Image.new("RGB", (cell*4, (cell+24)*4), (39,44,54))
        draw = ImageDraw.Draw(canvas)
        for col, (name, states) in enumerate(all_frames.items()):
            for row, (state, frames) in enumerate(states.items()):
                fps = STATES[state][1]
                index = int(tick*fps/12)
                index = index % len(frames) if STATES[state][2] else min(index % 24, len(frames)-1)
                draw.text((col*cell+8, row*(cell+24)+6), f"{name} / {state}", fill=(220,229,240))
                img = frames[index].resize((cell,cell), NEAREST)
                canvas.paste(img, (col*cell, row*(cell+24)+24), img)
        rendered.append(canvas)
    rendered[0].save(PREVIEW/"monsters_animated.gif", save_all=True,
                     append_images=rendered[1:], duration=80, loop=0, disposal=2)
    rendered[0].save(PREVIEW/"overview.png")
    for name, states in all_frames.items():
        for state, frames in states.items():
            images = []
            for frame in frames:
                bg = Image.new("RGB", (SIZE*4,SIZE*4), (39,44,54))
                large = frame.resize(bg.size, NEAREST)
                bg.paste(large, (0,0), large)
                images.append(bg)
            images[0].save(PREVIEW/f"{name}_{state}.gif", save_all=True,
                           append_images=images[1:], duration=round(1000/STATES[state][1]), loop=0)


def main():
    # Guard against accidentally targeting an existing user folder.
    marker = OUTPUT/"generated-manifest.json"
    if OUTPUT.exists() and not marker.exists() and any(OUTPUT.iterdir()):
        raise RuntimeError(f"Refusing to overwrite unowned directory {OUTPUT}")
    before = {f: hashlib.sha256((SOURCE/f).read_bytes()).hexdigest() for f in FILES.values()}
    OUTPUT.mkdir(parents=True, exist_ok=True)
    marker.write_text(json.dumps({"status":"generation-in-progress", "sources_sha256":before}),
                      encoding="utf-8")
    all_frames = {}
    for name, filename in FILES.items():
        source = Image.open(SOURCE/filename).convert("RGBA")
        assert source.size == (48,48), f"Unexpected reference size: {filename}"
        frames = make_frames(name, source)
        folder = OUTPUT/name
        folder.mkdir(parents=True, exist_ok=True)
        for state, sequence in frames.items():
            sheet = Image.new("RGBA", (SIZE*len(sequence), SIZE))
            for index, frame in enumerate(sequence):
                sheet.alpha_composite(frame, (SIZE*index,0))
            sheet.save(folder/f"{state}.png")
            reloaded = Image.open(folder/f"{state}.png")
            assert reloaded.size == (SIZE*STATES[state][0], SIZE)
            assert reloaded.getextrema()[3][0] == 0, "Missing transparency"
        all_frames[name] = frames
    preview(all_frames)
    after = {f: hashlib.sha256((SOURCE/f).read_bytes()).hexdigest() for f in FILES.values()}
    assert before == after, "Source files changed!"
    manifest = {"frame_size":SIZE, "pixels_per_unit":48,
                "pivot":[0.5,10/64], "sources_sha256":before,
                "states":{s:{"frames":v[0],"fps":v[1],"loop":v[2]} for s,v in STATES.items()},
                "monsters":list(FILES)}
    marker.write_text(json.dumps(manifest, indent=2)+"\n", encoding="utf-8")
    print("PASS: 16 RGBA sheets, 112 frames, no clipped edges; 4 reference hashes unchanged.")
    print(f"Preview: {PREVIEW/'monsters_animated.gif'}")


if __name__ == "__main__":
    main()
