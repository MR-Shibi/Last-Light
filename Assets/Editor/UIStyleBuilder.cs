using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastLight.Editor
{
    /// <summary>
    /// Procedural Dark-Gothic Victorian UI & Mansion Texture Builder:
    /// - Beveled dark-slate frames with antique brass double-line inner trim
    /// - Broken weathered dark-wood plank button (jagged splinters, grain, rusty nails) for OPEN / CLOSE
    /// - Crumpled aged parchment paper texture (creased fold lines, burnt/torn edges) for Story Notes & Inventory
    /// - Translucent procedural Cobweb texture + 4 eerie Gothic Oil Painting canvas textures
    /// - Gear icon + 4 Gothic Symbol sprites (Skull, Moon, Key, Crown).
    /// </summary>
    public static class UIStyleBuilder
    {
        public struct GothicUiAssets
        {
            public Sprite gothicFrame;
            public Sprite gothicButton;
            public Sprite oilVialFrame;
            public Sprite brokenWoodPlank;   // Splintered dark-wood plank for OPEN / CLOSE button
            public Sprite crumpledPaper;     // Aged crumpled parchment for Story Notes & Inventory reader
            public Sprite redVignette;       // Pulsing crimson border vignette for Bandit Patrol warning
            public Sprite circleFill;
            public Sprite circleRing;
            public Sprite whiteSolid;
            public Sprite gearIcon;
            public Sprite symbolSkull;
            public Sprite symbolMoon;
            public Sprite symbolKey;
            public Sprite symbolCrown;
            public Texture2D cobwebTexture;
            public Texture2D paintingPortrait;
            public Texture2D paintingMansion;
            public Texture2D paintingMoonTree;
            public Texture2D paintingMirror;
            public Font font;
        }

        public static GothicUiAssets GenerateAll(string spritesDir)
        {
            if (!Directory.Exists(spritesDir)) Directory.CreateDirectory(spritesDir);

            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 32);

            return new GothicUiAssets
            {
                gothicFrame = GenSprite(spritesDir, "UI_GothicFrame.png", 96, 96, new Vector4(24, 24, 24, 24), DrawGothicFrame),
                gothicButton = GenSprite(spritesDir, "UI_GothicButton.png", 96, 96, new Vector4(22, 22, 22, 22), DrawGothicButton),
                oilVialFrame = GenSprite(spritesDir, "UI_OilVialFrame.png", 96, 64, new Vector4(20, 18, 20, 18), DrawOilVialFrame),
                brokenWoodPlank = GenSprite(spritesDir, "UI_BrokenWoodPlank.png", 256, 96, Vector4.zero, DrawBrokenWoodPlank),
                crumpledPaper = GenSprite(spritesDir, "UI_CrumpledPaper.png", 384, 288, Vector4.zero, DrawCrumpledPaper),
                redVignette = GenSprite(spritesDir, "UI_RedVignette.png", 256, 256, Vector4.zero, DrawRedVignette),
                circleFill = GenSprite(spritesDir, "UI_CircleFill.png", 128, 128, Vector4.zero, DrawCircleFill),
                circleRing = GenSprite(spritesDir, "UI_CircleRing.png", 128, 128, Vector4.zero, DrawCircleRing),
                whiteSolid = GenSprite(spritesDir, "UI_WhiteSolid.png", 16, 16, Vector4.zero, (x, y, w, h) => Color.white),
                gearIcon = GenSprite(spritesDir, "UI_GearIcon.png", 96, 96, Vector4.zero, DrawGearIcon),
                symbolSkull = GenSprite(spritesDir, "UI_SymSkull.png", 128, 128, Vector4.zero, DrawSkullSymbol),
                symbolMoon = GenSprite(spritesDir, "UI_SymMoon.png", 128, 128, Vector4.zero, DrawMoonSymbol),
                symbolKey = GenSprite(spritesDir, "UI_SymKey.png", 128, 128, Vector4.zero, DrawKeySymbol),
                symbolCrown = GenSprite(spritesDir, "UI_SymCrown.png", 128, 128, Vector4.zero, DrawCrownSymbol),
                cobwebTexture = GenTexture(spritesDir, "Tex_Cobweb.png", 256, 256, DrawCobwebTexture),
                paintingPortrait = GenTexture(spritesDir, "Tex_Painting_Portrait.png", 128, 160, DrawPortraitPainting),
                paintingMansion = GenTexture(spritesDir, "Tex_Painting_Mansion.png", 160, 112, DrawMansionPainting),
                paintingMoonTree = GenTexture(spritesDir, "Tex_Painting_MoonTree.png", 160, 112, DrawMoonTreePainting),
                paintingMirror = GenTexture(spritesDir, "Tex_Painting_Mirror.png", 128, 160, DrawCrackedMirrorPainting),
                font = f
            };
        }

        public static GameObject CreateBeveledPanel(string name, Transform parent, Sprite frameSprite, Color tint)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = frameSprite;
            img.type = Image.Type.Sliced;
            img.color = tint;
            return go;
        }

        public static Button CreateGothicButton(string name, Transform parent, Sprite btnSprite, Font font, string label, int fontSize, Color bgTint, Color textColor, Image.Type imgType = Image.Type.Sliced)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = btnSprite;
            img.type = imgType;
            img.color = bgTint;

            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.08f, 1.0f, 1f);
            cb.pressedColor = new Color(0.80f, 0.76f, 0.68f, 1f);
            btn.colors = cb;

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(go.transform, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.color = textColor;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            RectTransform rt = txt.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(14f, 6f);
            rt.offsetMax = new Vector2(-14f, -6f);

            Shadow s = txtObj.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.80f);
            s.effectDistance = new Vector2(2f, -2f);

            return btn;
        }

        // =========================================================================
        // PROCEDURAL UI & ENVIRONMENT TEXTURE GENERATORS
        // =========================================================================

        private delegate Color PixelFunc(int x, int y, int w, int h);

        private static Sprite GenSprite(string dir, string fileName, int w, int h, Vector4 border, PixelFunc func)
        {
            string path = $"{dir}/{fileName}";
            WriteTexturePng(path, w, h, func);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spriteBorder = border;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Texture2D GenTexture(string dir, string fileName, int w, int h, PixelFunc func)
        {
            string path = $"{dir}/{fileName}";
            WriteTexturePng(path, w, h, func);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Default;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = true;
                imp.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void WriteTexturePng(string path, int w, int h, PixelFunc func)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = func(x, y, w, h);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// Splintered, weathered dark-wood plank silhouette with jagged broken ends,
        /// horizontal wood-grain fibers, knots, and 4 rusty iron nail heads!
        /// </summary>
        private static Color DrawBrokenWoodPlank(int x, int y, int w, int h)
        {
            float u = x / (float)(w - 1);
            float v = y / (float)(h - 1);

            // Jagged splintered left and right broken edges
            float leftSplinter = 0.04f + 0.06f * Mathf.PerlinNoise(0.7f, v * 11.5f) + (Mathf.Sin(v * 29f) > 0.45f ? 0.03f : 0f);
            float rightSplinter = 0.96f - 0.06f * Mathf.PerlinNoise(4.3f, v * 12.2f) - (Mathf.Cos(v * 25f) > 0.45f ? 0.03f : 0f);
            float topChip = 0.93f - 0.05f * Mathf.PerlinNoise(u * 9f, 2.1f);
            float botChip = 0.07f + 0.05f * Mathf.PerlinNoise(u * 9f, 7.8f);

            if (u < leftSplinter || u > rightSplinter || v < botChip || v > topChip)
            {
                return Color.clear;
            }

            // Rich weathered dark walnut wood grain + subtle knots
            float grain = Mathf.PerlinNoise(u * 4.5f, v * 28f);
            float fineFibers = Mathf.PerlinNoise(u * 14f, v * 58f) * 0.25f;
            float plankSeam = Mathf.Abs(v - 0.52f) < 0.015f ? -0.18f : 0f;
            float darkEdge = Mathf.Min(Mathf.Min(u - leftSplinter, rightSplinter - u) * 14f, Mathf.Min(v - botChip, topChip - v) * 12f);
            float bevel = Mathf.Clamp01(darkEdge);

            float shade = Mathf.Clamp01(0.58f + (grain - 0.5f) * 0.42f + (fineFibers - 0.12f) + plankSeam) * Mathf.Lerp(0.45f, 1.0f, bevel);
            Color wood = new Color(0.40f * shade, 0.24f * shade, 0.13f * shade, 1f);

            // 4 rusty iron nail heads near corners
            Vector2[] nails =
            {
                new Vector2(0.13f, 0.76f), new Vector2(0.13f, 0.24f),
                new Vector2(0.87f, 0.76f), new Vector2(0.87f, 0.24f)
            };
            for (int i = 0; i < nails.Length; i++)
            {
                float d = Vector2.Distance(new Vector2(u * 2.5f, v), new Vector2(nails[i].x * 2.5f, nails[i].y));
                if (d < 0.055f)
                {
                    float rim = d > 0.036f ? 0.55f : 1f;
                    return new Color(0.32f * rim, 0.19f * rim, 0.14f * rim, 1f);
                }
            }

            return wood;
        }

        /// <summary>
        /// Aged, crumpled parchment sheet with darkened burnt/torn edges,
        /// visible diagonal & crossed fold creases, and crisp readable center!
        /// </summary>
        private static Color DrawCrumpledPaper(int x, int y, int w, int h)
        {
            float u = x / (float)(w - 1);
            float v = y / (float)(h - 1);

            // Ragged torn parchment perimeter
            float tearL = 0.025f + 0.028f * Mathf.PerlinNoise(1.2f, v * 14f);
            float tearR = 0.975f - 0.028f * Mathf.PerlinNoise(5.4f, v * 14f);
            float tearB = 0.030f + 0.028f * Mathf.PerlinNoise(u * 14f, 3.1f);
            float tearT = 0.970f - 0.028f * Mathf.PerlinNoise(u * 14f, 8.6f);

            if (u < tearL || u > tearR || v < tearB || v > tearT) return Color.clear;

            float edgeDist = Mathf.Min(Mathf.Min(u - tearL, tearR - u), Mathf.Min(v - tearB, tearT - v));
            float burntEdge = Mathf.Clamp01(edgeDist * 10.5f);

            // Paper fiber & sepia age mottling
            float fiber = Mathf.PerlinNoise(u * 18f, v * 18f);
            float stain = Mathf.PerlinNoise(u * 4.2f + 2f, v * 4.2f + 1f);

            // Visible crumpled fold creases (vertical, horizontal & diagonal crease shadows)
            float creaseV = Mathf.Abs(u - 0.50f + (Mathf.PerlinNoise(0.5f, v * 6f) - 0.5f) * 0.02f);
            float creaseH = Mathf.Abs(v - 0.52f + (Mathf.PerlinNoise(u * 6f, 0.5f) - 0.5f) * 0.02f);
            float creaseD1 = Mathf.Abs((u * 0.8f + v * 0.6f) - 0.72f);
            float creaseD2 = Mathf.Abs((u * 0.65f - v * 0.75f) + 0.14f);

            float creaseDark = 0f;
            if (creaseV < 0.012f) creaseDark += (1f - creaseV / 0.012f) * 0.13f;
            if (creaseH < 0.014f) creaseDark += (1f - creaseH / 0.014f) * 0.13f;
            if (creaseD1 < 0.011f) creaseDark += (1f - creaseD1 / 0.011f) * 0.09f;
            if (creaseD2 < 0.011f) creaseDark += (1f - creaseD2 / 0.011f) * 0.08f;

            float paperVal = Mathf.Clamp01(0.95f - (1f - fiber) * 0.09f - (1f - stain) * 0.08f - creaseDark);
            Color basePaper = new Color(0.94f * paperVal, 0.87f * paperVal, 0.73f * paperVal, 1f);
            Color scorchEdge = new Color(0.38f, 0.25f, 0.14f, 1f);

            return Color.Lerp(scorchEdge, basePaper, Mathf.Pow(burntEdge, 0.55f));
        }

        /// <summary>
        /// Translucent corner cobweb texture (radial spokes + sagging concentric silk threads).
        /// </summary>
        private static Color DrawCobwebTexture(int x, int y, int w, int h)
        {
            // Corner-anchored at top-left (u=0, v=1)
            float u = x / (float)(w - 1);
            float v = 1f - (y / (float)(h - 1));
            float r = Mathf.Sqrt(u * u + v * v);
            if (r > 1.02f) return Color.clear;

            float ang = Mathf.Atan2(v, u); // 0..PI/2
            int spokeCount = 7;
            float sector = (Mathf.PI * 0.5f) / spokeCount;
            float spokeMod = Mathf.Abs(((ang + sector * 0.5f) % sector) - sector * 0.5f);
            float spokeDist = spokeMod * Mathf.Max(0.08f, r);

            // Concentric sagging web rings
            float sag = Mathf.Sin((ang % sector) / sector * Mathf.PI) * 0.045f * r;
            float ringFreq = (r + sag) * 8.5f;
            float ringDist = Mathf.Abs(ringFreq - Mathf.Round(ringFreq));

            float alpha = 0f;
            if (spokeDist < 0.0085f) alpha = Mathf.Max(alpha, (1f - spokeDist / 0.0085f) * 0.72f);
            if (ringDist < 0.065f && r > 0.08f) alpha = Mathf.Max(alpha, (1f - ringDist / 0.065f) * 0.58f);

            float fade = Mathf.Clamp01(1.05f - r * 0.85f);
            return new Color(0.84f, 0.84f, 0.86f, alpha * fade);
        }

        private static Color DrawPortraitPainting(int x, int y, int w, int h)
        {
            float u = x / (float)(w - 1);
            float v = y / (float)(h - 1);
            float vig = 1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.52f)) * 1.15f;
            Color bg = new Color(0.18f, 0.14f, 0.11f, 1f) * Mathf.Clamp(vig + 0.3f, 0.2f, 1.1f);
            // Shadowed Victorian bust silhouette
            bool head = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.66f)) < 0.16f;
            bool shoulders = Vector2.Distance(new Vector2(u, v * 0.7f), new Vector2(0.5f, 0.18f)) < 0.34f && v < 0.52f;
            if (head) return new Color(0.42f, 0.34f, 0.26f, 1f);
            if (shoulders) return new Color(0.11f, 0.09f, 0.10f, 1f);
            bg.a = 1f;
            return bg;
        }

        private static Color DrawMansionPainting(int x, int y, int w, int h)
        {
            float u = x / (float)(w - 1);
            float v = y / (float)(h - 1);
            Color sky = Color.Lerp(new Color(0.10f, 0.11f, 0.15f, 1f), new Color(0.22f, 0.24f, 0.30f, 1f), v);
            if (Vector2.Distance(new Vector2(u, v), new Vector2(0.75f, 0.78f)) < 0.10f) return new Color(0.72f, 0.70f, 0.58f, 1f);
            bool tower = Mathf.Abs(u - 0.5f) < 0.16f && v < 0.62f;
            bool spire = Mathf.Abs(u - 0.5f) < Mathf.Max(0f, (0.86f - v) * 0.65f) && v >= 0.62f && v < 0.86f;
            bool hill = v < 0.22f + Mathf.Sin(u * 3.14f) * 0.08f;
            if (tower || spire || hill) return new Color(0.06f, 0.06f, 0.07f, 1f);
            return sky;
        }

        private static Color DrawMoonTreePainting(int x, int y, int w, int h)
        {
            float u = x / (float)(w - 1);
            float v = y / (float)(h - 1);
            Color bg = Color.Lerp(new Color(0.09f, 0.12f, 0.14f, 1f), new Color(0.19f, 0.22f, 0.26f, 1f), v);
            if (Vector2.Distance(new Vector2(u, v), new Vector2(0.32f, 0.74f)) < 0.13f) return new Color(0.68f, 0.64f, 0.50f, 1f);
            bool trunk = Mathf.Abs(u - 0.62f) < 0.035f && v < 0.66f;
            bool branch = Mathf.Abs((v - 0.52f) - (u - 0.62f) * 0.7f) < 0.025f && u > 0.38f && u < 0.85f;
            if (trunk || branch || v < 0.16f) return new Color(0.05f, 0.05f, 0.05f, 1f);
            return bg;
        }

        private static Color DrawCrackedMirrorPainting(int x, int y, int w, int h)
        {
            float u = x / (float)(w - 1);
            float v = y / (float)(h - 1);
            float sheen = 0.22f + 0.16f * Mathf.PerlinNoise(u * 4f, v * 4f);
            Color glass = new Color(sheen * 0.9f, sheen, sheen * 1.12f, 1f);
            float crack1 = Mathf.Abs((u - 0.35f) * 1.1f - (v - 0.65f));
            float crack2 = Mathf.Abs((u - 0.35f) * 0.4f + (v - 0.65f));
            if ((crack1 < 0.012f && u > 0.18f && u < 0.82f) || (crack2 < 0.010f && u > 0.32f))
                return new Color(0.06f, 0.06f, 0.07f, 1f);
            return glass;
        }

        private static Color DrawGothicFrame(int x, int y, int w, int h)
        {
            int d = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
            int corner = Mathf.Min(x, w - 1 - x) + Mathf.Min(y, h - 1 - y);
            if (corner < 10) return Color.clear;

            Color brass = new Color(0.82f, 0.66f, 0.30f, 1f);
            Color darkTrim = new Color(0.20f, 0.16f, 0.10f, 1f);
            Color slateFill = new Color(0.88f, 0.89f, 0.92f, 1f);

            if (d <= 2 || corner <= 13) return brass;
            if (d <= 5) return darkTrim;
            if (d == 7 || d == 8) return new Color(0.75f, 0.60f, 0.28f, 0.85f);
            return slateFill;
        }

        private static Color DrawGothicButton(int x, int y, int w, int h)
        {
            int d = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
            int corner = Mathf.Min(x, w - 1 - x) + Mathf.Min(y, h - 1 - y);
            if (corner < 8) return Color.clear;

            Color brass = new Color(0.88f, 0.72f, 0.34f, 1f);
            float grad = Mathf.Lerp(0.82f, 1.0f, (float)y / Mathf.Max(1, h - 1));
            if (d <= 2 || corner <= 10) return brass;
            if (d == 5) return new Color(0.70f, 0.56f, 0.26f, 0.65f);
            return new Color(grad, grad, grad, 1f);
        }

        private static Color DrawOilVialFrame(int x, int y, int w, int h)
        {
            int d = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
            int corner = Mathf.Min(x, w - 1 - x) + Mathf.Min(y, h - 1 - y);
            if (corner < 9) return Color.clear;

            Color brass = new Color(0.86f, 0.68f, 0.30f, 1f);
            if (d <= 3 || corner <= 12) return brass;
            if (d <= 6) return new Color(0.14f, 0.11f, 0.08f, 1f);
            return new Color(0.10f, 0.09f, 0.09f, 0.92f);
        }

        private static Color DrawCircleFill(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float r = Mathf.Min(cx, cy) - 2f;
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            return new Color(1f, 1f, 1f, Mathf.Clamp01((r - d) * 0.6f + 0.5f));
        }

        private static Color DrawCircleRing(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float outer = Mathf.Min(cx, cy) - 2f, inner = outer - 7f;
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            float a = Mathf.Min(Mathf.Clamp01((outer - d) * 0.6f + 0.5f), Mathf.Clamp01((d - inner) * 0.6f + 0.5f));
            return new Color(1f, 1f, 1f, a);
        }

        private static Color DrawGearIcon(int x, int y, int w, int h)
        {
            float nx = (x / (float)(w - 1)) * 2f - 1f;
            float ny = (y / (float)(h - 1)) * 2f - 1f;
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            float ang = Mathf.Atan2(ny, nx);

            float teeth = Mathf.Cos(ang * 8f) > 0.2f ? 0.86f : 0.66f;
            bool inGear = r <= teeth && r >= 0.30f;
            return inGear ? new Color(1f, 0.86f, 0.46f, 1f) : Color.clear;
        }

        private static Color DrawSkullSymbol(int x, int y, int w, int h)
        {
            float nx = (x / (float)(w - 1)) * 2f - 1f;
            float ny = (y / (float)(h - 1)) * 2f - 1f;

            bool cranium = (nx * nx) / 0.42f + ((ny - 0.16f) * (ny - 0.16f)) / 0.36f <= 1f;
            bool jaw = Mathf.Abs(nx) < 0.34f && ny < 0.05f && ny > -0.62f;
            bool leftEye = Vector2.Distance(new Vector2(nx, ny), new Vector2(-0.24f, 0.14f)) < 0.16f;
            bool rightEye = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.24f, 0.14f)) < 0.16f;
            bool nose = Mathf.Abs(nx) < 0.08f && ny < -0.05f && ny > -0.26f;
            bool teethGap = ny < -0.40f && ny > -0.62f && (Mathf.Abs(nx) < 0.03f || Mathf.Abs(Mathf.Abs(nx) - 0.17f) < 0.03f);

            bool filled = (cranium || jaw) && !leftEye && !rightEye && !nose && !teethGap;
            return filled ? new Color(0.95f, 0.88f, 0.68f, 1f) : Color.clear;
        }

        private static Color DrawMoonSymbol(int x, int y, int w, int h)
        {
            float nx = (x / (float)(w - 1)) * 2f - 1f;
            float ny = (y / (float)(h - 1)) * 2f - 1f;

            bool outer = Vector2.Distance(new Vector2(nx, ny), new Vector2(-0.08f, 0f)) < 0.72f;
            bool cutout = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.24f, 0.12f)) < 0.62f;
            return (outer && !cutout) ? new Color(0.95f, 0.88f, 0.68f, 1f) : Color.clear;
        }

        private static Color DrawKeySymbol(int x, int y, int w, int h)
        {
            float nx = (x / (float)(w - 1)) * 2f - 1f;
            float ny = (y / (float)(h - 1)) * 2f - 1f;

            float dBow = Vector2.Distance(new Vector2(nx, ny), new Vector2(0f, 0.42f));
            bool bow = dBow < 0.38f && dBow > 0.18f;
            bool shaft = Mathf.Abs(nx) < 0.10f && ny <= 0.22f && ny >= -0.78f;
            bool tooth1 = nx >= 0.08f && nx <= 0.42f && ny <= -0.34f && ny >= -0.48f;
            bool tooth2 = nx >= 0.08f && nx <= 0.34f && ny <= -0.58f && ny >= -0.72f;

            return (bow || shaft || tooth1 || tooth2) ? new Color(0.95f, 0.88f, 0.68f, 1f) : Color.clear;
        }

        private static Color DrawCrownSymbol(int x, int y, int w, int h)
        {
            float nx = (x / (float)(w - 1)) * 2f - 1f;
            float ny = (y / (float)(h - 1)) * 2f - 1f;

            bool band = Mathf.Abs(nx) < 0.68f && ny >= -0.50f && ny <= -0.18f;
            bool leftPeak = nx >= -0.68f && nx <= -0.22f && ny >= -0.20f && ny <= 0.52f - Mathf.Abs(nx + 0.52f) * 2.2f;
            bool midPeak = Mathf.Abs(nx) <= 0.32f && ny >= -0.20f && ny <= 0.68f - Mathf.Abs(nx) * 2.4f;
            bool rightPeak = nx >= 0.22f && nx <= 0.68f && ny >= -0.20f && ny <= 0.52f - Mathf.Abs(nx - 0.52f) * 2.2f;

            return (band || leftPeak || midPeak || rightPeak) ? new Color(0.95f, 0.88f, 0.68f, 1f) : Color.clear;
        }

        private static Color DrawRedVignette(int x, int y, int w, int h)
        {
            float nx = (x / (float)(w - 1)) * 2f - 1f;
            float ny = (y / (float)(h - 1)) * 2f - 1f;
            float edgeDist = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
            float radial = Mathf.Sqrt(nx * nx + ny * ny) * 0.82f;
            float d = Mathf.Clamp01(Mathf.Lerp(radial, edgeDist, 0.45f));

            // Transparent center, deep crimson danger pulse around the border
            float alpha = Mathf.SmoothStep(0.42f, 1.02f, d) * 0.78f;
            if (alpha <= 0.005f) return Color.clear;
            return new Color(0.76f, 0.04f, 0.04f, alpha);
        }
    }
}
