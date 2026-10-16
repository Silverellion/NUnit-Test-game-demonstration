using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls frame-by-frame sprite animations (Idle, Walk, Attack, Hurt, Death)
/// and combat arena movement (walking up to opponent, striking, walking back).
/// </summary>
public class CharacterCombatVisual : MonoBehaviour
{
    [Header("Sprite Frames")]
    public Sprite[] idleFrames;
    public Sprite[] walkFrames;
    public Sprite[] attack01Frames;
    public Sprite[] attack02Frames;
    public Sprite[] hurtFrames;
    public Sprite[] deathFrames;

    [Header("Animation Speeds")]
    public float idleFps = 8f;
    public float walkFps = 10f;
    public float attackFps = 12f;
    public float hurtFps = 8f;
    public float deathFps = 8f;

    [Header("Components")]
    public Image characterImage;
    public RectTransform rectTransform;

    [Header("Targeting Feedback")]
    public GameObject targetIndicator;

    private Coroutine loopCoroutine;
    private Coroutine actionCoroutine;
    private Vector2 homePosition;
    private Vector3 originalScale = Vector3.one;
    private bool isDead = false;
    public bool baseFacingRight = true;
    private bool isDirectHover = false;

    private const float PIXEL_SCALE = 8f; // 8x crisp pixel scaling for 1600x900 resolution

