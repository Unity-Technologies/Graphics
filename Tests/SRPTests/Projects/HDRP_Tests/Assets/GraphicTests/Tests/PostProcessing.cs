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
    public class PostProcessingTests : HdrpTestsBase
    {
        public PostProcessingTests(GpuResidentDrawerContext grdContext) : base(grdContext) { }

        [UnityTest]
        [SceneGraphicsTest(@"Assets/GraphicTests/Scenes/4x_PostProcessing/^[0-9]+")]
        [Timeout(450 * 1000)]
        [IgnoreGraphicsTest("4012_MotionBlur_CameraOnly$", "Missing ref-image.")]
        [IgnoreGraphicsTest(
            "4075_PhysicalCamera-gateFit$",
            "Noisy result in Linux + VK..",
            GraphicsDeviceType.Vulkan,
            RuntimePlatform.LinuxEditor
        )]
        [IgnoreGraphicsTest(
            "4096_DRS-TAAU-Hardware$",
            "Very small fringing across edges. Maybe a sampling artifact?",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "4101_FP16Alpha$",
            "Outdated ref-image.",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "4105_LensFlareScreenSpace$",
            "(Intel Mac) Lens-flare behaviour seems to be different from all the other platforms.",
            GraphicsDeviceType.Metal, Architecture.X64
        )]
        [IgnoreGraphicsTest(
            "4106_DRS-TAAU-AfterPost$",
            "Very small fringing across edges. Maybe a sampling artifact?",
            GraphicsDeviceType.Metal
        )]
        [TestOnlySupportedOn(
            "4107_DRS-FSR2-Hardware",
            "Platform not supported", // FSR is DX12/DX11/Vulkan on PC-only
            GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Vulkan,
            RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer
        )]
        [TestOnlySupportedOn(
            "4108_DRS-FSR2-Software",
            "Platform not supported", // FSR is DX12/DX11/Vulkan on PC-only
            GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Vulkan,
            RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer
        )]
        [TestOnlySupportedOn(
            "4109_DRS-FSR2-AfterPost",
            "Platform not supported", // FSR is DX12/DX11/Vulkan on PC-only
            GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Vulkan,
            RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer
        )]
        [TestOnlySupportedOn(
            "4110_DRS-FSR2-With-CustomPass",
            "Platform not supported", // FSR is DX12/DX11/Vulkan on PC-only
            GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Vulkan,
            RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer
        )]
        [TestOnlySupportedOn(
            "4088_DRS-DLSS-Hardware",
            "Platform not supported", // DLSS is DX12/DX11/Vulkan on PC-only
            GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Vulkan,
            RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer
        )]
        [TestOnlySupportedOn(
            "4089_DRS-DLSS-Software",
            "Platform not supported", // DLSS is DX12/DX11/Vulkan on PC-only
            GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Vulkan,
            RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer
        )]
        [TestOnlySupportedOn(
            "4103_DRS-DLSS-AfterPost",
            "Platform not supported", // DLSS is DX12/DX11/Vulkan on PC-only
            GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Vulkan,
            RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer
        )]
        [IgnoreGraphicsTest(
            "4103_DRS-DLSS-AfterPost",
            "Unstable: https://jira.unity3d.com/browse/UUM-137893"
        )]
        [TestOnlySupportedOn(
            "4111_DRS-DLSS-With-CustomPass",
            "Platform not supported", // DLSS is DX12/DX11/Vulkan on PC-only
            GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Vulkan,
            RuntimePlatform.WindowsEditor, RuntimePlatform.WindowsPlayer
        )]
        [IgnoreGraphicsTest(
            "4075_PhysicalCamera-gateFit|4111_DRS-DLSS-With-CustomPass",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-134786",
            RuntimePlatform.WindowsEditor
        )]
        [IgnoreGraphicsTest(
            "4014_PrecomputedVelocityAlembic",
            "https://jira.unity3d.com/browse/UUM-136889"
        )]
        [IgnoreGraphicsTest(
            "4089_DRS-DLSS-Software",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-135194",
            RuntimePlatform.WindowsEditor
        )]
        [IgnoreGraphicsTest(
            "4088_DRS-DLSS-Hardware",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-135194",
            RuntimePlatform.WindowsEditor
        )]
        [IgnoreGraphicsTest(
            "^(?!.*(4088_DRS-DLSS-Hardware|4089_DRS-DLSS-Software|4090_DRS-Hardware|4096_DRS-TAAU-Hardware|4100_DRS-STP-Hardware|4102_DRS-CAS-AfterPost|4103_DRS-DLSS-AfterPost|4104_DRS-STP-AfterPost|4106_DRS-TAAU-AfterPost|4107_DRS-FSR2-Hardware|4109_DRS-FSR2-AfterPost|4110_DRS-FSR2-With-CustomPass|4111_DRS-DLSS-With-CustomPass)).*",
            "Upscaler-only job — non-upscaler tests excluded when ENABLE_UPSCALER_FRAMEWORK is defined",
            UpscalerFrameworkContext.UpscalerFrameworkEnabled
        )]
        [TestNotSupportedOn(
            "4011|4013|4014|4038|4060|4061|4099|4100",
            "Not XR compatible",
            StereoRenderingContext.StereoEnabled
        )]
        [TestNotSupportedOn(
            "4011|4013|4014|4038|4060|4061|4099|4100",
            "Not XR compatible",
            XrDevice.OculusLoader, XrDevice.OpenXRLoader
        )]
        public IEnumerator GraphicsTest(SceneGraphicsTestCase testCase)
        {
            yield return HDRP_GraphicTestRunner.Run(testCase);
        }

    }
}
