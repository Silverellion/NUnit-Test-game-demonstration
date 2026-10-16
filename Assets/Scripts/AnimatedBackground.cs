using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates a UI Image by cycling through sprite frames at a specified frame rate.
/// Used for the animated town background (Assets/Assets/Background/Animated/animated_town.gif).
/// Loads extracted frames and enforces Point filtering for razor-sharp pixel art.
/// </summary>
[RequireComponent(typeof(Image))]
public class AnimatedBackground : MonoBehaviour
{
    public Image backgroundImage;
    public Sprite[] frames;
    public float fps = 10f;
    public bool isLooping = true;

    private int currentFrameIndex = 0;
    private float timer = 0f;

    void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (frames == null || frames.Length == 0 || frames[0] == null || frames[0].texture == null)
        {
            LoadFrames();
        }
    }

    /// <summary>
    /// Loads the 70 extracted background frames from Assets/Assets/Background/Animated/Frames.
    /// Supports both Unity Editor AssetDatabase loading and runtime/standalone byte decoding.
    /// </summary>
    public void LoadFrames()
    {
        List<Sprite> loaded = new List<Sprite>();
        string framesDir = "Assets/Assets/Background/Animated/Frames";

#if UNITY_EDITOR
        for (int i = 0; i < 70; i++)
        {
            string path = $"{framesDir}/frame_{i:D2}.png";
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets != null)
            {
                foreach (var a in assets)
                {
                    if (a is Sprite s)
                    {
                        if (s.texture != null && s.texture.filterMode != FilterMode.Point)
                        {
                            s.texture.filterMode = FilterMode.Point;
                        }
                        loaded.Add(s);
                        break;
                    }
                }
            }
        }
#endif

        // Direct PNG bytes decoding fallback for runtime, tests, or before AssetDatabase reimport
        if (loaded.Count < 70 && Directory.Exists(framesDir))
        {
            loaded.Clear();
            for (int i = 0; i < 70; i++)
            {
                string path = Path.Combine(framesDir, $"frame_{i:D2}.png");
                if (File.Exists(path))
                {
                    byte[] data = File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp,
                        name = $"animated_town_{i:D2}"
                    };
                    if (tex.LoadImage(data))
                    {
                        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                        loaded.Add(s);
                    }
                }
            }
        }

        frames = loaded.ToArray();
        if (frames.Length > 0 && backgroundImage != null)
        {
            backgroundImage.sprite = frames[0];
            backgroundImage.color = Color.white;
        }
    }

    void Update()
    {
        if (frames == null || frames.Length == 0 || backgroundImage == null) return;

        timer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(1f, fps);

        if (timer >= frameDuration)
        {
            int framesToAdvance = Mathf.FloorToInt(timer / frameDuration);
            timer -= framesToAdvance * frameDuration;

            if (isLooping)
            {
                currentFrameIndex = (currentFrameIndex + framesToAdvance) % frames.Length;
            }
            else
            {
                currentFrameIndex = Mathf.Min(currentFrameIndex + framesToAdvance, frames.Length - 1);
            }

            backgroundImage.sprite = frames[currentFrameIndex];
        }
    }
}
