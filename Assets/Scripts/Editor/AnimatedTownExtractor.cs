#if UNITY_EDITOR
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using UnityEditor;
using UnityEngine;

public static class AnimatedTownExtractor
{
    public const string GIF_PATH = "Assets/Assets/Background/Animated/animated_town.gif";
    public const string FRAMES_DIR = "Assets/Assets/Background/Animated/Frames";

    [MenuItem("Tools/Extract Background GIF Frames")]
    public static void ExtractFrames()
    {
        if (!File.Exists(GIF_PATH))
        {
            Debug.LogWarning($"[AnimatedTownExtractor] GIF not found at: {GIF_PATH}");
            return;
        }

        if (!Directory.Exists(FRAMES_DIR))
        {
            Directory.CreateDirectory(FRAMES_DIR);
        }

        string[] existingFrames = Directory.GetFiles(FRAMES_DIR, "frame_*.png");
        if (existingFrames.Length >= 70)
        {
            // Already extracted
            return;
        }

        try
        {
            using (var img = System.Drawing.Image.FromFile(GIF_PATH))
            {
                var dimension = new FrameDimension(img.FrameDimensionsList[0]);
                int frameCount = img.GetFrameCount(dimension);

                for (int i = 0; i < frameCount; i++)
                {
                    img.SelectActiveFrame(dimension, i);
                    string outPath = Path.Combine(FRAMES_DIR, $"frame_{i:D2}.png");
                    img.Save(outPath, ImageFormat.Png);
                }

                Debug.Log($"[AnimatedTownExtractor] Successfully extracted {frameCount} frames to {FRAMES_DIR}");
            }

            AssetDatabase.Refresh();

            // Set importer settings to Sprite and Point filter
            for (int i = 0; i < 70; i++)
            {
                string assetPath = $"{FRAMES_DIR}/frame_{i:D2}.png";
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AnimatedTownExtractor] Failed to extract frames: {ex.Message}");
        }
    }
}
#endif
