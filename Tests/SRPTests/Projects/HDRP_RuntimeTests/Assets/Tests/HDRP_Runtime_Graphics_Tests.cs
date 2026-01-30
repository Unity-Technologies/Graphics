using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using Unity.Testing.XR.Runtime;

// [MockHmdSetup]
public class HDRP_Runtime_Graphics_Tests
#if UNITY_EDITOR
    : IPrebuildSetup
#endif
{
    [IgnoreGraphicsTest("001-HDTemplate", "https://jira.unity3d.com/browse/UUM-48116", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
    [IgnoreGraphicsTest("002-HDMaterials", "", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
    [IgnoreGraphicsTest("003-VirtualTexturing", "", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
    [IgnoreGraphicsTest("003-VirtualTexturing", "VT not supported on Switch", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch })]
    [IgnoreGraphicsTest("003-VirtualTexturing-Forward", "", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
    [IgnoreGraphicsTest("003-VirtualTexturing-Forward", "VT not supported on Switch", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch })]
    [IgnoreGraphicsTest("003-VirtualTexturing-Forward", "https://jira.unity3d.com/browse/UUM-51336", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.GameCoreXboxSeries })]
    [IgnoreGraphicsTest("007-BasicAPV", "https://jira.unity3d.com/browse/UUM-54029", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }, architectures: new Architecture[] { Architecture.X64 })]
    [IgnoreGraphicsTest("012-SVL_Check", "https://jira.unity3d.com/browse/UUM-70791", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.PS4 })]
    [IgnoreGraphicsTest("012-SVL_Check", "https://jira.unity3d.com/browse/UUM-70791", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.PS5 })]
    [IgnoreGraphicsTest("003-VirtualTexturing", "Unstable: https://jira.unity3d.com/browse/UUM-113462", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.PS4 })]
    [IgnoreGraphicsTest("003-VirtualTexturing-Forward", "Unstable: https://jira.unity3d.com/browse/UUM-113462", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.PS4 })]
    [IgnoreGraphicsTest("012-SVL_Check", "VT not supported on Switch2", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch2 })]
    [IgnoreGraphicsTest(
        "005-DistortCloudsParallax|008-EnlightenRealtimeGI",
        "Image comparison failed"
    )]
    [UnityTest, SceneGraphicsTest(@"Assets/Scenes/^[0-9]+")]
    [Timeout(450 * 1000)] // Set timeout to 450 sec. to handle complex scenes with many shaders (previous timeout was 300s)
    public IEnumerator Run(SceneGraphicsTestCase testCase)
    {
        yield return HDRP_GraphicTestRunner.Run(testCase);
    }

#if UNITY_EDITOR

    public void Setup()
    {
        Unity.Testing.XR.Editor.SetupMockHMD.SetupLoader();
    }

    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(
            TestContext.CurrentContext.Test
        );
    }

    [TearDown]
    public void TearDownXR()
    {
        XRGraphicsAutomatedTests.running = false;
    }

#endif
}
