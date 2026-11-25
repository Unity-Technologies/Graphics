using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class VFXPreprocessBuild : IPreprocessBuildWithReport
{
    public int callbackOrder => int.MinValue + 49;

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("Manually excluding Switch to workaround UUM-73363");
        QualitySettings.TryExcludePlatformAt("Switch", 1, out var _);
        DecalSurfaceGradientStripper.ResetBuildState();
    }
}

class DecalSurfaceGradientStripper : IPreprocessShaders
{
    const string k_KeywordName = "DECAL_SURFACE_GRADIENT";
    static readonly ShaderKeyword k_Keyword = new ShaderKeyword(k_KeywordName);
    static bool? s_EveryAssetEnablesKeyword;

    public int callbackOrder => 0;

    public static void ResetBuildState()
    {
        s_EveryAssetEnablesKeyword = null;
    }

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (data.Count == 0 || !EveryAssetEnablesKeyword())
            return;

        var keyword = shader.keywordSpace.FindKeyword(k_KeywordName);
        if (!keyword.isValid || !ShaderUtil.PassHasKeyword(shader, snippet.pass, keyword, snippet.shaderType, data[0].shaderCompilerPlatform))
            return;

        for (int i = data.Count - 1; i >= 0; --i)
        {
            if (!data[i].shaderKeywordSet.IsEnabled(k_Keyword))
                data.RemoveAt(i);
        }
    }

    static bool EveryAssetEnablesKeyword()
    {
        if (s_EveryAssetEnablesKeyword.HasValue)
            return s_EveryAssetEnablesKeyword.Value;

        var assets = new List<HDRenderPipelineAsset>();
        bool everyAssetEnablesKeyword = EditorUserBuildSettings.activeBuildTarget.TryGetRenderPipelineAssets(assets) && assets.Count > 0;
        foreach (var asset in assets)
        {
            var settings = asset.currentPlatformRenderPipelineSettings;
            everyAssetEnablesKeyword &= settings.supportDecals && settings.supportSurfaceGradient;
        }

        s_EveryAssetEnablesKeyword = everyAssetEnablesKeyword;
        return everyAssetEnablesKeyword;
    }
}
