# Prompt tạo quái vật Pixel Art — System Leveling

Tài liệu này khớp với art hiện có của project:

| Tham chiếu | Kích thước | PPU | Ghi chú |
|---|---|---|---|
| Player `Move.png` | 23×32 | 32 | cao đúng 1 unit |
| `Blue_Slime/Move.png` | 32×32 | 32 | 6 frame, không mắt, không tay chân |
| `Skeleton/Skeleton.png` | 32×48 | 48 | 9 frame walk, xương trắng ngà |
| `Slime_king/*` | boss | — | Move / Jump / Dash / Split |

Quy tắc chung của project: **mọi quái cao ~1 Unity unit**, nhìn nghiêng (3/4 top-down), chỉ lật `flipX`, outline 1px tối, palette trầm, không đổ bóng.

---

## 0. Khối style dùng chung (dán trước mọi prompt)

```text
pixel art sprite, top-down 3/4 view, side profile facing right, single creature centered,
full body, feet aligned to the bottom edge, 1-pixel dark outline, limited 16-color palette,
hard-edged pixels, no anti-aliasing, no blur, no gradients, no noise dithering,
flat 2–3 tone cel shading per material, no cast shadow, no ground,
flat solid magenta background (#FF00FF), game-ready 2D RPG monster asset,
dark fantasy dungeon crawler, muted colors, crisp pixel clusters,
no text, no watermark, no UI, no border
```

## 0b. Negative prompt (SD / ComfyUI / PixelLab)

```text
3d render, photorealistic, blender, smooth gradient, soft shading, anti-aliased edges, blurry,
painterly, watercolor, high resolution detail, drop shadow, bloom, glow, background scenery,
multiple characters, cropped, off-center, floating feet, extra limbs, deformed anatomy,
text, watermark, signature, UI, frame, border, jpeg artifacts
```

---

## 1. Nhện — Cave Weaver Spider

**Vai trò:** quái nhanh, yếu máu, đánh lén / bắn tơ. Palette tím–than, mắt đỏ.

### Sprite tĩnh (48×40, PPU 40)

```text
pixel art sprite of a dungeon cave spider, top-down 3/4 view, side profile facing right,
8 segmented legs in a low aggressive stance, bulbous abdomen with faint violet rune markings,
cluster of 4 glowing red eyes, bone-white chelicerae dripping green venom,
chitin plates in charcoal #241a2e and deep violet #5b3f7a, magenta rim highlights #b04fd1,
1-pixel dark outline, 16-color palette, hard pixels, no anti-aliasing, no shadow,
flat magenta background #FF00FF, 48x40 px canvas, body 40 px wide, feet on bottom edge,
game-ready RPG monster sprite, dark fantasy, no text
```

### Sprite sheet đi bộ (4 frame)

```text
same cave spider, 4-frame skitter walk cycle sprite sheet, one horizontal row,
4 equal cells of 48x40 px, no gaps, no labels, identical scale and baseline in every cell,
legs alternate in two groups of four, abdomen bobs 1 pixel, side profile facing right,
1-pixel dark outline, 16-color palette, hard pixels, flat magenta background #FF00FF
```

**Animation gợi ý:** Move 4f (12 fps) · Attack: nhấc 2 chân trước rồi đâm xuống · Hit: giật 1px, mắt nháy trắng · Die: chân co vào bụng, bụng xẹp.
**Elite:** Broodmother — bụng có túi trứng, thêm 2 chân, màu tím sáng hơn.

---

## 2. Skeleton — Bone Soldier

**Vai trò:** tank nhẹ, chậm, đòn đau. **Bám đúng style `Skeleton.png` hiện có** (xương trắng ngà, giáp da rách, kiếm gỉ).

### Sprite tĩnh (32×48, PPU 48)

