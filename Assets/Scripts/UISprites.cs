using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Generates procedural, anti-aliased 9-slice sprites with rounded corners, borders,
/// gradients, and soft drop shadows. Fully cached to prevent redundant allocations.
/// </summary>
public static class UISprites
{
    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetCache()
    {
        spriteCache.Clear();
    }

    private static bool TryGetCached(string key, out Sprite sprite)
    {
        if (spriteCache.TryGetValue(key, out sprite) && sprite != null && sprite.texture != null)
        {
            return true;
        }
        sprite = null;
        return false;
    }

    // ─── HIGH-LEVEL CARD & UI PRESETS ───

    /// <summary>
    /// Dark navy card stock with subtle top-to-bottom slate gradient.
    /// </summary>
    public static Sprite CardBackground => GetRoundedRect(
        "CardBackground", 128, 128, 22f,
        fillBottom: new Color(0.06f, 0.08f, 0.12f, 0.98f),
        fillTop: new Color(0.11f, 0.14f, 0.20f, 0.98f),
        borderWidth: 0f, borderColor: Color.clear
    );

    /// <summary>
    /// Outer metallic warm gold border for playable cards.
    /// </summary>
    public static Sprite CardBorderPlayable => GetRoundedRect(
        "CardBorderPlayable", 128, 128, 24f,
        fillBottom: Color.clear, fillTop: Color.clear,
        borderWidth: 3.5f,
        borderColor: new Color(0.92f, 0.78f, 0.35f, 1f) // Radiant gold
    );

    /// <summary>
    /// Weathered dark iron border for unplayable cards.
    /// </summary>
    public static Sprite CardBorderUnplayable => GetRoundedRect(
        "CardBorderUnplayable", 128, 128, 24f,
        fillBottom: Color.clear, fillTop: Color.clear,
        borderWidth: 3.0f,
        borderColor: new Color(0.32f, 0.34f, 0.40f, 0.75f)
    );

    /// <summary>
    /// Card type header bar with rounded top corners and subtle gradient.
    /// </summary>
    public static Sprite GetCardHeader(string cardType)
    {
        string key = $"CardHeader_{cardType}";
        if (TryGetCached(key, out Sprite s)) return s;

        Color btm, top, border;
        switch (cardType.ToUpperInvariant())
        {
            case "ATTACK":
                btm = new Color(0.50f, 0.12f, 0.14f, 1f);
                top = new Color(0.72f, 0.20f, 0.22f, 1f);
                border = new Color(0.95f, 0.40f, 0.35f, 0.6f);
                break;
            case "SKILL":
            case "DEFEND":
                btm = new Color(0.12f, 0.26f, 0.48f, 1f);
                top = new Color(0.18f, 0.42f, 0.70f, 1f);
                border = new Color(0.45f, 0.70f, 0.95f, 0.6f);
                break;
            default: // Utility / Power
                btm = new Color(0.12f, 0.38f, 0.22f, 1f);
                top = new Color(0.18f, 0.54f, 0.32f, 1f);
                border = new Color(0.45f, 0.85f, 0.55f, 0.6f);
                break;
        }

        s = GenerateTopRoundedRect(key, 128, 64, 20f, btm, top, 2f, border);
        spriteCache[key] = s;
        return s;
    }

    /// <summary>
    /// Recessed frame for card artwork with smooth rounded corners.
    /// </summary>
    public static Sprite ArtworkFrame => GetRoundedRect(
        "ArtworkFrame", 128, 128, 14f,
        fillBottom: new Color(0.04f, 0.05f, 0.08f, 1f),
        fillTop: new Color(0.08f, 0.09f, 0.14f, 1f),
        borderWidth: 2f,
        borderColor: new Color(0.30f, 0.32f, 0.42f, 0.70f)
    );

    /// <summary>
    /// Energy cost circle badge (bright electric gem with cyan border ring).
    /// </summary>
    public static Sprite CostBadgePlayable => GetCircle(
        "CostBadgePlayable", 64,
        fillBottom: new Color(0.08f, 0.38f, 0.80f, 1f),
        fillTop: new Color(0.20f, 0.65f, 0.98f, 1f),
        borderWidth: 3f,
        borderColor: new Color(0.70f, 0.90f, 1f, 0.95f)
    );

