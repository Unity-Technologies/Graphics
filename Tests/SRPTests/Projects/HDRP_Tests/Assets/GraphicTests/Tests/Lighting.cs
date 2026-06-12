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
    public class LightingTests : HdrpTestsBase
    {
        public LightingTests(GpuResidentDrawerContext grdContext) : base(grdContext) { }

        [UnityTest]
        [SceneGraphicsTest(@"Assets/GraphicTests/Scenes/2x_Lighting/^[0-9]+")]
        [Timeout(450 * 1000)]
        [IgnoreGraphicsTest(
            "2120_APV_Baking$",
            "Incorrect on DX12/Metal.",
            GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "2121_APV_Baking_Sky_Occlusion$",
            "Incorrect on Metal/VK.",
            GraphicsDeviceType.Metal, GraphicsDeviceType.Vulkan
        )]
        [IgnoreGraphicsTest(
            "2123_APV_Baking_Shadowmask$",
            "Seems to be missing ref-image on Metal.",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "2220_SmoothPlanarReflection$",
            "Rough reflections seem to be very different in Metal compared to other APIs.",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "2222_ReflectionProbeDistanceBased$",
            "Need to update Metal ref-image.",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "2301_Shadow_Mask$",
            "Rendered results are too bright on Metal and DX11(instability?).",
            GraphicsDeviceType.Metal, GraphicsDeviceType.Direct3D11
        )]
        [IgnoreGraphicsTest(
            "2316_ShadowTint$",
            "Very small divergence on Metal. Maybe it needs a ref-image update?",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "2319_Mixed_Cached_ShadowMap_Area",
            "Fails when GRD is enabled.",
            GpuResidentDrawerContext.GRDEnabled
        )]
        [IgnoreGraphicsTest(
            "2323_Shadow_Interlaced_Cascades_Update$",
            "(Intel Mac) Slight divergence on the right area of the image.",
            GraphicsDeviceType.Metal,
            Architecture.X64
        )]
        [IgnoreGraphicsTest(
            "2405_EnlightenDynamicAreaLights$",
            "Results on CI are very close to DX/VK ref-images. Maybe it just need a ref-image update?",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "2405_EnlightenDynamicAreaLights$",
            "Results on CI are very close to Windows ref-images. Maybe it just need a ref-image update?",
            GraphicsDeviceType.Vulkan,
            RuntimePlatform.LinuxEditor
        )]
        [IgnoreGraphicsTest(
            "2120_APV_Baking",
            "Failing with GpuResidentDrawer",
            RuntimePlatform.WindowsEditor,
            Architecture.X64,
            GpuResidentDrawerContext.GRDEnabled
        )]
        public IEnumerator GraphicsTest(SceneGraphicsTestCase testCase)
        {
            yield return HDRP_GraphicTestRunner.Run(testCase);
        }

    }
}
