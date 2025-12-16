using NUnit.Framework;
using System.Collections;
using System.Reflection;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.HighDefinition
{
    public class HDGlobalSettingsStrippingTests
    {
        [Test]

        // Available on Standalone builds
        [TestCase(typeof(ShaderStrippingSetting), true)]
        [TestCase(typeof(RenderingPathFrameSettings), true)]
        [TestCase(typeof(LensSettings), true)]
        [TestCase(typeof(ColorGradingSettings), true)]
        [TestCase(typeof(SpecularFadeSettings), true)]
        [TestCase(typeof(AnalyticDerivativeSettings), true)]
        [TestCase(typeof(CustomPostProcessOrdersSettings), true)]
        [TestCase(typeof(HDRPDefaultVolumeProfileSettings), true)]

        // Not available on Standalone
        [TestCase(typeof(DiffusionProfileDefaultSettings), false)]
        [TestCase(typeof(LookDevVolumeProfileSettings), false)]

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
}
