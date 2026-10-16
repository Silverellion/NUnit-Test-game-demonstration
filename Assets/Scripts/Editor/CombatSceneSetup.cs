#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to quickly set up the Combat Scene.
/// Use menu: Tools > Setup Combat Scene
/// </summary>
public class CombatSceneSetup : EditorWindow
{
    [MenuItem("Tools/Setup Combat Scene")]
    public static void SetupScene()
    {
        CombatSceneBootstrapper bootstrapper = Object.FindAnyObjectByType<CombatSceneBootstrapper>();

        if (bootstrapper == null)
        {
            GameObject bootstrapperObj = new GameObject("CombatBootstrapper");
            bootstrapper = bootstrapperObj.AddComponent<CombatSceneBootstrapper>();
            Debug.Log("Created CombatBootstrapper game object.");
        }

        CinzelFontAssetCreator.EnsureFontAssetsExist();

        Selection.activeGameObject = bootstrapper.gameObject;
        EditorUtility.SetDirty(bootstrapper);
        ValidateSceneUI();
        Debug.Log("Combat Scene setup complete! Press Play to start the game.");
    }

    [MenuItem("Tools/Validate Scene UI Scaling")]
    public static void ValidateSceneUI()
    {
        int warningCount = 0;
        foreach (var graphic in Object.FindObjectsByType<UnityEngine.UI.Graphic>())
        {
            // Character battle sprites and outlines inside SpriteContainer are intentionally flipped to face opponents
            if (graphic.GetComponentInParent<CharacterCombatVisual>() != null)
                continue;

            if (graphic.transform.lossyScale.x < 0f)
            {
                Debug.LogWarning($"[UI Validator] Mirrored UI Graphic detected on '{graphic.gameObject.name}' (lossyScale.x = {graphic.transform.lossyScale.x})!", graphic);
                warningCount++;
            }
        }

        foreach (var tmp in Object.FindObjectsByType<TMPro.TMP_Text>())
        {
            if (tmp.transform.lossyScale.x < 0f)
            {
                Debug.LogWarning($"[UI Validator] Mirrored TMP_Text detected on '{tmp.gameObject.name}' (lossyScale.x = {tmp.transform.lossyScale.x})!", tmp);
                warningCount++;
            }
        }

        if (warningCount == 0)
        {
            Debug.Log("[UI Validator] Validation passed: No mirrored TMP_Text or UI Graphic transforms detected.");
        }
        else
        {
            Debug.LogWarning($"[UI Validator] Validation found {warningCount} mirrored UI element(s).");
        }
    }
}
#endif