    public static Sprite CostBadgeUnplayable => GetCircle(
        "CostBadgeUnplayable", 64,
        fillBottom: new Color(0.45f, 0.12f, 0.14f, 1f),
        fillTop: new Color(0.60f, 0.18f, 0.22f, 1f),
        borderWidth: 2.5f,
        borderColor: new Color(0.65f, 0.35f, 0.35f, 0.75f)
    );

    /// <summary>
    /// Pill shape for END TURN button (rich crimson with warm gold border).
    /// </summary>
    public static Sprite EndTurnButton => GetPill(
        "EndTurnButton", 160, 64,
        fillBottom: new Color(0.55f, 0.14f, 0.15f, 1f),
        fillTop: new Color(0.78f, 0.24f, 0.22f, 1f),
        borderWidth: 3f,
        borderColor: new Color(0.95f, 0.82f, 0.40f, 0.95f)
    );

    public static Sprite PlayAgainButton => GetPill(
        "PlayAgainButton", 160, 64,
        fillBottom: new Color(0.12f, 0.48f, 0.25f, 1f),
        fillTop: new Color(0.22f, 0.68f, 0.36f, 1f),
        borderWidth: 3f,
        borderColor: new Color(0.85f, 0.95f, 0.80f, 0.90f)
    );

    /// <summary>
    /// Intent badge pill (Red for attack, Blue for defend).
    /// </summary>
    public static Sprite IntentBadgeAttack => GetPill(
        "IntentBadgeAttack", 128, 52,
        fillBottom: new Color(0.60f, 0.12f, 0.15f, 0.95f),
        fillTop: new Color(0.82f, 0.22f, 0.25f, 0.95f),
        borderWidth: 2.5f,
        borderColor: new Color(1f, 0.55f, 0.55f, 0.90f)
    );

    public static Sprite IntentBadgeDefend => GetPill(
        "IntentBadgeDefend", 128, 52,
        fillBottom: new Color(0.12f, 0.30f, 0.65f, 0.95f),
        fillTop: new Color(0.20f, 0.50f, 0.88f, 0.95f),
        borderWidth: 2.5f,
        borderColor: new Color(0.55f, 0.80f, 1f, 0.90f)
    );

    /// <summary>
    /// Block badge pill with bright azure border.
    /// </summary>
    public static Sprite BlockBadge => GetPill(
        "BlockBadge", 96, 52,
        fillBottom: new Color(0.10f, 0.32f, 0.68f, 0.98f),
        fillTop: new Color(0.18f, 0.50f, 0.92f, 0.98f),
        borderWidth: 2.5f,
        borderColor: new Color(0.65f, 0.88f, 1f, 0.95f)
    );

    /// <summary>
    /// Draw pile counter badge pill.
    /// </summary>
    public static Sprite DrawPileBadge => GetRoundedRect(
        "DrawPileBadge", 128, 80, 16f,
        fillBottom: new Color(0.09f, 0.18f, 0.32f, 0.95f),
        fillTop: new Color(0.14f, 0.28f, 0.48f, 0.95f),
        borderWidth: 2f,
        borderColor: new Color(0.40f, 0.65f, 0.95f, 0.75f)
    );

    /// <summary>
    /// Discard pile counter badge pill.
    /// </summary>
    public static Sprite DiscardPileBadge => GetRoundedRect(
        "DiscardPileBadge", 128, 80, 16f,
        fillBottom: new Color(0.32f, 0.14f, 0.12f, 0.95f),
        fillTop: new Color(0.48f, 0.22f, 0.18f, 0.95f),
        borderWidth: 2f,
        borderColor: new Color(0.90f, 0.55f, 0.45f, 0.75f)
    );

    /// <summary>
    /// Name plate background with rounded corners and subtle dark gradient.
    /// </summary>
    public static Sprite NamePlate => GetRoundedRect(
        "NamePlate", 128, 48, 12f,
        fillBottom: new Color(0.06f, 0.07f, 0.10f, 0.88f),
        fillTop: new Color(0.12f, 0.14f, 0.20f, 0.88f),
        borderWidth: 1.5f,
        borderColor: new Color(0.55f, 0.50f, 0.40f, 0.55f)
    );

