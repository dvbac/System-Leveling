using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sinh sprite pixel-art cho quái (Nhện, Sói) trực tiếp trong project.
/// Chạy: Tools/System Leveling/Generate Monster Sprites.
/// </summary>
public static class MonsterSpriteGenerator
{
    private const string CreepRoot = "Assets/MyAssets/Monster/Creep";

    // Palette Nhện
    private static readonly Color32 SpiderOutline = new Color32(20, 14, 26, 255);
    private static readonly Color32 SpiderBase = new Color32(58, 43, 74, 255);
    private static readonly Color32 SpiderLight = new Color32(107, 79, 138, 255);
    private static readonly Color32 SpiderDark = new Color32(36, 26, 46, 255);
    private static readonly Color32 SpiderAccent = new Color32(176, 79, 209, 255);
    private static readonly Color32 SpiderEye = new Color32(255, 59, 59, 255);
    private static readonly Color32 SpiderFang = new Color32(232, 224, 200, 255);
    private static readonly Color32 Venom = new Color32(124, 220, 96, 255);

    // Palette Sói
    private static readonly Color32 WolfOutline = new Color32(18, 20, 28, 255);
    private static readonly Color32 WolfBase = new Color32(58, 64, 83, 255);
    private static readonly Color32 WolfLight = new Color32(74, 81, 99, 255);
    private static readonly Color32 WolfHighlight = new Color32(139, 147, 168, 255);
    private static readonly Color32 WolfDark = new Color32(36, 40, 54, 255);
    private static readonly Color32 WolfEye = new Color32(255, 179, 71, 255);
    private static readonly Color32 WolfFang = new Color32(238, 234, 220, 255);
    private static readonly Color32 WolfTongue = new Color32(196, 88, 96, 255);

    [MenuItem("Tools/System Leveling/Generate Monster Sprites")]
    public static void Generate()
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Generate Monster Sprites only outside Play Mode.");

        GenerateSpider();
        GenerateWolf();