```text
pixel art sprite of a skeleton dungeon soldier, top-down 3/4 view, side profile facing right,
32x48 px canvas, full body, cracked skull with hollow eye sockets, visible ribcage,
thin bone arms, tattered dark leather loincloth, rusty chipped short sword in the right hand,
bone white #e8e0c8 with warm grey shading #b9ab86, 1-pixel near-black outline,
12-color palette, hard pixels, no anti-aliasing, no shadow, flat magenta background #FF00FF,
feet on bottom edge, game-ready RPG monster sprite, dark fantasy, no text
```

> Nếu tool cho phép ảnh tham chiếu: upload `Skeleton.png` và thêm
> `match the exact pixel density, proportions and bone palette of the reference sprite`.

### Sprite sheet đi bộ (8 frame)

```text
same skeleton soldier, 8-frame walk cycle sprite sheet, one horizontal row, 8 equal cells 32x48 px,
identical scale, baseline and sword hand in every cell, side profile facing right,
loose undead gait, shoulder sway, skull bobs, 1-pixel outline, bone palette only,
hard pixels, flat magenta background #FF00FF
```

**Biến thể:** Archer (cung xương, mũ trùm) · Mage (áo choàng tím, quyền trượng, lửa lam trong hốc mắt) · Knight (elite: mũ giáp, khiên gỉ, giáp vai).
**Animation:** Walk 8f · Attack: vung kiếm 3f + 1f wind-up · Hit: sọ lệch 1px · Die: rụng đầu → xương đổ thành đống.

---

## 3. Chó sói — Shadow Wolf

**Vai trò:** áp sát cực nhanh, lao theo đường thẳng. Palette xám xanh, mắt hổ phách.

### Sprite tĩnh (48×32, PPU 32)

```text
pixel art sprite of a lean shadow wolf, top-down 3/4 view, side profile facing right,
48x32 px canvas, gaunt quadruped body with visible ribs, spiky dark blue-grey mane,
glowing amber eyes, long snout with white fangs, torn ears, low bushy tail,
fur palette charcoal #2b2f3d, slate #4a5163, highlight #8b93a8,
1-pixel dark outline, 12-color palette, hard pixels, no anti-aliasing, no shadow,
flat magenta background #FF00FF, paws on bottom edge, game-ready RPG monster sprite,
dark fantasy, no text
```

### Sprite sheet chạy (6 frame)

```text
same shadow wolf, 6-frame gallop run cycle sprite sheet, one horizontal row,
6 equal cells 48x32 px, identical scale and ground line, side profile facing right,
front and back legs in 2-beat gallop, mane and tail trailing, 1-pixel outline,
12-color palette, hard pixels, flat magenta background #FF00FF
```

**Animation:** Idle 4f (thở, đuôi phe phẩy) · Run 6f (14 fps) · Attack: hạ thấp người → lao 1 ô · Hit: lùi 1px, tai cụp · Die: khuỵu chân trước rồi đổ nghiêng.
**Elite:** Alpha — bờm xương gai, sẹo mắt, mắt đỏ.

---

## 4. Slime — Gel Blob

**Vai trò:** creep cơ bản, dễ farm. **Giữ đúng style `Blue_Slime`** (bóng, không mắt, không tay chân, highlight trắng góc trên trái).

### Sprite tĩnh (32×32, PPU 32)

```text
pixel art sprite of a glossy blue slime, top-down 3/4 view, 32x32 px canvas,
rounded gelatinous blob, flat pixel bands suggesting translucency,
bright cyan body #4fd3ff, deep blue core shadow #1d6fd6, white specular highlight top-left,
dark navy 1-pixel outline, 8-color palette, no eyes, no mouth, no limbs,
hard pixels, no anti-aliasing, no shadow, flat magenta background #FF00FF,
base flattened on the bottom edge, game-ready RPG monster sprite, no text
```

### Sprite sheet nhún (6 frame)

```text
same blue slime, 6-frame squash-and-stretch bounce sprite sheet, one horizontal row,
6 equal cells 32x32 px, identical scale, side view, body squashes wide then stretches tall,
highlight stays top-left, 1-pixel outline, 8-color palette, hard pixels,
flat magenta background #FF00FF
```

