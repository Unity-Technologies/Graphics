using NUnit.Framework;
using System.Reflection;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class URPGlobalSettingsStrippingTests
{
    [Test]
    // Runtime settings
    [TestCase(typeof(ShaderStrippingSetting), true)]
    [TestCase(typeof(URPDefaultVolumeProfileSettings), true)]
    [TestCase(typeof(UniversalRendererResources), true)]
    [TestCase(typeof(UniversalRenderPipelineRuntimeTextures), true)]
    [TestCase(typeof(UniversalRenderPipelineRuntimeShaders), true)]
    // Editor-only settings
    [TestCase(typeof(URPShaderStrippingSetting), false)]
    public void IsAvailableOnPlayerBuilds(System.Type type, bool expectedAvailable)
    {
        MethodInfo method = typeof(GraphicsSettings).GetMethod(nameof(GraphicsSettings.GetRenderPipelineSettings));
        Assert.IsNotNull(method, $"Unable to find method {nameof(GraphicsSettings.GetRenderPipelineSettings)}");

        MethodInfo generic = method.MakeGenericMethod(type);
        Assert.IsNotNull(generic, $"The method {nameof(GraphicsSettings.GetRenderPipelineSettings)} is not generic");

        var setting = generic.Invoke(this, null);

#if UNITY_EDITOR
        // When we are on editor, the settings are always found.
        Assert.IsNotNull(setting);
#else
        // When we are on standalone builds, we need to make sure that the behaviour defined by the setting when implementing
        // IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild is correct.
        if (expectedAvailable)
            Assert.IsNotNull(setting);
        else
            Assert.IsNull(setting);
#endif
    }
}
