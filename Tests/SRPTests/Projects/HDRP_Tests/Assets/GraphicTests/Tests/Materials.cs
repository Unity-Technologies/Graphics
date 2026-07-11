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
    public class MaterialsTests : HdrpTestsBase
    {
        public MaterialsTests(GpuResidentDrawerContext grdContext) : base(grdContext) { }

        [UnityTest]
        [SceneGraphicsTest(@"Assets/GraphicTests/Scenes/1x_Materials/^[0-9]+")]
        [Timeout(450 * 1000)]
        [IgnoreGraphicsTest(
            "1104_Unlit_Distortion_Compose$",
            "Need to update DX/VK/Intel-Mac ref-images."
        )]
        [IgnoreGraphicsTest(
            "1206_Lit_Transparent_Distortion$",
            "Outdated (Lit doesn't support distortion anymore). Should be removed."
        )]
        [IgnoreGraphicsTest(
            "1215_Lit_SubSurfaceScattering$",
            "Outdated ref-image + Slight noise divergence in the SSS.",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "1217_Lit_SSS_Pre-Post$",
            "Slight noise divergence in the SSS + Some bigger difference near the top of the image.",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "1221_Lit_POM_Emission$",
            "There seems to be differences in texture-sampling between DX/VK and Metal GPUs. DX seems to be forcing trilinear filtering when using anisotropy, according to: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Texture-anisoLevel.html",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "1227_Lit_Planar_Triplanar_ObjectSpace$",
            "Similar sampling issue to 1221.",
            GraphicsDeviceType.Metal, GraphicsDeviceType.Vulkan
        )]
        [IgnoreGraphicsTest(
            "1301_StackLitSG$",
            "Test fails when running with code coverage instrumentation.",
            CoverageState.CoverageEnabled
        )]
        [IgnoreGraphicsTest(
            "1351_Fabric$",
            "(Intel Mac) Slight divergence on the right-most materials.",
            GraphicsDeviceType.Metal,
            Architecture.X64
        )]
        [IgnoreGraphicsTest(
            "1710_Decals_Normal_Patch$",
            "(Intel Mac) Decals missing on top of StackLit and Fabric planes.",
            GraphicsDeviceType.Metal,
            Architecture.X64
        )]
        [IgnoreGraphicsTest(
            "1705_Decals-stress-test",
            "Fails when GRD is enabled.",
            GpuResidentDrawerContext.GRDEnabled
        )]
        [IgnoreGraphicsTest(
            "1805_Depth_Pre_Post_Unlit$",
            "(Intel Mac) Certain overlapping areas diverge, though not too apparent to the naked eye.",
            GraphicsDeviceType.Metal,
            Architecture.X64
        )]
        [IgnoreGraphicsTest("1806_BatchCount$", "Fails everywhere (seems to be missing CPU markers).")]
        [TestNotSupportedOn(
            "1227|1805",
            "Not XR compatible",
            StereoRenderingContext.StereoEnabled
        )]
        [TestNotSupportedOn(
            "1227|1805",
            "Not XR compatible", XrDevice.Any
        )]
        public IEnumerator GraphicsTest(SceneGraphicsTestCase testCase)
        {
            yield return HDRP_GraphicTestRunner.Run(testCase);
        }

    }
}