    void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (characterImage == null)
            characterImage = GetComponent<Image>();
        if (rectTransform != null)
        {
            rectTransform.pivot = new Vector2(0.5f, 0f); // Feet anchored to ground
            homePosition = rectTransform.anchoredPosition;
            originalScale = Vector3.one;
        }
    }

    /// <summary>
    /// Applies a sprite frame while strictly preserving its pixel-scale ratio,
    /// ensuring characters NEVER shrink or stretch during attack or hurt animations.
    /// Also enforces FilterMode.Point for razor-sharp pixel art.
    /// </summary>
    public void ApplySpriteFrame(Sprite s)
    {
        if (characterImage == null || s == null) return;

        characterImage.sprite = s;
        if (s.texture != null && s.texture.filterMode != FilterMode.Point)
        {
            s.texture.filterMode = FilterMode.Point;
        }

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(s.rect.width * PIXEL_SCALE, s.rect.height * PIXEL_SCALE);
        }
    }

    public void SetFacing(bool faceRight)
    {
        if (rectTransform != null)
        {
            float sign = faceRight ? 1f : -1f;
            float scaleMul = isDirectHover ? 1.08f : 1.0f;
            rectTransform.localScale = new Vector3(Mathf.Abs(originalScale.x) * sign * scaleMul, originalScale.y * scaleMul, originalScale.z);
        }
    }

    public void SetTargetHighlighted(bool highlighted, bool directHover, Color highlightColor)
    {
        isDirectHover = directHover;

        if (targetIndicator != null)
        {
            targetIndicator.SetActive(highlighted);
            Image indImg = targetIndicator.GetComponent<Image>();
            if (indImg != null)
            {
                Color c = highlightColor;
                c.a = directHover ? 0.95f : 0.45f;
                indImg.color = c;
            }
        }

        if (rectTransform != null)
        {
            float scaleMul = directHover ? 1.08f : 1.0f;
            float sign = (rectTransform.localScale.x >= 0) ? 1f : -1f;
            rectTransform.localScale = new Vector3(Mathf.Abs(originalScale.x) * sign * scaleMul, originalScale.y * scaleMul, originalScale.z);
        }
    }

    public void SetHomePosition(Vector2 pos)
    {
        homePosition = pos;
        if (rectTransform != null)
            rectTransform.anchoredPosition = pos;
    }

    public Vector2 GetHomePosition() => homePosition;

    public void StartIdle()
    {
        if (isDead) return;
        StopAllAnimCoroutines();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = homePosition;
            SetFacing(baseFacingRight);
        }
        if (idleFrames != null && idleFrames.Length > 0)
        {
            loopCoroutine = StartCoroutine(LoopFrames(idleFrames, idleFps));
        }
    }

    public void PlayHurt(Action onComplete = null)
    {
        if (isDead) return;
        StopAllAnimCoroutines();
        StartCoroutine(HurtSequence(onComplete));
    }

    private IEnumerator HurtSequence(Action onComplete)
    {
        if (hurtFrames != null && hurtFrames.Length > 0)
        {
            yield return StartCoroutine(PlayFramesOnce(hurtFrames, hurtFps, new Color(1f, 0.4f, 0.4f, 1f)));
        }
        else
        {
            yield return StartCoroutine(FlashColor(new Color(1f, 0.3f, 0.3f, 1f), 0.2f));
        }

        if (!isDead)
        {
            StartIdle();
        }
        onComplete?.Invoke();
    }

    public void PlayDeath(Action onComplete = null)
    {
        isDead = true;
        StopAllAnimCoroutines();
        StartCoroutine(DeathSequence(onComplete));
    }

    private IEnumerator DeathSequence(Action onComplete)
    {
        if (deathFrames != null && deathFrames.Length > 0)
        {
            yield return StartCoroutine(PlayFramesOnce(deathFrames, deathFps, Color.white, holdLastFrame: true));
        }
        else
        {
            // Fallback fade out
            float elapsed = 0f;
            Color start = characterImage != null ? characterImage.color : Color.white;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                if (characterImage != null)
                    characterImage.color = Color.Lerp(start, new Color(start.r, start.g, start.b, 0f), elapsed / 0.5f);
                yield return null;
            }
        }
        onComplete?.Invoke();
    }

    /// <summary>
    /// Walks up to target, plays attack animation, triggers hit callback on strike, and walks back.
    /// </summary>
    public void WalkAndAttack(Vector2 targetPos, bool useHeavyAttack, Action onHit, Action onComplete)
    {
        if (isDead) return;
        StopAllAnimCoroutines();
        actionCoroutine = StartCoroutine(WalkAndAttackRoutine(targetPos, useHeavyAttack, onHit, onComplete));
    }

    private IEnumerator WalkAndAttackRoutine(Vector2 targetPos, bool useHeavyAttack, Action onHit, Action onComplete)
    {
        Vector2 startPos = homePosition;
        float walkDuration = 0.45f;

        // 1. Walk towards target
        SetFacing(targetPos.x > startPos.x);
        Coroutine walkLoop = null;
        if (walkFrames != null && walkFrames.Length > 0)
            walkLoop = StartCoroutine(LoopFrames(walkFrames, walkFps));

        float elapsed = 0f;
        while (elapsed < walkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / walkDuration);
            if (rectTransform != null)
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        if (walkLoop != null) StopCoroutine(walkLoop);

        // 2. Play attack animation
        Sprite[] atk = (useHeavyAttack && attack02Frames != null && attack02Frames.Length > 0)
            ? attack02Frames
            : ((attack01Frames != null && attack01Frames.Length > 0) ? attack01Frames : idleFrames);

        int hitFrameIndex = Mathf.Clamp(atk.Length / 2, 0, atk.Length - 1);
        bool hitTriggered = false;

        float frameDelay = 1f / attackFps;
        for (int i = 0; i < atk.Length; i++)
        {
            if (atk[i] != null)
                ApplySpriteFrame(atk[i]);

            if (i >= hitFrameIndex && !hitTriggered)
            {
                hitTriggered = true;
                onHit?.Invoke();
            }

            yield return new WaitForSeconds(frameDelay);
        }

        if (!hitTriggered)
            onHit?.Invoke();

        yield return new WaitForSeconds(0.08f);

        // 3. Walk back to home position
        SetFacing(startPos.x > targetPos.x); // face home direction
        if (walkFrames != null && walkFrames.Length > 0)
            walkLoop = StartCoroutine(LoopFrames(walkFrames, walkFps));

        elapsed = 0f;
        while (elapsed < walkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / walkDuration);
            if (rectTransform != null)
                rectTransform.anchoredPosition = Vector2.Lerp(targetPos, startPos, t);
            yield return null;
        }

        if (walkLoop != null) StopCoroutine(walkLoop);

        // 4. Restore base facing (turn back to face the enemy!)
        SetFacing(baseFacingRight);
        if (rectTransform != null)
            rectTransform.anchoredPosition = startPos;

        StartIdle();
        onComplete?.Invoke();
    }

    private IEnumerator LoopFrames(Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0) yield break;
        float delay = 1f / Mathf.Max(1f, fps);
        int index = 0;
        while (true)
        {
            if (frames[index] != null)
                ApplySpriteFrame(frames[index]);
            index = (index + 1) % frames.Length;
            yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator PlayFramesOnce(Sprite[] frames, float fps, Color tint, bool holdLastFrame = false)
    {
        if (frames == null || frames.Length == 0) yield break;
        Color originalColor = characterImage != null ? characterImage.color : Color.white;
        if (characterImage != null) characterImage.color = tint;

        float delay = 1f / Mathf.Max(1f, fps);
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
                ApplySpriteFrame(frames[i]);
            yield return new WaitForSeconds(delay);
        }

        if (characterImage != null)
            characterImage.color = originalColor;

        if (!holdLastFrame && frames.Length > 0)
        {
            ApplySpriteFrame(frames[0]);
        }
    }

    private IEnumerator FlashColor(Color flashColor, float duration)
    {
        if (characterImage == null) yield break;
        Color orig = characterImage.color;
        characterImage.color = flashColor;
        yield return new WaitForSeconds(duration);
        characterImage.color = orig;
    }

    private void StopAllAnimCoroutines()
    {
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
        }
        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
            actionCoroutine = null;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Loads sprite arrays from sliced sprite sheets in the project.
    /// </summary>
    public void LoadSpritesFromFolder(string folderPath, string characterPrefix)
    {
        idleFrames = LoadSpritesOrdered($"{folderPath}/{characterPrefix}-Idle.png");
        walkFrames = LoadSpritesOrdered($"{folderPath}/{characterPrefix}-Walk.png");
        attack01Frames = LoadSpritesOrdered($"{folderPath}/{characterPrefix}-Attack01.png");
        attack02Frames = LoadSpritesOrdered($"{folderPath}/{characterPrefix}-Attack02.png");
        hurtFrames = LoadSpritesOrdered($"{folderPath}/{characterPrefix}-Hurt.png");
        deathFrames = LoadSpritesOrdered($"{folderPath}/{characterPrefix}-Death.png");

        if (idleFrames != null && idleFrames.Length > 0)
        {
            ApplySpriteFrame(idleFrames[0]);
        }
    }

    private static Sprite[] LoadSpritesOrdered(string path)
    {
        var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets == null || assets.Length == 0) return new Sprite[0];

        var sprites = assets.OfType<Sprite>().ToList();
        sprites.Sort((a, b) =>
        {
            int idxA = ExtractFrameIndex(a.name);
            int idxB = ExtractFrameIndex(b.name);
            return idxA.CompareTo(idxB);
        });
        return sprites.ToArray();
    }

    private static int ExtractFrameIndex(string name)
    {
        int lastUnderscore = name.LastIndexOf('_');
        if (lastUnderscore >= 0 && int.TryParse(name.Substring(lastUnderscore + 1), out int val))
            return val;
        return 0;
    }
#endif
}
