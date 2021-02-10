using NUnit.Framework;
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

        if(renderpipelineAsset == null)
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
