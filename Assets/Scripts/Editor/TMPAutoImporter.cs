#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using UnityEngine;

/// <summary>
/// Automatically imports TextMesh Pro Essential Resources if missing,
/// eliminating the TMP prompt and missing font asset errors.
/// </summary>
[InitializeOnLoad]
public static class TMPAutoImporter
{
    static TMPAutoImporter()
    {
        EditorApplication.delayCall += CheckAndImportTMPEssentials;
    }

    [MenuItem("Tools/Import TMP Essentials Now")]
    public static void CheckAndImportTMPEssentials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
        {
            string packagePath = "Library/PackageCache/com.unity.ugui@0727b404f3a0/Package Resources/TMP Essential Resources.unitypackage";
            if (!File.Exists(packagePath))
            {
                string[] files = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories);
                if (files.Length > 0)
                    packagePath = files[0];
            }

            if (File.Exists(packagePath))
            {
                Debug.Log($"[TMPAutoImporter] Auto-importing TMP Essential Resources from: {packagePath}");
#pragma warning disable CS0618
                AssetDatabase.ImportPackage(packagePath, false);
#pragma warning restore CS0618
            }
            else
            {
                Debug.LogWarning("[TMPAutoImporter] Could not locate TMP Essential Resources.unitypackage in PackageCache.");
            }
        }
    }
}
#endif
