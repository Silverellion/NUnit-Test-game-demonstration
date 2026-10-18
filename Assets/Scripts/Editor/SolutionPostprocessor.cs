#if UNITY_EDITOR
using UnityEditor;

public class SolutionPostprocessor : AssetPostprocessor
{
    private static string OnGeneratedSlnSolution(string path, string content)
    {
        if (content.Contains("CombatRules.Tests.csproj"))
            return content;

        if (content.Contains("</Solution>"))
            return content.Replace("</Solution>", "  <Project Path=\"CombatRules.Tests\\CombatRules.Tests.csproj\" />\n</Solution>");

        return content;
    }
}
#endif
