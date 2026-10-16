#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

[InitializeOnLoad]
public static class CinzelFontAssetCreator
{
    private const string CINZEL_DIR = "Assets/Fonts/Cinzel";
    private const string CINZEL_STATIC_DIR = "Assets/Fonts/Cinzel/static";

    // ASCII plus common punctuation
    private const string CHARACTER_SET = 
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~—–…°•¡¿";

    static CinzelFontAssetCreator()
    {
        EditorApplication.delayCall += EnsureFontAssetsExist;
    }

    public static void EnsureFontAssetsExist()
    {
        string boldPath = Path.Combine(CINZEL_DIR, "Cinzel-Bold SDF.asset").Replace("\\", "/");
        string semiBoldPath = Path.Combine(CINZEL_DIR, "Cinzel-SemiBold SDF.asset").Replace("\\", "/");
        string regularPath = Path.Combine(CINZEL_DIR, "Cinzel-Regular SDF.asset").Replace("\\", "/");

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(boldPath) == null ||
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(semiBoldPath) == null ||
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(regularPath) == null)
        {
            Debug.Log("[CinzelFontAssetCreator] Missing Cinzel SDF font assets. Auto-generating now...");
            GenerateCinzelFontAssets();
        }
    }

    [MenuItem("Tools/Generate Cinzel Font Assets")]
    public static void GenerateCinzelFontAssets()
    {
        GenerateFontAsset("Cinzel-Bold.ttf", "Cinzel-Bold SDF.asset", true);
        GenerateFontAsset("Cinzel-SemiBold.ttf", "Cinzel-SemiBold SDF.asset", false);
        GenerateFontAsset("Cinzel-Regular.ttf", "Cinzel-Regular SDF.asset", false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CinzelFontAssetCreator] Cinzel SDF font assets successfully generated!");
    }

    private static void GenerateFontAsset(string ttfFileName, string outputAssetName, bool isBold)
    {
        string ttfPath = Path.Combine(CINZEL_STATIC_DIR, ttfFileName).Replace("\\", "/");
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (sourceFont == null)
        {
            Debug.LogError($"[CinzelFontAssetCreator] Could not load TTF at path: {ttfPath}");
            return;
        }

        string outputPath = Path.Combine(CINZEL_DIR, outputAssetName).Replace("\\", "/");

        // Sampling size 64, padding 6, SDFAA, 2048x2048, Dynamic (or Static via TryAddCharacters)
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            64,
            6,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic,
            true
        );

        if (fontAsset == null)
        {
            Debug.LogError($"[CinzelFontAssetCreator] Failed to create font asset for {ttfFileName}");
            return;
        }

        // Pre-populate glyphs for all ASCII + common punctuation
        fontAsset.TryAddCharacters(CHARACTER_SET);

        // Bilinear filtering on atlas texture
        if (fontAsset.atlasTexture != null)
        {
            fontAsset.atlasTexture.filterMode = FilterMode.Bilinear;
        }

        // Configure Material with thin dark outline and soft shadow
        Material mat = fontAsset.material;
        if (mat != null)
        {
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.12f);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.06f, 0.06f, 0.09f, 0.95f));

            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.08f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.35f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.35f);
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.70f));
        }

        // Check if existing asset needs to be overwritten or created
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(outputPath);
        }

        AssetDatabase.CreateAsset(fontAsset, outputPath);

        // Sub-assets: material and atlas texture
        if (fontAsset.material != null)
        {
            fontAsset.material.name = outputAssetName.Replace(".asset", " Material");
            AssetDatabase.AddObjectToAsset(fontAsset.material, outputPath);
        }

        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                if (fontAsset.atlasTextures[i] != null)
                {
                    fontAsset.atlasTextures[i].name = outputAssetName.Replace(".asset", $" Atlas {i}");
                    fontAsset.atlasTextures[i].filterMode = FilterMode.Bilinear;
                    AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], outputPath);
                }
            }
        }

        EditorUtility.SetDirty(fontAsset);
        Debug.Log($"[CinzelFontAssetCreator] Created font asset at: {outputPath}");
    }
}
#endif
