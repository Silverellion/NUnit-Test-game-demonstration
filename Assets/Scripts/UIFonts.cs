using UnityEngine;
using TMPro;

/// <summary>
/// Centralized font manager for the game, providing high-quality Cinzel SDF fonts,
/// custom materials with crisp outlines and soft shadows, and standard formatting rules.
/// </summary>
public static class UIFonts
{
    private static TMP_FontAsset boldFont;
    private static TMP_FontAsset semiBoldFont;
    private static TMP_FontAsset regularFont;

    private static Material boldMaterial;
    private static Material semiBoldMaterial;
    private static Material regularMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        boldFont = null;
        semiBoldFont = null;
        regularFont = null;
        boldMaterial = null;
        semiBoldMaterial = null;
        regularMaterial = null;
    }

    public static TMP_FontAsset Bold
    {
        get
        {
            if (boldFont == null)
                boldFont = LoadFont("Cinzel-Bold SDF", "Assets/Fonts/Cinzel/Cinzel-Bold SDF.asset");
            return boldFont;
        }
    }

    public static TMP_FontAsset SemiBold
    {
        get
        {
            if (semiBoldFont == null)
                semiBoldFont = LoadFont("Cinzel-SemiBold SDF", "Assets/Fonts/Cinzel/Cinzel-SemiBold SDF.asset");
            return semiBoldFont;
        }
    }

    public static TMP_FontAsset Regular
    {
        get
        {
            if (regularFont == null)
                regularFont = LoadFont("Cinzel-Regular SDF", "Assets/Fonts/Cinzel/Cinzel-Regular SDF.asset");
            return regularFont;
        }
    }

    public static Material BoldMaterial
    {
        get
        {
            if ((boldMaterial == null || boldMaterial.shader == null) && Bold != null)
                boldMaterial = CreateStyledMaterial(Bold, "Cinzel-Bold SDF Material (Styled)");
            return boldMaterial != null ? boldMaterial : (Bold != null ? Bold.material : null);
        }
    }

    public static Material SemiBoldMaterial
    {
        get
        {
            if ((semiBoldMaterial == null || semiBoldMaterial.shader == null) && SemiBold != null)
                semiBoldMaterial = CreateStyledMaterial(SemiBold, "Cinzel-SemiBold SDF Material (Styled)");
            return semiBoldMaterial != null ? semiBoldMaterial : (SemiBold != null ? SemiBold.material : null);
        }
    }

    public static Material RegularMaterial
    {
        get
        {
            if ((regularMaterial == null || regularMaterial.shader == null) && Regular != null)
                regularMaterial = CreateStyledMaterial(Regular, "Cinzel-Regular SDF Material (Styled)");
            return regularMaterial != null ? regularMaterial : (Regular != null ? Regular.material : null);
        }
    }

    private static TMP_FontAsset LoadFont(string resourceName, string assetPath)
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>($"Fonts/{resourceName}");
#if UNITY_EDITOR
        if (font == null)
        {
            font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        }
        if (font == null)
        {
            // If asset not yet generated, try alternate locations or fallback
            font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"Assets/Resources/Fonts/{resourceName}.asset");
        }
#endif
        if (font == null)
        {
            // Graceful fallback to default font so nothing breaks
            font = CombatSceneBootstrapper.GetDefaultFont();
        }
        return font;
    }

    private static Material CreateStyledMaterial(TMP_FontAsset fontAsset, string name)
    {
        if (fontAsset == null || fontAsset.material == null) return null;

        Material mat = new Material(fontAsset.material);
        mat.name = name;

        // Thin dark outline
        mat.EnableKeyword("OUTLINE_ON");
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.12f);
        mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.05f, 0.05f, 0.08f, 0.95f));

        // Soft drop shadow
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.08f);
        mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.35f);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.35f);
        mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.70f));

        return mat;
    }

    /// <summary>
    /// Applies Cinzel Bold to high-emphasis titles ("YOUR TURN", "END TURN", card titles).
    /// </summary>
    public static void ApplyTitle(TextMeshProUGUI tmp, bool autoSize = false, float minSize = 12f, float maxSize = 24f)
    {
        if (tmp == null) return;
        if (Bold != null) tmp.font = Bold;
        if (BoldMaterial != null) tmp.fontSharedMaterial = BoldMaterial;

        tmp.enableAutoSizing = autoSize;
        if (autoSize)
        {
            tmp.fontSizeMin = minSize;
            tmp.fontSizeMax = maxSize;
        }
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.extraPadding = true;
    }

    /// <summary>
    /// Applies Cinzel SemiBold or Regular to body text, character names, HP/mana, intent badges, etc.
    /// Enables auto-size and word wrapping to accommodate Cinzel's wide small-caps glyphs.
    /// </summary>
    public static void ApplyBody(TextMeshProUGUI tmp, bool semiBold = true, bool autoSize = false, float minSize = 10f, float maxSize = 20f)
    {
        if (tmp == null) return;
        TMP_FontAsset font = semiBold ? SemiBold : Regular;
        Material mat = semiBold ? SemiBoldMaterial : RegularMaterial;

        if (font != null) tmp.font = font;
        if (mat != null) tmp.fontSharedMaterial = mat;

        tmp.enableAutoSizing = autoSize;
        if (autoSize)
        {
            tmp.fontSizeMin = minSize;
            tmp.fontSizeMax = maxSize;
        }
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.extraPadding = true;
    }
}
