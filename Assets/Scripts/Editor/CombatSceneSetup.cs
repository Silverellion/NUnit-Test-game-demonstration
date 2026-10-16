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

        Selection.activeGameObject = bootstrapper.gameObject;
        EditorUtility.SetDirty(bootstrapper);
        Debug.Log("Combat Scene setup complete! Press Play to start the game.");
    }
}
#endif