    /// <summary>
    /// HP Bar track background (rounded ends).
    /// </summary>
    public static Sprite BarTrack => GetPill(
        "BarTrack", 128, 32,
        fillBottom: new Color(0.07f, 0.08f, 0.11f, 0.95f),
        fillTop: new Color(0.12f, 0.13f, 0.18f, 0.95f),
        borderWidth: 1.5f,
        borderColor: new Color(0.25f, 0.27f, 0.35f, 0.75f)
    );

    /// <summary>
    /// HP Bar fill sprite (smooth rounded pill fill).
    /// </summary>
    public static Sprite BarFill => GetPill(
        "BarFill", 64, 32,
        fillBottom: Color.white, fillTop: Color.white,
        borderWidth: 0f, borderColor: Color.clear
    );

    /// <summary>
    /// Bottom hand panel (dark navy with rounded top corners, gradient, and top gold highlight).
    /// </summary>
    public static Sprite HandPanel => GetTopRoundedPanel(
        "HandPanel", 128, 128, 28f,
        bottomColor: new Color(0.04f, 0.05f, 0.08f, 0.98f),
        topColor: new Color(0.09f, 0.11f, 0.17f, 0.98f),
        topHighlightWidth: 2.5f,
        topHighlightColor: new Color(0.85f, 0.70f, 0.32f, 0.85f)
    );

    /// <summary>
    /// Elegant divider line with soft fade on edges.
    /// </summary>
    public static Sprite DividerLine => GetDividerLine(
        "DividerLine", 256, 16,
        centerColor: new Color(0.75f, 0.62f, 0.32f, 0.75f),
        edgeColor: new Color(0.75f, 0.62f, 0.32f, 0f)
    );

    /// <summary>
    /// Soft gaussian-style feathered drop shadow for cards and dialog boxes.
    /// </summary>
    public static Sprite SoftShadow => GetSoftShadow(
        "SoftShadow", 128, 128, 24f, 22f,
        new Color(0f, 0f, 0f, 0.65f)
    );

    /// <summary>
    /// Rounded rectangular target outline frame for highlighted combat targets.
    /// Anti-aliased SDF with rounded corners and crisp border.
    /// Tinted blue for player, red for enemy.
    /// </summary>
    public static Sprite TargetOutlineFrame => GetRoundedRect(
        "TargetOutlineFrame", 128, 128, 16f,
        fillBottom: Color.clear, fillTop: Color.clear,
        borderWidth: 4.5f,
        borderColor: Color.white
    );

    /// <summary>
    /// Vertical gradient fading from completely transparent at top (y=1) to solid opaque black at bottom (y=0).
    /// Uses a smooth cubic ease-in S-curve to ensure a completely seamless, banding-free transition.
    /// </summary>
    public static Sprite BlackFadeGradient
    {
        get
        {
            string key = "BlackFadeGradient";
            if (TryGetCached(key, out Sprite cached)) return cached;

            int w = 32;
            int h = 256;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = key
            };

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, t);
                Color c = new Color(0f, 0f, 0f, alpha);
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            s.name = key;
            spriteCache[key] = s;
            return s;
        }
    }

    // ─── CARD ARTWORK & ICON LOADERS ───

    public static Sprite GetCardArtwork(string cardName)
    {
        string path = null;
        if (cardName.IndexOf("heavy", StringComparison.OrdinalIgnoreCase) >= 0)
            path = "Assets/Assets/Cards/heavy_strike.png";
        else if (cardName.IndexOf("strike", StringComparison.OrdinalIgnoreCase) >= 0)
            path = "Assets/Assets/Cards/Sword.png";
        else if (cardName.IndexOf("defend", StringComparison.OrdinalIgnoreCase) >= 0)
            path = "Assets/Assets/Cards/shield.png";
        else if (cardName.IndexOf("strategize", StringComparison.OrdinalIgnoreCase) >= 0)
            path = "Assets/Assets/Cards/strategize.png";

        if (!string.IsNullOrEmpty(path))
        {
            Sprite s = LoadSpriteAtPath(path);
            if (s != null) return s;
        }

        // Fallback default artwork placeholder
        return ArtworkFrame;
    }

    public static Sprite GetSwordIcon()
    {
        return LoadSpriteAtPath("Assets/Assets/AdventureGameIconPack/280x280/Tools/Sword.png");
    }

    public static Sprite GetShieldIcon()
    {
        return LoadSpriteAtPath("Assets/Assets/AdventureGameIconPack/280x280/Tools/Shield.png");
    }

    private static Sprite LoadSpriteAtPath(string path)
    {
#if UNITY_EDITOR
        var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets != null)
        {
            var sprite = assets.OfType<Sprite>().FirstOrDefault();
            if (sprite != null) return sprite;
        }
