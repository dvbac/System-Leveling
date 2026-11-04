"""Sparkle the original card without moving or replacing its pixels.

Run: uv run --with pillow python Tools/animate_red_card.py
"""
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/MyAssets/Other/Drop/RedCard.png"
OUTPUT = ROOT / "Captures/RedCard/Animations"
PREVIEW = ROOT / "Captures/RedCard/Preview"
COUNT, FPS = 8, 8


def mix(color, target, amount):
    return tuple(round(a + (b - a) * amount) for a, b in zip(color, target))


def render(source, frame):
    phase = (frame % COUNT) / COUNT
    result = source.copy()
    left, top, right, bottom = source.getchannel("A").getbbox()
    span = right - left + (bottom - top) * 0.7
    sweep = -0.3 + phase * 1.6
    for y in range(top, bottom):
        for x in range(left, right):
            r, g, b, alpha = source.getpixel((x, y))
            if not alpha:
                continue
            position = (x - left + (y - top) * 0.7) / span
            highlight = max(0, 1 - abs(position - sweep) / 0.075)
            amount = highlight * 0.68
            color = mix((r, g, b), (255, 235, 180), amount)
            result.putpixel((x, y), (*color, alpha))

    sparkle = Image.new("RGBA", source.size)
    draw = ImageDraw.Draw(sparkle)
    points = [(left, top + 1, 0), (right - 1, top + 10, 0.3),
              (right - 2, bottom - 2, 0.58), (left + 1, bottom - 9, 0.8)]
    for x, y, offset in points:
        glow = max(0, math.cos(math.tau * (phase - offset))) ** 4
        if glow < 0.08:
            continue
        alpha = round(glow * 255)
        radius = 2 if glow > 0.55 else 1
        draw.line((x - radius, y, x + radius, y), fill=(255, 215, 113, alpha))
        draw.line((x, y - radius, x, y + radius), fill=(255, 215, 113, alpha))
        draw.point((x, y), fill=(255, 255, 232, alpha))
        if glow > 0.85:
            draw.point((x - 3, y - 3), fill=(255, 221, 142, round(alpha * 0.65)))
    result.alpha_composite(sparkle)
    return result


def background(frame, scale):
    result = Image.new("RGB", frame.size, (31, 35, 48))
    result.paste(frame, (0, 0), frame)
    return result.resize((frame.width * scale, frame.height * scale), Image.Resampling.NEAREST)


def main():
    original = SOURCE.read_bytes()
    source = Image.open(SOURCE).convert("RGBA")
    assert source.size == (64, 64), "Expected the supplied 64x64 card"
    assert source.getchannel("A").getbbox(), "Source card is empty"
    OUTPUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    frames = [render(source, i) for i in range(COUNT)]
    sheet = Image.new("RGBA", (source.width * COUNT, source.height))
    for i, frame in enumerate(frames):
        sheet.paste(frame, (i * source.width, 0))
        box = frame.getchannel("A").getbbox()
        assert box and box[0] > 0 and box[1] > 0 and box[2] < 64 and box[3] < 64
        for y in range(source.height):
            for x in range(source.width):
                if source.getpixel((x, y))[3]:
                    assert frame.getpixel((x, y))[3] == source.getpixel((x, y))[3], "Card silhouette changed"
    assert len({f.tobytes() for f in frames}) == COUNT, "Animation contains repeated frames"
    assert render(source, COUNT).tobytes() == frames[0].tobytes(), "Loop does not wrap"
    path = OUTPUT / "RedCardSparkle.png"
    sheet.save(path)
    with Image.open(path) as saved:
        for i, frame in enumerate(frames):
            assert saved.crop((i * 64, 0, (i + 1) * 64, 64)).tobytes() == frame.tobytes()

    # GIF delays use 10ms units: alternate 120/130ms, totaling exactly 1s.
    delays = [120, 130] * 4
    previews = [background(frame, 8) for frame in frames]
    gif_path = PREVIEW / "RedCardSparkle.gif"
    previews[0].save(gif_path, save_all=True, append_images=previews[1:], duration=delays, loop=0, disposal=2)
    with Image.open(gif_path) as gif:
        assert gif.n_frames == COUNT
        total = 0
        for i in range(gif.n_frames):
            gif.seek(i)
            total += gif.info["duration"]
        assert total == 1000
    assert SOURCE.read_bytes() == original, "Original card was modified"
    metadata = {"source": str(SOURCE.relative_to(ROOT)).replace("\\", "/"),
                "source_sha256": hashlib.sha256(original).hexdigest(),
                "sheet": "RedCardSparkle.png", "layout": "horizontal, left to right",
                "cell_size": [64, 64], "frames": COUNT, "fps": FPS,
                "duration_seconds": 1, "loop": True,
                "checks": "PASS: original unchanged, silhouette fixed, 8 unique frames, no clipping, exact extraction and GIF timing"}
    (OUTPUT / "animation-index.json").write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8")
    print("PASS: 8 unique RGBA frames, 64x64 cells, 8 FPS, 1s loop; source unchanged.")


if __name__ == "__main__":
    main()
