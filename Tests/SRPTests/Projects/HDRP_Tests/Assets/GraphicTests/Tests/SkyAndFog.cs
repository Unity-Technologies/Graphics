using System.Collections;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Contexts;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    // NOTE: Important! IgnoreGraphicsTest uses a pattern to ignore all Unity-scenes found which name matches that pattern.
    // That means that, you can filter-out more than one test without realizing if not careful (i.e. the pattern '5011_VolumetricClouds'
    // will filter-out the tests 5011_VolumetricClouds, 5011_VolumetricCloudsShadows and 5011_VolumetricCloudsShadowsBake).
    // To avoid this, add an extra '$' character to the end of your pattern to make it a strict-match.
    [MockHmdSetup]
    [TestFixtureSource(
        typeof(HighDefinitionTestFixtureData),
        nameof(HighDefinitionTestFixtureData.FixtureParams)
    )]
    public class SkyAndFogTests : HdrpTestsBase
    {
        public SkyAndFogTests(GpuResidentDrawerContext grdContext) : base(grdContext) { }

        [UnityTest]
        [SceneGraphicsTest(@"Assets/GraphicTests/Scenes/5x_SkyAndFog/^[0-9]+")]
        [Timeout(450 * 1000)]
        [IgnoreGraphicsTest(
            "5006_Pbr_Sky_Low_Altitude$",
            "Differences in banding around the horizon.",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "5007_Exponential_Fog$",
            "Small divergence. Needs further investigation.",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "5010_CloudLayer$",
            "Very minor divergence. Maybe it's an outdated ref-image.",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "5011_VolumetricClouds$",
            "Outdated ref-image.",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "5011_VolumetricCloudsShadows$",
            "Sky and planar reflections diverge too much from the reference image.",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "5011_VolumetricCloudsShadowsBake$",
            "(Intel Mac) Rendered image is completely black.",
            GraphicsDeviceType.Metal,
            Architecture.X64
        )]
        [IgnoreGraphicsTest(
            "5013_VolumetricCloudsShadowsNoExposureControl$",
            "(Intel Mac) Rendered image is completely black.",
            GraphicsDeviceType.Metal,
            Architecture.X64
        )]
        [IgnoreGraphicsTest(
            "5001_Fog_FogFallback",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-144922",
            RuntimePlatform.LinuxEditor, RuntimePlatform.OSXEditor, RuntimePlatform.WindowsEditor
        )]
        [TestNotSupportedOn(
            "501[0-5]|5020",
            "Not XR compatible",
            StereoRenderingContext.StereoEnabled
        )]
        [TestNotSupportedOn(
            "501[0-5]|5020",
            "Not XR compatible",
            XrDevice.OculusLoader, XrDevice.OpenXRLoader
        )]
        public IEnumerator GraphicsTest(SceneGraphicsTestCase testCase)
        {
            yield return HDRP_GraphicTestRunner.Run(testCase);
        }

    }
}
