using System.Collections;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Contexts;

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
    public class RenderingFeaturesTests : HdrpTestsBase
    {
        public RenderingFeaturesTests(GpuResidentDrawerContext grdContext) : base(grdContext) { }

        [UnityTest]
        [SceneGraphicsTest(@"Assets/GraphicTests/Scenes/9x_Other/^[0-9]+")]
        [Timeout(450 * 1000)]
        [IgnoreGraphicsTest("9006_StencilUsage$", "Missing ref-image.")]
        [IgnoreGraphicsTest("9601_SkinnedMeshBatching-Off$", "Outdated ref-image.")]
        [IgnoreGraphicsTest("9602_SkinnedMeshBatching-On$", "Outdated ref-image.")]
        [IgnoreGraphicsTest(
            "9701_CustomPass_DrawRenderers",
            "Unstable - see https://jira.unity3d.com/browse/UUM-130183",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "9703_SampleColorBuffer_InjectionPoints_Scaling$",
            "Small differences in texture-sampling. Could be related to the forced-trilinear issue?",
            GraphicsDeviceType.Metal
        )]
        [TestNotSupportedOn(
            "9704_CustomPass_VRS$",
            "VRS not supported",
            GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "9910_GlobalMipBias$",
            "Needs further investigation.",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "9920_WaterSurface$",
            "Diverges in the wave-modifier edges.",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "9920_WaterSurface",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-141715",
            RuntimePlatform.WindowsEditor
        )]
        [IgnoreGraphicsTest(
            "9921_UnderWater$",
            "Unstable - see https://jira.unity3d.com/browse/UUM-134223",
            RuntimePlatform.WindowsEditor
        )]
        [IgnoreGraphicsTest(
            "9921_UnderWater_Back",
            "Unstable - see https://jira.unity3d.com/browse/UUM-134223",
            RuntimePlatform.WindowsEditor
        )]
        [IgnoreGraphicsTest(
            "9922_WaterPrefab",
            "Unstable - see https://jira.unity3d.com/browse/UUM-134223",
            RuntimePlatform.WindowsEditor
        )]
        [IgnoreGraphicsTest(
            "9922_WaterPrefab$",
            "Minor divergence across the waves' crests.",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "9001_LODTransition",
            "Failing with GpuResidentDrawer",
            RuntimePlatform.OSXEditor,
            Architecture.X64,
            GpuResidentDrawerContext.GRDEnabled
        )]
        [IgnoreGraphicsTest(
            "9950-LineRendering",
            "Failing with GpuResidentDrawer",
            RuntimePlatform.OSXEditor,
            Architecture.Arm64,
            GpuResidentDrawerContext.GRDEnabled
        )]
        [IgnoreGraphicsTest(
            "9304_MotionVectorsPrecomputedAndCustomVelocity",
            "Also fails because of Alembic removal https://jira.unity3d.com/browse/UUM-136889"
        )]
        [IgnoreGraphicsTest(
            "9921_UnderWater$",
            "Also fails because of Alembic removal https://jira.unity3d.com/browse/UUM-136889"
        )]
        [IgnoreGraphicsTest(
            "9921_UnderWater_Back",
            "Also fails because of Alembic removal https://jira.unity3d.com/browse/UUM-136889"
        )]
        [IgnoreGraphicsTest(
            "9950-LineRendering",
            "Also fails because of Alembic removal https://jira.unity3d.com/browse/UUM-136889"
        )]
        [IgnoreGraphicsTest(
            "9800_Compositor",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-138001",
            RuntimePlatform.WindowsEditor
        )]
        [IgnoreGraphicsTest(
            "9801_ShurikenLightModule",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-138001",
            RuntimePlatform.WindowsEditor
        )]
        [IgnoreGraphicsTest(
            "9701_CustomPass_DrawRenderers",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-145446",
            RuntimePlatform.WindowsEditor
        )]
        public IEnumerator GraphicsTest(SceneGraphicsTestCase testCase)
        {
            yield return HDRP_GraphicTestRunner.Run(testCase);
        }

    }
}