**Biến thể màu:** Acid (xanh lá, bong bóng, nhỏ giọt) · Magma (đỏ cam, vỏ nứt, glow trong) · Void (tím đen, đốm sao) · King (boss 64×64, vương miện vàng, đã có sprite).
**Animation:** Move 6f · Hit: xẹp dẹt 2f · Die: bắn thành vũng, vũng tan dần 4f.

---

## 5. Prompt "lineup" kiểm tra tỉ lệ (tuỳ chọn)

```text
character lineup sheet, 4 dark fantasy dungeon monsters standing side by side on one baseline:
cave spider, skeleton soldier, shadow wolf, blue slime, all same scale, same pixel density,
top-down 3/4 view facing right, 1-pixel outlines, muted 16-color palette,
flat magenta background #FF00FF, no labels, no text, game asset reference sheet
```

---

## 6. Stat gợi ý (khớp `EnemyScriptableObject`: moveSpeed / maxHealth / damage)

| Quái | Vai trò | moveSpeed | maxHealth | damage | Ghi chú code |
|---|---|---|---|---|---|
| Blue Slime | creep cơ bản | 2.0 | 10 | 4 | giữ nguyên |
| Spider | swarm / nhanh | 3.0 | 12 | 3 | cần slow hoặc bắn tơ |
| Shadow Wolf | lao tuyến | 3.6 | 18 | 7 | cần dash/telegraph |
| Skeleton | tank nhẹ | 1.8 | 25 | 6 | chậm, đòn nặng |
| Elite (x3 chỉ số) | mini-boss | +20% | ×3 | ×1.5 | thêm outline đỏ/aura |
| Boss (Slime King) | boss | 2.2 | ×6 | ×2 | đã có animation Split/Dash/Jump |

---

## 7. Cấu trúc file đề xuất

```text
Assets/MyAssets/Monster/Creep/Spider/{Move,Hit,Die}.png
Assets/MyAssets/Monster/Creep/Wolf/{Move,Hit,Die}.png
Assets/MyAssets/Monster/Creep/Skeleton/Skeleton.png      (đã có)
Assets/MyAssets/Monster/Creep/Blue_Slime/{Move,Hit,Die}.png (đã có)
Assets/MyAssets/Monster/Boss/Slime_king/...              (đã có)
Assets/Scriptable Object/Enemy/{Spider,Wolf,Skeleton}.asset
```

## 8. Import settings trong Unity

- Texture Type: **Sprite (2D and UI)**, Sprite Mode: **Multiple**
- Pivot: **Bottom Center** (để chân quái khớp mặt đất)
- **Pixels Per Unit = chiều cao frame** (32 → 32, 48 → 48) ⇒ mọi quái cao đúng 1 unit như player
- Filter Mode: **Point (no filter)**, Compression: **None**, Mip Maps: **off**
- Max Size ≥ 2048, Mesh Type: Full Rect

## 9. Mẹo thực dụng

1. **AI không ra đúng 32px.** Hãy generate ở 512–1024px rồi downscale **nearest-neighbor** về 32/48px, sau đó dọn tay trong Aseprite.
2. **Ưu tiên 1 con / 1 lần generate**, dùng chung khối style + seed cố định; lineup chỉ để check tỉ lệ.
3. **Nền magenta** dễ tách alpha hơn nền trắng/trong suốt (AI thường vẽ nền giả).
4. Nếu tool là **Midjourney**: thêm `--ar 1:1 --style raw --v 6 --no text, shadow, background`.
5. Nếu tool là **SD/PixelLab**: dùng tag ngắn `pixel art, 16 colors, 1px outline, side view, magenta bg`, guidance 6–8.
6. **Luôn giữ baseline chân cố định** giữa các frame, nếu không nhân vật sẽ "trôi" khi chạy animation.
