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
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Assets" });
        int updatedCount = 0;

        foreach (string guid in textureGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
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
            Debug.Log($"[PixelArtCrispSetup] Updated {updatedCount} textures to FilterMode.Point (Crisp & Sharp).");
        }
    }
}
#endif
