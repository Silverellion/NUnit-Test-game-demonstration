using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EaseType
{
    Linear,
    EaseOutQuad,
    EaseOutCubic,
    EaseInOutQuad,
    EaseOutBack,
    EaseInQuad
}

/// <summary>
/// Lightweight, zero-dependency coroutine tween helper for smooth UI animations
/// (movement, scale, rotation, alpha fading, and bouncy pop-ins).
/// </summary>
public class UITween : MonoBehaviour
{
    private static UITween instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        instance = null;
    }

    private static UITween Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<UITween>();
                if (instance == null)
                {
                    GameObject go = new GameObject("[UITweenRunner]");
                    instance = go.AddComponent<UITween>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private readonly Dictionary<RectTransform, List<Coroutine>> activeTweens = new Dictionary<RectTransform, List<Coroutine>>();

    public static void Cancel(RectTransform rt)
    {
        if (rt == null || instance == null) return;
        if (instance.activeTweens.TryGetValue(rt, out var list))
        {
            foreach (var coroutine in list)
            {
                if (coroutine != null) instance.StopCoroutine(coroutine);
            }
            list.Clear();
        }
    }

    public static Coroutine Move(RectTransform rt, Vector2 targetPos, float duration, EaseType ease = EaseType.EaseOutCubic, Action onComplete = null)
    {
        if (rt == null) return null;
        return Instance.StartTween(rt, Instance.MoveRoutine(rt, targetPos, duration, ease, onComplete));
    }

    public static Coroutine Scale(RectTransform rt, Vector3 targetScale, float duration, EaseType ease = EaseType.EaseOutCubic, Action onComplete = null)
    {
        if (rt == null) return null;
        return Instance.StartTween(rt, Instance.ScaleRoutine(rt, targetScale, duration, ease, onComplete));
    }

    public static Coroutine Rotate(RectTransform rt, Quaternion targetRot, float duration, EaseType ease = EaseType.EaseOutCubic, Action onComplete = null)
    {
        if (rt == null) return null;
        return Instance.StartTween(rt, Instance.RotateRoutine(rt, targetRot, duration, ease, onComplete));
    }

    public static Coroutine Fade(CanvasGroup cg, float targetAlpha, float duration, EaseType ease = EaseType.Linear, Action onComplete = null)
    {
        if (cg == null) return null;
        RectTransform rt = cg.GetComponent<RectTransform>();
        return Instance.StartTween(rt, Instance.FadeRoutine(cg, targetAlpha, duration, ease, onComplete));
    }

    public static Coroutine PopIn(RectTransform rt, float duration = 0.35f, Action onComplete = null)
    {
        if (rt == null) return null;
        rt.localScale = Vector3.zero;
        return Instance.StartTween(rt, Instance.ScaleRoutine(rt, Vector3.one, duration, EaseType.EaseOutBack, onComplete));
    }

    private Coroutine StartTween(RectTransform rt, IEnumerator routine)
    {
        if (rt != null)
        {
            if (!activeTweens.TryGetValue(rt, out var list))
            {
                list = new List<Coroutine>();
                activeTweens[rt] = list;
            }
        }
        Coroutine c = StartCoroutine(WrapRoutine(rt, routine));
        if (rt != null && activeTweens.TryGetValue(rt, out var l))
        {
            l.Add(c);
        }
        return c;
    }

    private IEnumerator WrapRoutine(RectTransform rt, IEnumerator routine)
    {
        yield return StartCoroutine(routine);
        if (rt != null && activeTweens.TryGetValue(rt, out var list))
        {
            // Clean up finished coroutine from list
        }
    }

    private IEnumerator MoveRoutine(RectTransform rt, Vector2 target, float duration, EaseType ease, Action onComplete)
    {
        Vector2 start = rt.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration && rt != null)
        {
            elapsed += Time.deltaTime;
            float t = Evaluate(Mathf.Clamp01(elapsed / duration), ease);
            rt.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
            yield return null;
        }

        if (rt != null) rt.anchoredPosition = target;
        onComplete?.Invoke();
    }

    private IEnumerator ScaleRoutine(RectTransform rt, Vector3 target, float duration, EaseType ease, Action onComplete)
    {
        Vector3 start = rt.localScale;
        float elapsed = 0f;

        while (elapsed < duration && rt != null)
        {
            elapsed += Time.deltaTime;
            float t = Evaluate(Mathf.Clamp01(elapsed / duration), ease);
            rt.localScale = Vector3.LerpUnclamped(start, target, t);
            yield return null;
        }

        if (rt != null) rt.localScale = target;
        onComplete?.Invoke();
    }

    private IEnumerator RotateRoutine(RectTransform rt, Quaternion target, float duration, EaseType ease, Action onComplete)
    {
        Quaternion start = rt.localRotation;
        float elapsed = 0f;

        while (elapsed < duration && rt != null)
        {
            elapsed += Time.deltaTime;
            float t = Evaluate(Mathf.Clamp01(elapsed / duration), ease);
            rt.localRotation = Quaternion.SlerpUnclamped(start, target, t);
            yield return null;
        }

        if (rt != null) rt.localRotation = target;
        onComplete?.Invoke();
    }

    private IEnumerator FadeRoutine(CanvasGroup cg, float target, float duration, EaseType ease, Action onComplete)
    {
        float start = cg.alpha;
        float elapsed = 0f;

        while (elapsed < duration && cg != null)
        {
            elapsed += Time.deltaTime;
            float t = Evaluate(Mathf.Clamp01(elapsed / duration), ease);
            cg.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }

        if (cg != null) cg.alpha = target;
        onComplete?.Invoke();
    }

    public static float Evaluate(float t, EaseType ease)
    {
        switch (ease)
        {
            case EaseType.EaseOutQuad:
                return 1f - (1f - t) * (1f - t);
            case EaseType.EaseOutCubic:
                return 1f - Mathf.Pow(1f - t, 3);
            case EaseType.EaseInOutQuad:
                return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2) / 2f;
            case EaseType.EaseOutBack:
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
            case EaseType.EaseInQuad:
                return t * t;
            default:
                return t;
        }
    }
}
