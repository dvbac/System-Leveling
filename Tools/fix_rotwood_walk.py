"""Stabilize the usable half-stride; export a separate reversible 8-frame loop.

Run: uv run --with pillow python Tools/fix_rotwood_walk.py
shortcut: reuses the stable half-stride in reverse, redraw the opposite stride for a full gait.
"""
import hashlib
import json
import re
import uuid
from pathlib import Path

from PIL import Image, ImageChops, ImageStat, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/MyAssets/Monster/Boss/Rotwood/Walk.png"
OUTPUT = SOURCE.with_name("Walk_Loop.png")
PREVIEW = ROOT / "Captures/RotwoodWalk"
ORDER = [0, 1, 2, 3, 4, 3, 2, 1]


def core_center(frame):
    points = [(x, y) for y in range(128) for x in range(128)
              if (lambda c: c[3] > 128 and c[0] > 150 and 45 < c[1] < 185 and c[2] < 65)
              (frame.getpixel((x, y))) ]
    assert points, "Missing amber core"
    return tuple(sum(p[i] for p in points)/len(points) for i in (0, 1))


def difference(a, b):
    return sum(ImageStat.Stat(ImageChops.difference(a, b)).sum)


def main():
    original_bytes = SOURCE.read_bytes()
    source = Image.open(SOURCE).convert("RGBA")
    assert source.size == (384, 384)
    original = [source.crop((i%3*128, i//3*128, i%3*128+128, i//3*128+128))
                for i in range(8)]
    centers = [core_center(f) for f in original]
    frames = []
    offsets = []
    for index in ORDER:
        # Remove the source's large core drift while allowing one pixel of body bob.
        x, y = centers[index]
        dx = round(centers[0][0]-x)
        dy = round(centers[0][1]-y) + (1 if index in (1, 2, 3) else 0)
        out = Image.new("RGBA", (128, 128))
        out.alpha_composite(original[index], (dx, dy))
        assert out.getchannel("A").getbbox(), "Empty output"
        assert sum(out.getchannel("A").getdata()) == sum(original[index].getchannel("A").getdata()), "Clipped pixels"
        frames.append(out)
        offsets.append([dx, dy])
    # Keep the 3x3 layout of the original, but use fixed full-cell slices.
    sheet = Image.new("RGBA", (384, 384))
    for i, frame in enumerate(frames):
        sheet.alpha_composite(frame, (i%3*128, i//3*128))
    sheet.save(OUTPUT)

    meta_path = Path(str(OUTPUT)+".meta")
    template = Path(str(SOURCE)+".meta").read_text(encoding="utf-8")
    existing = meta_path.read_text(encoding="utf-8") if meta_path.exists() else ""
    guid = re.search(r"^guid: (\w+)", existing, re.M)
    template = re.sub(r"^guid: \w+", "guid: "+(guid[1] if guid else uuid.uuid4().hex), template, count=1, flags=re.M)
    template = template.replace("Walk_", "Walk_Loop_")
    for i in range(8):
        pattern = (rf"(      name: Walk_Loop_{i}\n      rect:\n        serializedVersion: 2\n)"
                   r"        x: [^\n]+\n        y: [^\n]+\n        width: [^\n]+\n        height: [^\n]+")
        rect = f"        x: {i%3*128}\n        y: {256-i//3*128}\n        width: 128\n        height: 128"
        template, matches = re.subn(pattern, lambda m: m[1]+rect, template)
        assert matches == 1, f"Missing sprite metadata {i}"
    meta_path.write_text(template, encoding="utf-8")

    PREVIEW.mkdir(parents=True, exist_ok=True)
    comparison = []
    for before, after in zip(original, frames):
        canvas = Image.new("RGB", (768, 410), (36, 40, 49))
        draw = ImageDraw.Draw(canvas)
        draw.text((12, 8), "BEFORE", fill="white")
        draw.text((396, 8), "AFTER - 8 FPS / 1s", fill="white")
        for x, frame in ((0,before),(384,after)):
            enlarged = frame.resize((384,384), Image.Resampling.NEAREST)
            canvas.paste(enlarged, (x,26), enlarged)
        comparison.append(canvas)
    # GIF timing is quantized to 10ms: alternate 120/130ms for exactly 1000ms.
    comparison[0].save(PREVIEW/"before-after.gif", save_all=True,
                       append_images=comparison[1:], duration=[120,130]*4, loop=0, disposal=2)
    comparison[0].save(PREVIEW/"before-after.png")
    assert SOURCE.read_bytes() == original_bytes, "Original modified"
    assert Image.open(OUTPUT).size == source.size
    assert difference(frames[-1],frames[0]) == difference(frames[0],frames[1]), "Uneven wrap boundary"
    assert difference(frames[-1],frames[0]) < difference(original[-1],original[0]), "Wrap did not improve"
    with Image.open(PREVIEW/"before-after.gif") as gif:
        assert gif.n_frames == 8
        duration = 0
        for i in range(8):
            gif.seek(i)
            duration += gif.info["duration"]
        assert duration == 1000
    report = {"source_sha256":hashlib.sha256(original_bytes).hexdigest(), "source_unchanged":True,
              "frame_order":ORDER, "offsets":offsets, "frame_size":[128,128], "fps":8,
              "wrap_difference_before":difference(original[-1],original[0]),
              "wrap_difference_after":difference(frames[-1],frames[0]),
              "limitation":"Reversible half-stride; not eight newly drawn gait poses."}
    (PREVIEW/"checks.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))


if __name__ == "__main__":
    main()
