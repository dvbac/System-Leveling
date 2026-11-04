"""Animate approved FX sprites; source art is never modified.

Run: uv run --with pillow python Tools/animate_frostfang_effects.py
Outputs separate PNG sheets, preview GIFs and frame/timing metadata.
"""
import hashlib
import json
import math
import random
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1] / "Captures/FrostfangFX"
SOURCE = ROOT / "Sprites"
OUTPUT = ROOT / "Animations"
PREVIEW = ROOT / "Preview"
NEAREST = Image.Resampling.NEAREST
SPECS = {
    "PounceImpact": ((128, 128), 8, 16, False),
    "FrostBreath": ((256, 128), 8, 16, True),
    "IcePillar": ((64, 128), 12, 12, False),
    "Blizzard": ((128, 128), 8, 8, True),
}


def blank(size):
    return Image.new("RGBA", size)


def fade(image, opacity):
    result = image.copy()
    result.putalpha(result.getchannel("A").point(lambda a: round(a * opacity)))
    return result


def impact(source):
    sectors = [blank(source.size) for _ in range(8)]
    for y in range(128):
        for x in range(128):
            if source.getpixel((x, y))[3]:
                angle = math.atan2((y - 64) * 2, x - 64)
                index = int((angle + math.pi) / math.tau * 8) % 8
                sectors[index].putpixel((x, y), source.getpixel((x, y)))
    scales = [0.22, 0.45, 0.7, 0.9, 0.9, 0.88, 0.85, 0.82]
    opacity = [0.9, 1, 1, 1, 0.85, 0.6, 0.35, 0.1]
    frames = []
    for frame, scale in enumerate(scales):
        out = blank(source.size)
        spread = max(0, frame - 3) * 2.5
        for index, sector in enumerate(sectors):
            angle = -math.pi + (index + 0.5) * math.tau / 8
            size = max(1, round(128 * scale))
            part = fade(sector.resize((size, size), NEAREST), opacity[frame])
            dx = math.cos(angle) * spread
            dy = math.sin(angle) * spread * 0.5 + max(0, frame - 3)
            out.alpha_composite(part, (round(64 - size / 2 + dx), round(64 - size / 2 + dy)))
        frames.append(out)
    return frames


def pieces(source, cell_w, cell_h):
    result = []
    for y in range(0, source.height, cell_h):
        for x in range(0, source.width, cell_w):
            part = source.crop((x, y, min(source.width, x + cell_w), min(source.height, y + cell_h)))
            box = part.getchannel("A").getbbox()
            if box:
                cropped = part.crop(box)
                if sum(a > 0 for a in cropped.getchannel("A").get_flattened_data()) >= 3:
                    result.append((cropped, x + box[0], y + box[1]))
    return result


def breath(source, count=8):
    # Advect source-art patches through a cone; births/fades wrap once per loop.
    particles = pieces(source, 28, 20)
    random.Random(71).shuffle(particles)
    frames = []
    clip = Image.new("L", source.size)
    draw = ImageDraw.Draw(clip)
    draw.polygon([(8, 64), (247, 5), (247, 123)], fill=255)
    for frame in range(count):
        out = blank(source.size)
        for index, (part, x, y) in enumerate(particles):
            cx, cy = x + part.width / 2, y + part.height / 2
            phase = ((index + 0.5) / len(particles) + frame / 8) % 1
            tx = 8 + 232 * phase
            slope = max(-0.23, min(0.23, (cy - 64) / max(12, cx - 8)))
            ty = 64 + slope * (tx - 8)
            scale = 0.45 + 0.55 * phase
            moving = part.resize((max(1, round(part.width * scale)), max(1, round(part.height * scale))), NEAREST)
            opacity = min(1, phase / 0.08, (1 - phase) / 0.12)
            moving = fade(moving, max(0, opacity))
            px = max(3, min(source.width - moving.width - 3, round(tx - moving.width / 2)))
            py = max(3, min(source.height - moving.height - 3, round(ty - moving.height / 2)))
            out.alpha_composite(moving, (px, py))
        out.putalpha(ImageChops.multiply(out.getchannel("A"), clip))
        ImageDraw.Draw(out).polygon([(8, 64), (17, 62), (17, 66)], fill=(196, 240, 246, 255))
        frames.append(out)
    return frames


def pillar(source):
    frames = []
    floor = source.getchannel("A").getbbox()[3]
    # Reveal upward while keeping the original base anchored, not scaling the pillar.
    for height in (10, 28, 54, 86, 128):
        out = blank(source.size)
        top = max(0, floor - height)
        out.alpha_composite(source.crop((0, top, 64, floor)), (0, top))
        frames.append(out)
    frames.extend([source.copy() for _ in range(3)])
    cracked = source.copy()
    cracks = Image.new("RGBA", source.size)
    draw = ImageDraw.Draw(cracks)
    draw.line([(29, 15), (34, 39), (25, 58), (36, 82), (30, 112)], fill=(42, 75, 99, 255), width=1)
    draw.line([(34, 39), (47, 46)], fill=(42, 75, 99, 255), width=1)
    draw.line([(25, 58), (15, 69)], fill=(42, 75, 99, 255), width=1)
    cracks.putalpha(ImageChops.multiply(cracks.getchannel("A"), source.getchannel("A")))
    cracked.alpha_composite(cracks)
    frames.append(cracked)
    shards = pieces(cracked, 8, 16)
    rng = random.Random(27)
    velocities = [(rng.uniform(-8, 8), rng.uniform(-13, -3), rng.uniform(-18, 18)) for _ in shards]
    for t in (1 / 3, 2 / 3, 1):
        out = blank(source.size)
        for (part, x, y), (vx, vy, angle) in zip(shards, velocities):
            rotated = part.rotate(angle * t, NEAREST, expand=True)
            rotated = fade(rotated, 1 - t)
            px = round(x + (part.width - rotated.width) / 2 + vx * t)
            py = round(y + (part.height - rotated.height) / 2 + vy * t + 100 * t * t)
            px = max(2, min(source.width - rotated.width - 2, px))
            py = max(2, min(floor - rotated.height, py))
            out.alpha_composite(rotated, (px, py))
        frames.append(out)
    return frames