#endif
        return null;
    }

    // ─── PROCEDURAL GENERATOR CORE (Anti-Aliased SDF) ───

    public static Sprite GetRoundedRect(string key, int w, int h, float r, Color fillBottom, Color fillTop, float borderWidth = 0f, Color borderColor = default)
    {
        if (TryGetCached(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = key
        };

        Color[] pixels = new Color[w * h];
        float hw = w * 0.5f - 0.5f;
        float hh = h * 0.5f - 0.5f;
        float bx = hw - r;
        float by = hh - r;

        for (int y = 0; y < h; y++)
        {
            float py = y - hh;
            float t = (float)y / (h - 1);
            Color fillColor = Color.Lerp(fillBottom, fillTop, t);

            for (int x = 0; x < w; x++)
            {
                float px = x - hw;
                float qx = Mathf.Abs(px) - bx;
                float qy = Mathf.Abs(py) - by;
                float ax = Mathf.Max(qx, 0f);
                float ay = Mathf.Max(qy, 0f);
                float dist = Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;

                float outerAlpha = Mathf.Clamp01(0.5f - dist);

                if (borderWidth > 0.01f)
                {
                    float distInner = dist + borderWidth;
                    float innerAlpha = Mathf.Clamp01(0.5f - distInner);
                    float borderAlpha = Mathf.Clamp01(outerAlpha - innerAlpha);

                    Color c = borderColor * (borderColor.a * borderAlpha) + fillColor * (fillColor.a * innerAlpha);
                    c.a = Mathf.Max(borderColor.a * borderAlpha, fillColor.a * innerAlpha);
                    pixels[y * w + x] = c;
                }
                else
                {
                    Color c = fillColor;
                    c.a *= outerAlpha;
                    pixels[y * w + x] = c;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        float slice = Mathf.Min(r + borderWidth + 4f, Mathf.Min(w, h) * 0.45f);
        Vector4 border = new Vector4(slice, slice, slice, slice);
        Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        s.name = key;
        spriteCache[key] = s;
        return s;
    }

    public static Sprite GetPill(string key, int w, int h, Color fillBottom, Color fillTop, float borderWidth = 0f, Color borderColor = default)
    {
        return GetRoundedRect(key, w, h, (h * 0.5f) - 1f, fillBottom, fillTop, borderWidth, borderColor);
    }

    public static Sprite GetCircle(string key, int size, Color fillBottom, Color fillTop, float borderWidth = 0f, Color borderColor = default)
    {
        if (TryGetCached(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = key
        };

        Color[] pixels = new Color[size * size];
        float radius = size * 0.5f - 1f;
        float center = size * 0.5f - 0.5f;

        for (int y = 0; y < size; y++)
        {
            float py = y - center;
            float t = (float)y / (size - 1);
            Color fillColor = Color.Lerp(fillBottom, fillTop, t);

            for (int x = 0; x < size; x++)
            {
                float px = x - center;
                float dist = Mathf.Sqrt(px * px + py * py) - radius;
                float outerAlpha = Mathf.Clamp01(0.5f - dist);

                if (borderWidth > 0.01f)
                {
                    float distInner = dist + borderWidth;
                    float innerAlpha = Mathf.Clamp01(0.5f - distInner);
                    float borderAlpha = Mathf.Clamp01(outerAlpha - innerAlpha);

                    Color c = borderColor * (borderColor.a * borderAlpha) + fillColor * (fillColor.a * innerAlpha);
                    c.a = Mathf.Max(borderColor.a * borderAlpha, fillColor.a * innerAlpha);
                    pixels[y * size + x] = c;
                }
                else
                {
                    Color c = fillColor;
                    c.a *= outerAlpha;
                    pixels[y * size + x] = c;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        s.name = key;
        spriteCache[key] = s;
        return s;
    }

    public static Sprite GetTopRoundedPanel(string key, int w, int h, float r, Color bottomColor, Color topColor, float topHighlightWidth, Color topHighlightColor)
    {
        if (TryGetCached(key, out Sprite cached)) return cached;
        return GenerateTopRoundedRect(key, w, h, r, bottomColor, topColor, topHighlightWidth, topHighlightColor);
    }

    private static Sprite GenerateTopRoundedRect(string key, int w, int h, float r, Color bottomColor, Color topColor, float topHighlightWidth, Color topHighlightColor)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = key
        };

        Color[] pixels = new Color[w * h];
        float hw = w * 0.5f - 0.5f;
        float hh = h * 0.5f - 0.5f;
        float bx = hw - r;
        float by = hh - r;

        for (int y = 0; y < h; y++)
        {
            float py = y - hh;
            float t = (float)y / (h - 1);
            Color fill = Color.Lerp(bottomColor, topColor, t);

            for (int x = 0; x < w; x++)
            {
                float px = x - hw;
                float dist;

                if (py > 0) // Top half: rounded corners
                {
                    float qx = Mathf.Abs(px) - bx;
                    float qy = py - by;
                    float ax = Mathf.Max(qx, 0f);
                    float ay = Mathf.Max(qy, 0f);
                    dist = Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                }
                else // Bottom half: flat rectangular edge
                {
                    float qx = Mathf.Abs(px) - hw;
                    float qy = -py - hh;
                    dist = Mathf.Max(qx, qy);
                }

                float alpha = Mathf.Clamp01(0.5f - dist);

                // Top rim highlight
                if (topHighlightWidth > 0f && py > hh - topHighlightWidth && alpha > 0.01f)
                {
                    float hlFactor = Mathf.Clamp01((py - (hh - topHighlightWidth)) / topHighlightWidth);
                    fill = Color.Lerp(fill, topHighlightColor, hlFactor * topHighlightColor.a);
                }

                fill.a *= alpha;
                pixels[y * w + x] = fill;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        float slice = Mathf.Min(r + 6f, Mathf.Min(w, h) * 0.45f);
        Vector4 border = new Vector4(slice, 4f, slice, slice);
        Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        s.name = key;
        spriteCache[key] = s;
        return s;
    }

    public static Sprite GetDividerLine(string key, int w, int h, Color centerColor, Color edgeColor)
    {
        if (TryGetCached(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = key
        };

        Color[] pixels = new Color[w * h];
        float hw = w * 0.5f;
        float hh = h * 0.5f;

        for (int y = 0; y < h; y++)
        {
            float vertFade = 1f - Mathf.Abs((y - hh) / hh);
            vertFade = Mathf.Clamp01(vertFade);

            for (int x = 0; x < w; x++)
            {
                float horizFade = 1f - Mathf.Abs((x - hw) / hw);
                horizFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(horizFade));

                Color c = Color.Lerp(edgeColor, centerColor, horizFade);
                c.a *= vertFade;
                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        s.name = key;
        spriteCache[key] = s;
        return s;
    }

    public static Sprite GetSoftShadow(string key, int w, int h, float r, float blur, Color shadowColor)
    {
        if (TryGetCached(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = key
        };

        Color[] pixels = new Color[w * h];
        float hw = w * 0.5f - blur;
        float hh = h * 0.5f - blur;
        float bx = Mathf.Max(0f, hw - r);
        float by = Mathf.Max(0f, hh - r);

        for (int y = 0; y < h; y++)
        {
            float py = y - (h * 0.5f);
            for (int x = 0; x < w; x++)
            {
                float px = x - (w * 0.5f);
                float qx = Mathf.Abs(px) - bx;
                float qy = Mathf.Abs(py) - by;
                float ax = Mathf.Max(qx, 0f);
                float ay = Mathf.Max(qy, 0f);
                float dist = Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;

                float falloff = 0f;
                if (dist <= 0f)
                {
                    falloff = 1f;
                }
                else if (dist < blur)
                {
                    float factor = 1f - (dist / blur);
                    falloff = factor * factor * (3f - 2f * factor); // Smoothstep falloff
                }

                Color c = shadowColor;
                c.a *= falloff;
                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        float slice = Mathf.Min(r + blur, Mathf.Min(w, h) * 0.45f);
        Vector4 border = new Vector4(slice, slice, slice, slice);
        Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        s.name = key;
        spriteCache[key] = s;
        return s;
    }
}