        AssetDatabase.Refresh();
        Debug.Log("[MonsterSpriteGenerator] Đã sinh sprite Nhện + Sói (Move / Hit / Die).");
    }

    [MenuItem("Tools/System Leveling/Monster Sprite Previews (x6)")]
    public static void MakePreviews()
    {
        Color32 bg = new Color32(42, 47, 56, 255);
        Preview($"{CreepRoot}/Spider/Move.png", 6, bg, "Spider_Move");
        Preview($"{CreepRoot}/Spider/Hit.png", 6, bg, "Spider_Hit");
        Preview($"{CreepRoot}/Spider/Die.png", 6, bg, "Spider_Die");
        Preview($"{CreepRoot}/Wolf/Move.png", 6, bg, "Wolf_Move");
        Preview($"{CreepRoot}/Wolf/Hit.png", 6, bg, "Wolf_Hit");
        Preview($"{CreepRoot}/Wolf/Die.png", 6, bg, "Wolf_Die");
        Debug.Log("[MonsterSpriteGenerator] Đã tạo preview x6 trong Captures/MonsterPreview.");
    }

    private static void Preview(string path, int scale, Color32 bg, string label)
    {
        Texture2D src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        src.LoadImage(File.ReadAllBytes(path));

        int w = src.width * scale, h = src.height * scale;
        Texture2D dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[w * h];
        Color32[] srcPixels = src.GetPixels32();

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 c = srcPixels[(y / scale) * src.width + (x / scale)];
                pixels[y * w + x] = c.a > 0 ? c : bg;
            }

        dst.SetPixels32(pixels);
        dst.Apply();

        string outPath = $"Captures/MonsterPreview/{label}_x{scale}.png";
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllBytes(outPath, dst.EncodeToPNG());

        UnityEngine.Object.DestroyImmediate(src);
        UnityEngine.Object.DestroyImmediate(dst);
        AssetDatabase.Refresh();
    }

    // ------------------------------------------------------------------
    // NHỆN: 48x40, PPU 40 (cao ~1 unit), 4 frame đi
    // ------------------------------------------------------------------
    private static void GenerateSpider()
    {
        const int W = 48, H = 40, PPU = 40;
        const int walkFrames = 4, dieFrames = 4;

        Raster walk = new Raster(W * walkFrames, H);
        for (int i = 0; i < walkFrames; i++)
            DrawSpiderFrame(walk, i * W, i / (float)walkFrames, 0f, false);
        SaveSheet(walk, $"{CreepRoot}/Spider/Move.png", W, H, PPU, "Move", walkFrames);

        Raster hit = new Raster(W * 2, H);
        DrawSpiderFrame(hit, 0, 0f, 1f, false);
        Raster flash = DrawSpiderRaster(W, H, 0, 0f, 1f, true);
        Blit(flash, hit, W, 0);
        SaveSheet(hit, $"{CreepRoot}/Spider/Hit.png", W, H, PPU, "Hit", 2);

        Raster die = new Raster(W * dieFrames, H);
        for (int i = 0; i < dieFrames; i++)
            DrawSpiderFrame(die, i * W, 0f, i / (float)(dieFrames - 1), true);
        SaveSheet(die, $"{CreepRoot}/Spider/Die.png", W, H, PPU, "Die", dieFrames);
    }

    private static void DrawSpiderFrame(Raster sheet, int offsetX, float phase, float hurt, bool dying)
    {
        Raster r = DrawSpiderRaster(48, 40, 0, phase, hurt, false);
        if (dying)
        {
            float t = Mathf.Clamp01(hurt);
            r = DrawSpiderRaster(48, 40, 0, phase, 0f, false, t);
        }
        Blit(r, sheet, offsetX, 0);
    }

    private static Raster DrawSpiderRaster(int W, int H, int unused, float phase, float hurt, bool flash, float dying = 0f)
    {
        Raster r = new Raster(W, H);

        int bob = (!flash && Mathf.RoundToInt(Mathf.Sin(phase * Mathf.PI * 2f)) > 0) ? 1 : 0;
        int recoil = hurt > 0f ? 2 : 0; // bị đánh lùi về sau (trái)
        int cx = 18 - recoil, cy = 21 + bob;
        int headX = 30 - recoil, headY = 21 + bob;
        bool dead = dying > 0f;

        // --- Chân xa (phía trên) ---
        float[] farTipX = { 12f, 21f, 33f, 44f };
        float[] farTipY = { 36f, 38f, 38f, 35f };
        float[] farKneeX = { 20f, 26f, 33f, 39f };
        float[] farKneeY = { 32f, 34f, 35f, 33f };
        for (int i = 0; i < 4; i++)
        {
            float p = phase * Mathf.PI * 2f + i * Mathf.PI * 0.5f;
            float tx = farTipX[i] + Mathf.Round(Mathf.Sin(p) * 2f);
            float ty = farTipY[i] + Mathf.Round(Mathf.Cos(p) * 1.2f);
            float kx = farKneeX[i];
            float ky = farKneeY[i] + Mathf.Round(Mathf.Cos(p) * 1.5f);
            if (dead)
            {
                float t = dying;
                tx = Mathf.Lerp(tx, headX - 2f, t);
                ty = Mathf.Lerp(ty, cy + 8f, t);
                kx = Mathf.Lerp(kx, cx + 2f, t);
                ky = Mathf.Lerp(ky, cy + 7f, t);
            }
            DrawArm(r, new Vector2(headX - 2 + i, headY + 2), new Vector2(kx, ky), new Vector2(tx, ty), true);
        }

        // --- Thân ---
        float deflate = dead ? 1f - 0.35f * dying : 1f;
        r.Ellipse(cx, cy, 10, 8 * deflate, SpiderBase);
        r.Ellipse(cx + 2, cy - 3, 8, 3 * deflate, SpiderDark);
        r.Ellipse(cx - 3, cy + 3, 4, 2.5f, SpiderLight);

        r.Ellipse(headX, headY, 7, 6, SpiderBase);
        r.Ellipse(headX + 1, headY - 2, 5, 2.5f, SpiderDark);
        r.Ellipse(headX - 2, headY + 2, 2.5f, 2, SpiderLight);

        // Vân lưng
        r.Set(cx - 1, cy + 5, SpiderAccent);
        r.Set(cx, cy + 3, SpiderAccent);
        r.Set(cx - 1, cy + 1, SpiderAccent);
        r.Set(cx, cy - 1, SpiderAccent);

        // Mắt (4 mắt đỏ)
        Color32 eye = dead ? new Color32(120, 30, 30, 255) : SpiderEye;
        r.Set(headX + 3, headY + 3, eye);
        r.Set(headX + 5, headY + 3, eye);
        r.Set(headX + 3, headY + 1, eye);
        r.Set(headX + 5, headY + 1, eye);

        // Nanh + nọc
        r.Set(headX + 4, headY - 2, SpiderFang);
        r.Set(headX + 6, headY - 2, SpiderFang);
        if (!dead && phase > 0.45f)
            r.Set(headX + 5, headY - 4, Venom);

        // --- Chân gần (phía dưới) ---
        float[] nearTipX = { 14f, 23f, 35f, 45f };
        float[] nearTipY = { 5f, 2f, 2f, 5f };
        float[] nearKneeX = { 21f, 28f, 35f, 41f };
        float[] nearKneeY = { 11f, 8f, 8f, 11f };
        for (int i = 0; i < 4; i++)
        {
            float p = phase * Mathf.PI * 2f + i * Mathf.PI * 0.5f + Mathf.PI;
            float tx = nearTipX[i] + Mathf.Round(Mathf.Sin(p) * 2f);
            float ty = nearTipY[i] + Mathf.Round(Mathf.Cos(p) * 1.2f);
            float kx = nearKneeX[i];
            float ky = nearKneeY[i] + Mathf.Round(Mathf.Cos(p) * 1.5f);
            if (dead)
            {
                float t = dying;
                tx = Mathf.Lerp(tx, headX - 2f, t);
                ty = Mathf.Lerp(ty, cy - 7f, t);
                kx = Mathf.Lerp(kx, cx + 2f, t);
                ky = Mathf.Lerp(ky, cy - 7f, t);
            }
            DrawArm(r, new Vector2(headX - 1 + i, headY - 2), new Vector2(kx, ky), new Vector2(tx, ty), false);
        }

        r.Outline(SpiderOutline);

        if (flash)
        {
            for (int i = 0; i < r.Pixels.Length; i++)
                if (r.Pixels[i].a > 0) r.Pixels[i] = new Color32(255, 255, 255, 255);
            r.Outline(SpiderOutline);
        }

        return r;
    }

    /// <summary>Chân 2 khúc: vẽ viền 3px rồi lõi 1px để chân đọc rõ ở 48px.</summary>
    private static void DrawArm(Raster r, Vector2 hip, Vector2 knee, Vector2 tip, bool far)
    {
        Color32 edge = SpiderOutline;
        Color32 core = far ? SpiderDark : SpiderBase;
        r.Poly(new[] { hip, knee }, 3f, edge);
        r.Poly(new[] { knee, tip }, 3f, edge);
        r.Poly(new[] { hip, knee }, 1f, core);
        r.Poly(new[] { knee, tip }, 1f, far ? SpiderDark : SpiderLight);
    }

    // ------------------------------------------------------------------
    // SÓI: 48x32, PPU 32, 6 frame chạy
    // ------------------------------------------------------------------
    private static void GenerateWolf()
    {
        const int W = 48, H = 32, PPU = 32;
        const int runFrames = 6, dieFrames = 4;

        Raster run = new Raster(W * runFrames, H);
        for (int i = 0; i < runFrames; i++)
            Blit(DrawWolfRaster(W, H, i / (float)runFrames, 0f, 0f), run, i * W, 0);
        SaveSheet(run, $"{CreepRoot}/Wolf/Move.png", W, H, PPU, "Move", runFrames);

        Raster hit = new Raster(W * 2, H);
        Blit(DrawWolfRaster(W, H, 0f, 1f, 0f), hit, 0, 0);
        Blit(DrawWolfRaster(W, H, 0f, 1f, 0f, true), hit, W, 0);
        SaveSheet(hit, $"{CreepRoot}/Wolf/Hit.png", W, H, PPU, "Hit", 2);

        Raster die = new Raster(W * dieFrames, H);
        for (int i = 0; i < dieFrames; i++)
            Blit(DrawWolfRaster(W, H, 0f, 0f, i / (float)(dieFrames - 1)), die, i * W, 0);
        SaveSheet(die, $"{CreepRoot}/Wolf/Die.png", W, H, PPU, "Die", dieFrames);
    }

    private static Raster DrawWolfRaster(int W, int H, float phase, float hurt, float dying, bool flash = false)
    {
        Raster r = new Raster(W, H);
        float t = Mathf.Clamp01(dying);
        int recoil = hurt > 0f ? 2 : 0;

        float bodyLift = Mathf.Lerp(0f, -8.5f, t);
        float bodySquash = Mathf.Lerp(1f, 0.74f, t);

        // Chân trước khi vẽ thân (thân sẽ che khớp hông).
        float[] hipX = { 11f, 14f, 29f, 32f };
        float[] legPhase = { 0f, 0.45f, Mathf.PI + 0.15f, Mathf.PI + 0.6f };
        for (int side = 0; side < 2; side++)
        {
            bool far = side == 0;
            for (int i = 0; i < 4; i++)
            {
                float p = phase * Mathf.PI * 2f + legPhase[i] + (far ? 0.25f : 0f);
                float stride = Mathf.Round(Mathf.Sin(p) * 3.5f);
                float lift = Mathf.Max(0f, Mathf.Cos(p)) * 3f;

                Vector2 hip = new Vector2(hipX[i] - recoil, 15f + bodyLift);
                Vector2 knee = new Vector2(hipX[i] - recoil + stride * 0.4f, 9f - lift + bodyLift * 0.2f);
                Vector2 paw = new Vector2(hipX[i] - recoil + stride, 2f);

                if (t > 0f)
                {
                    float splay = i < 2 ? -3.5f : 3.5f;
                    knee = Vector2.Lerp(knee, new Vector2(hipX[i] - recoil + splay * 0.8f, 4f), t);
                    paw = Vector2.Lerp(paw, new Vector2(hipX[i] - recoil + splay, 2f), t);
                }

                DrawWolfLeg(r, hip, knee, paw, far);
            }
        }

        // Đuôi (vẽ sau chân xa, trước thân).
        float swing = Mathf.Round(Mathf.Sin(phase * Mathf.PI * 2f) * 2f);
        Vector2 tailRoot = new Vector2(8f - recoil, 20.5f + bodyLift * 0.8f);
        Vector2 tailMid = new Vector2(4f - recoil, Mathf.Lerp(17f + swing, 8f, t) + bodyLift * 0.25f);
        Vector2 tailTip = new Vector2(2f - recoil, Mathf.Lerp(11.5f + swing, 3f, t));
        r.Poly(new[] { tailRoot, tailMid, tailTip }, 4f, WolfOutline);
        r.Poly(new[] { tailRoot, tailMid, tailTip }, 2f, WolfBase);
        r.Disc(tailTip.x, tailTip.y, 1.4f, WolfLight);

        // Thân (silhouette sói: ức sâu, eo thóp, lưng dốc).
        Vector2[] body =
        {
            new Vector2(8, 21), new Vector2(12, 23), new Vector2(18, 22.5f),
            new Vector2(24, 22), new Vector2(29, 23.5f), new Vector2(32, 24),
            new Vector2(34, 22), new Vector2(34.5f, 19), new Vector2(32, 15.5f),
            new Vector2(28, 14.5f), new Vector2(22, 15), new Vector2(16, 15.5f),
            new Vector2(11, 17)
        };
        for (int i = 0; i < body.Length; i++)
            body[i] = new Vector2(body[i].x - recoil, 14f + (body[i].y - 14f) * bodySquash + bodyLift);

        r.Polygon(body, WolfBase);

        // Bóng bụng + highlight sống lưng.
        r.Ellipse(21 - recoil, 16.2f + bodyLift, 9, 1.6f, WolfDark);
        r.Ellipse(30 - recoil, 20.5f + bodyLift, 2.6f, 3.2f, WolfLight);
        r.Poly(new[]
        {
            new Vector2(9 - recoil, 22.2f + bodyLift), new Vector2(15 - recoil, 23.4f + bodyLift),
            new Vector2(22 - recoil, 22.9f + bodyLift), new Vector2(28 - recoil, 23.8f + bodyLift),
            new Vector2(32.5f - recoil, 24f + bodyLift)
        }, 1f, WolfLight);

        // Gờ sườn.
        for (int i = 0; i < 3; i++)
            r.Set(23 + i * 3 - recoil, 17 + (int)bodyLift, WolfDark);

        // Bờm gai sau gáy.
        DrawSpike(r, 28.5f - recoil, 23.3f + bodyLift, 30f - recoil, 26.5f + bodyLift, WolfLight);
        DrawSpike(r, 31.5f - recoil, 23.8f + bodyLift, 32.5f - recoil, 27f + bodyLift, WolfHighlight);
        DrawSpike(r, 33.5f - recoil, 23.6f + bodyLift, 35f - recoil, 26f + bodyLift, WolfLight);
        // Lông ức
        DrawSpike(r, 33f - recoil, 18.5f + bodyLift, 35f - recoil, 20.5f + bodyLift, WolfLight);

        // --- Đầu ---
        float headX = 38f - recoil - t * 2f;
        float headY = 21.5f + bodyLift * 0.85f - t * 3f;

        // Tai (sau -> trước)
        r.Polygon(new[]
        {
            new Vector2(35.5f, headY + 2), new Vector2(36.4f, headY + 6.2f), new Vector2(38.1f, headY + 2.2f)
        }, WolfBase);
        r.Polygon(new[]
        {
            new Vector2(38.3f, headY + 2.2f), new Vector2(39.2f, headY + 6f), new Vector2(40.6f, headY + 2)
        }, WolfBase);

        // Sọ + gò má
        r.Ellipse(headX, headY, 4, 3.3f, WolfLight);
        r.Ellipse(headX - 1, headY - 1.4f, 2.6f, 1.5f, WolfDark);

        // Mõm dài (đặc trưng sói)
        r.Polygon(new[]
        {
            new Vector2(40f, headY + 1.5f), new Vector2(44.5f, headY + 0.7f),
            new Vector2(46.5f, headY - 0.5f), new Vector2(45f, headY - 1.9f),
            new Vector2(40.5f, headY - 2f)
        }, WolfLight);
        r.Poly(new[]
        {
            new Vector2(40f, headY + 1.5f), new Vector2(44.5f, headY + 0.7f),
            new Vector2(46f, headY - 0.4f)
        }, 1f, WolfHighlight);

        // Mũi, miệng
        r.Set((int)headX + 8, (int)headY - 1, WolfOutline);
        r.Set((int)headX + 8, (int)headY, WolfOutline);
        r.Set((int)headX + 7, (int)headY - 2, WolfOutline);

        // Mắt hổ phách
        if (t < 0.99f)
        {
            r.Set((int)headX + 2, (int)headY + 2, WolfEye);
            r.Set((int)headX + 3, (int)headY + 2, WolfEye);
        }
        else
        {
            for (int i = 0; i < 3; i++)
                r.Set((int)headX + 1 + i, (int)headY + 2, WolfOutline);
        }

        // Nanh + lưỡi
        r.Set((int)headX + 7, (int)headY - 3, WolfFang);
        if (hurt > 0f || t > 0f)
            r.Set((int)headX + 6, (int)headY - 4, WolfTongue);

        r.Outline(WolfOutline);

        if (flash)
        {
            for (int i = 0; i < r.Pixels.Length; i++)
                if (r.Pixels[i].a > 0) r.Pixels[i] = new Color32(255, 255, 255, 255);
            r.Outline(WolfOutline);
        }

        return r;
    }

    private static void DrawSpike(Raster r, float x0, float y0, float tipX, float tipY, Color32 c)
    {
        r.Polygon(new[]
        {
            new Vector2(x0 - 1.2f, y0), new Vector2(tipX, tipY), new Vector2(x0 + 1.2f, y0)
        }, c);
    }

    private static void DrawWolfLeg(Raster r, Vector2 hip, Vector2 knee, Vector2 paw, bool far)
    {
        Color32 core = far ? WolfDark : WolfBase;
        r.Poly(new[] { hip, knee }, 2f, WolfOutline);
        r.Poly(new[] { knee, paw }, 2f, WolfOutline);
        r.Poly(new[] { hip, knee }, 1f, core);
        r.Poly(new[] { knee, paw }, 1f, far ? WolfDark : WolfLight);
        r.Disc(paw.x, paw.y, 1.1f, far ? WolfDark : WolfBase);
        r.Set((int)paw.x, (int)paw.y + 1, far ? WolfDark : WolfLight);
    }

    // ------------------------------------------------------------------
    // IO + IMPORT
    // ------------------------------------------------------------------
    private static void Blit(Raster src, Raster dst, int offsetX, int offsetY)
    {
        for (int y = 0; y < src.H; y++)
            for (int x = 0; x < src.W; x++)
            {
                Color32 c = src.Pixels[y * src.W + x];
                if (c.a > 0)
                    dst.Set(x + offsetX, y + offsetY, c);
            }
    }

    private static void SaveSheet(Raster sheet, string path, int frameW, int frameH, int ppu, string prefix, int frames)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        Texture2D tex = new Texture2D(sheet.W, sheet.H, TextureFormat.RGBA32, false);
        tex.SetPixels32(sheet.Pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 2048;

        SpriteMetaData[] metas = new SpriteMetaData[frames];
        for (int i = 0; i < frames; i++)
        {
            metas[i] = new SpriteMetaData
            {
                name = $"{prefix}_{i}",
                rect = new Rect(i * frameW, 0, frameW, frameH),
                alignment = (int)SpriteAlignment.BottomCenter,
                pivot = new Vector2(0.5f, 0f)
            };
        }
        importer.spritesheet = metas;
        importer.SaveAndReimport();
    }

    // ------------------------------------------------------------------
    // Raster pixel-art tối giản
    // ------------------------------------------------------------------
    private class Raster
    {
        public readonly int W, H;
        public readonly Color32[] Pixels;

        public Raster(int w, int h)
        {
            W = w;
            H = h;
            Pixels = new Color32[w * h];
        }

        public void Set(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            Pixels[y * W + x] = c;
        }

        public void Disc(float cx, float cy, float radius, Color32 c)
        {
            int x0 = Mathf.FloorToInt(cx - radius);
            int x1 = Mathf.CeilToInt(cx + radius);
            int y0 = Mathf.FloorToInt(cy - radius);
            int y1 = Mathf.CeilToInt(cy + radius);
            float r2 = radius * radius;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    if (dx * dx + dy * dy <= r2) Set(x, y, c);
                }
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
        {
            int y0 = Mathf.FloorToInt(cy - ry);
            int y1 = Mathf.CeilToInt(cy + ry);
            for (int y = y0; y <= y1; y++)
            {
                float dy = (y - cy) / ry;
                if (Mathf.Abs(dy) > 1f) continue;
                float half = rx * Mathf.Sqrt(1f - dy * dy);
                int x0 = Mathf.CeilToInt(cx - half);
                int x1 = Mathf.FloorToInt(cx + half);
                for (int x = x0; x <= x1; x++) Set(x, y, c);
            }
        }

        public void Line(Vector2 a, Vector2 b, float thickness, Color32 c)
        {
            float len = Vector2.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(len * 2f));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
                Disc(p.x, p.y, thickness * 0.5f, c);
            }
        }

        public void Poly(Vector2[] pts, float thickness, Color32 c)
        {
            for (int i = 0; i < pts.Length - 1; i++)
                Line(pts[i], pts[i + 1], thickness, c);
        }

        public void Polygon(Vector2[] pts, Color32 c)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < pts.Length; i++)
            {
                minY = Mathf.Min(minY, pts[i].y);
                maxY = Mathf.Max(maxY, pts[i].y);
            }

            int y0 = Mathf.FloorToInt(minY);
            int y1 = Mathf.CeilToInt(maxY);
            List<float> xs = new List<float>(8);

            for (int y = y0; y <= y1; y++)
            {
                float sy = y + 0.5f;
                xs.Clear();
                for (int i = 0; i < pts.Length; i++)
                {
                    Vector2 a = pts[i];
                    Vector2 b = pts[(i + 1) % pts.Length];
                    if ((a.y <= sy && b.y > sy) || (b.y <= sy && a.y > sy))
                        xs.Add(a.x + (sy - a.y) / (b.y - a.y) * (b.x - a.x));
                }
                xs.Sort();
                for (int i = 0; i + 1 < xs.Count; i += 2)
                {
                    int x0 = Mathf.CeilToInt(xs[i]);
                    int x1 = Mathf.FloorToInt(xs[i + 1]);
                    for (int x = x0; x <= x1; x++) Set(x, y, c);
                }
            }
        }

        public void Outline(Color32 c)
        {
            bool[] mask = new bool[W * H];
            for (int i = 0; i < Pixels.Length; i++)
                mask[i] = Pixels[i].a > 0;

            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    if (mask[i]) continue;
                    if ((x > 0 && mask[i - 1]) || (x < W - 1 && mask[i + 1]) ||
                        (y > 0 && mask[i - W]) || (y < H - 1 && mask[i + W]))
                        Pixels[i] = c;
                }
        }
    }
}
