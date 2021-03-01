using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UniversalProjectEditorTests
{
    private static UniversalRenderPipelineAsset currentAsset;

    [Test]
    public void GetCurrentAsset()
    {
        GetUniversalAsset();
    }

    [Test]
    public void CheckAllLightingSettings()
    {
        var guids = AssetDatabase.FindAssets("t:LightingSettings");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LightingSettings lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (lightingSettings.bakedGI)
            {
                Assert.IsTrue(lightingSettings.lightmapper != LightingSettings.Lightmapper.Enlighten,
                    $"Lighting settings ({path}) uses deprecated lightmapper Enlighten.");
                Assert.IsTrue(lightingSettings.filteringMode == LightingSettings.FilterMode.None,
                    $"Lighting settings ({path}) have baked GI with filter mode enabled. It is recommended to turn of filter mode to reduce halo effect (If you still want to use it please contact URP team first).");
            }
        }
    }

    //[Test]
    public void GetDefaultRenderer()
    {
        GetUniversalAsset();

        Assert.IsNotNull(currentAsset.scriptableRenderer, "Current ScriptableRenderer is null.");
    }

    //Utilities
    void GetUniversalAsset()
    {
        var renderpipelineAsset = GraphicsSettings.currentRenderPipeline;

        if (renderpipelineAsset == null)
            Assert.Fail("No Render Pipeline Asset assigned.");

        if (renderpipelineAsset.GetType() == typeof(UniversalRenderPipelineAsset))
        {
            currentAsset = renderpipelineAsset as UniversalRenderPipelineAsset;
        }
        else
        {
            Assert.Inconclusive("Project not setup for Universal RP.");
        }
    }
}