def snow(source):
    return [ImageChops.offset(source, frame * 16, frame * 16) for frame in range(8)]


def durations(count, fps):
    # GIF delays have 10ms precision; distribute rounding to preserve total duration.
    ticks = [round(i * 100 / fps) for i in range(count + 1)]
    return [(ticks[i + 1] - ticks[i]) * 10 for i in range(count)]


def preview_frame(frame, size=None):
    if size:
        frame = frame.resize(size, NEAREST)
    result = Image.new("RGB", frame.size, (35, 45, 60))
    result.paste(frame, (0, 0), frame)
    return result


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    before = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in SOURCE.glob("*.png")}
    build = {"PounceImpact": impact, "FrostBreath": breath, "IcePillar": pillar, "Blizzard": snow}
    animations = {}
    manifest = {"source_sha256": before, "animations": {}}
    for name, (size, count, fps, loop) in SPECS.items():
        source = Image.open(SOURCE / (name + ".png")).convert("RGBA")
        assert source.size == size
        frames = build[name](source)
        assert len(frames) == count and all(f.size == size for f in frames)
        assert len({f.tobytes() for f in frames}) >= min(count, 6), "Static or duplicated animation: " + name
        assert all(f.getchannel("A").getextrema()[0] == 0 for f in frames), "Missing transparency"
        sheet = blank((size[0] * count, size[1]))
        for index, frame in enumerate(frames):
            sheet.paste(frame, (index * size[0], 0))
        sheet.save(OUTPUT / (name + ".png"))
        with Image.open(OUTPUT / (name + ".png")) as saved:
            for index, frame in enumerate(frames):
                assert saved.crop((index * size[0], 0, (index + 1) * size[0], size[1])).tobytes() == frame.tobytes()
        gif_frames = [preview_frame(frame, (size[0] * 3, size[1] * 3)) for frame in frames]
        gif_path = PREVIEW / (name + ".gif")
        gif_frames[0].save(gif_path, save_all=True, append_images=gif_frames[1:], duration=durations(count, fps), loop=0, disposal=2)
        with Image.open(gif_path) as gif:
            total = 0
            for i in range(gif.n_frames):
                gif.seek(i)
                total += gif.info["duration"]
            assert total == sum(durations(count, fps)), "Wrong preview timing"
        animations[name] = frames
        manifest["animations"][name] = {"frames": count, "cell_size": list(size), "fps": fps,
                                          "duration_seconds": count / fps, "loop": loop,
                                          "layout": "one horizontal row, left to right"}
        print(f"PASS {name}: {count} frames, {fps} FPS, {count / fps:g}s, RGBA")
    assert snow(Image.open(SOURCE / "Blizzard.png").convert("RGBA"))[0].tobytes() == ImageChops.offset(
        Image.open(SOURCE / "Blizzard.png").convert("RGBA"), 128, 128).tobytes(), "Snow wrap mismatch"
    assert breath(Image.open(SOURCE / "FrostBreath.png").convert("RGBA"), 9)[8].tobytes() == animations["FrostBreath"][0].tobytes(), "Breath wrap mismatch"
    assert all(hashlib.sha256((SOURCE / name).read_bytes()).hexdigest() == digest for name, digest in before.items())
    manifest["animations"]["IcePillar"]["phases_zero_based"] = {"grow": [0, 4], "hold": [5, 7], "shatter": [8, 11]}
    manifest["note"] = "Preview GIFs repeat for viewing. Only loop=True clips should loop in gameplay. Particle motion is derived from the approved still images; no boss scripts or hitboxes changed."
    (OUTPUT / "animation-index.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 18)
    except OSError:
        font = ImageFont.load_default()
    gallery = []
    for tick in range(48):
        canvas = Image.new("RGB", (1088, 640), (30, 38, 49))
        draw = ImageDraw.Draw(canvas)
        for slot, (name, frames) in enumerate(animations.items()):
            size, count, fps, loop = SPECS[name]
            time = tick / 16
            cycle = count / fps if loop else count / fps + 0.5
            frame = frames[min(count - 1, int((time % cycle) * fps))]
            x, y = (slot % 2) * 544, (slot // 2) * 320
            draw.rectangle((x + 8, y + 8, x + 535, y + 310), fill=(35, 45, 60))
            draw.text((x + 18, y + 15), f"{name} | {count} frames | {fps} FPS", font=font, fill=(229, 240, 250))
            enlarged = frame.resize((size[0] * 2, size[1] * 2), NEAREST)
            canvas.paste(enlarged, (x + (544 - enlarged.width) // 2, y + 52), enlarged)
        gallery.append(canvas)
    gallery[0].save(PREVIEW / "animations.gif", save_all=True, append_images=gallery[1:], duration=[60, 60, 60, 70] * 12, loop=0, disposal=2)
    print("PASS: 4 original sprites unchanged; sheet extraction and GIF durations verified.")


if __name__ == "__main__":
    main()
