using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

// This class detects the -urp-compatibility-mode command line argument and adds the URP_COMPATIBILITY_MODE define
// to the active build target in order to be able to run editor/playmode tests that require URP Compatibility Mode.
#if UNITY_EDITOR
[InitializeOnLoad]
public static class CompatibilityModeInitializer
{
    static CompatibilityModeInitializer()
    {
        if (RuntimeSettings.urpCompatibilityMode && !HasCompatibilityModeScriptingDefine())
        {
            SetCompatibilityModeScriptingDefine();
            Debug.Log($"Added URP_COMPATIBILITY_MODE scripting define to '{GetNamedBuildTarget().TargetName}' build target in project settings.");
        }
    }

    static NamedBuildTarget GetNamedBuildTarget()
    {
        var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
        var activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);
        return NamedBuildTarget.FromBuildTargetGroup(activeBuildTargetGroup);
    }

    static bool HasCompatibilityModeScriptingDefine()
    {
        var namedBuildTarget = GetNamedBuildTarget();
        return PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget).Contains("URP_COMPATIBILITY_MODE");
    }

    static void SetCompatibilityModeScriptingDefine()
    {
        var namedBuildTarget = GetNamedBuildTarget();
        var defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
        if (!string.IsNullOrEmpty(defines))
            defines += ";";
        defines += "URP_COMPATIBILITY_MODE";

        PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);
        AssetDatabase.SaveAssets(); // Recompile
    }
}
#endif
