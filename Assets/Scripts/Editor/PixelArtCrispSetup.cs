#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Ensures all 2D pixel-art character sprites in Assets/Assets are imported
/// with FilterMode.Point and Uncompressed format, making them crystal clear and crisp
/// instead of blurry or smudged.
/// </summary>
[InitializeOnLoad]
public static class PixelArtCrispSetup
{
    static PixelArtCrispSetup()
    {
        EditorApplication.delayCall += MakePixelArtCrisp;
    }

    [MenuItem("Tools/Make Pixel Art Crisp & Sharp")]
    public static void MakePixelArtCrisp()
    {
        // Only target pixel-art character sprite and background folders
        string[] pixelArtFolders = new[]
        {
            "Assets/Assets/Soldier with shadows",
            "Assets/Assets/Orc with shadows",
            "Assets/Assets/Background"
        };

        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", pixelArtFolders);
        int updatedCount = 0;

        foreach (string guid in textureGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            // Never apply Point filtering to font atlases, smooth UI, or card art
            if (path.IndexOf("Font", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("Atlas", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("Card", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("UI", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.filterMode != FilterMode.Point)
                {
                    importer.filterMode = FilterMode.Point;
                    dirty = true;
                }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                    updatedCount++;
                }
            }
        }

        if (updatedCount > 0)
        {
            Debug.Log($"[PixelArtCrispSetup] Updated {updatedCount} pixel-art textures to FilterMode.Point (Crisp & Sharp).");
        }

        // Ensure smooth UI, card art, and icons use Bilinear filtering
        string[] smoothFolders = new[]
        {
            "Assets/Assets/Cards",
            "Assets/Assets/AdventureGameIconPack"
        };

        string[] smoothGuids = AssetDatabase.FindAssets("t:Texture2D", smoothFolders);
        int smoothUpdatedCount = 0;

        foreach (string guid in smoothGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
                smoothUpdatedCount++;
            }
        }

        if (smoothUpdatedCount > 0)
        {
            Debug.Log($"[PixelArtCrispSetup] Updated {smoothUpdatedCount} smooth UI/card textures to FilterMode.Bilinear.");
        }
    }
}
#endif
