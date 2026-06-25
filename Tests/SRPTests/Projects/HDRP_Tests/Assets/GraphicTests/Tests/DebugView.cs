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
    public class DebugViewTests : HdrpTestsBase
    {
        public DebugViewTests(GpuResidentDrawerContext grdContext) : base(grdContext) { }

        [UnityTest]
        [SceneGraphicsTest(@"Assets/GraphicTests/Scenes/3x_DebugView/^[0-9]+")]
        [Timeout(450 * 1000)]
        [IgnoreGraphicsTest(
            "3005_VertexDensity",
            "Unstable: https://jira.unity3d.com/browse/UUM-135177",
            RuntimePlatform.WindowsEditor
        )]
        [IgnoreGraphicsTest(
            "3006_TileCluster_Cluster$",
            "Outdated ref-images.",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "3006_TileCluster_Cluster$",
            "https://jira.unity3d.com/browse/UUM-144350 - Localized tile/cluster artifact on NVIDIA A10 in the XR playmode job after the win11-22H2-x64-azure-nvidia image update. XR and non-XR share one reference image (xr-reuse-tests), so the ignore is scoped to DX11+WindowsEditor.",
            GraphicsDeviceType.Direct3D11, RuntimePlatform.WindowsEditor
        )]
        [IgnoreGraphicsTest(
            "3008_ShadowDebugMode$",
            "(Intel Mac) Clear color of the debug-view seems to be black instead of white. Probably just an outdated ref-image.",
            GraphicsDeviceType.Metal,
            Architecture.X64
        )]
        [IgnoreGraphicsTest(
            "3009_MaterialOverrides",
            "https://jira.unity3d.com/browse/UUM-134370 - Weird artifacts on NVIDIA A10",
            GraphicsDeviceType.Direct3D11,
            RuntimePlatform.WindowsEditor
        )]
        [IgnoreGraphicsTest(
            "3004_QuadOverdraw",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-134754",
            RuntimePlatform.WindowsEditor,
            GraphicsDeviceType.Direct3D12
        )]
        [TestNotSupportedOn(
            "3002|3008|301[0-7]",
            "Not XR compatible",
            StereoRenderingContext.StereoEnabled
        )]
        [TestNotSupportedOn(
            "3002|3008|301[0-7]",
            "Not XR compatible",
            XrDevice.OculusLoader, XrDevice.OpenXRLoader
        )]
        public IEnumerator GraphicsTest(SceneGraphicsTestCase testCase)
        {
            yield return HDRP_GraphicTestRunner.Run(testCase);
        }

    }
}
