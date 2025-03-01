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
    [UnityTest]
    [SceneGraphicsTest(@"Assets/Scenes/^[0-9]+")]
    [Timeout(450 * 1000)] // Set timeout to 450 sec. to handle complex scenes with many shaders (previous timeout was 300s)
    [IgnoreGraphicsTest("005", "Was excluded from the build settings")]
    [IgnoreGraphicsTest("007|008", "Temporary ignore due to light baking")]
    [IgnoreGraphicsTest(
        "001-HDTemplate",
        "https://jira.unity3d.com/browse/UUM-48116",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "002-HDMaterials",
        "",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest("003-VirtualTexturing", "")]
    [IgnoreGraphicsTest(
        "003-VirtualTexturing",
        "VT not supported on Switch",
        runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch }
    )]
    [IgnoreGraphicsTest(
        "003-VirtualTexturing-Forward",
        "",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "003-VirtualTexturing-Forward",
        "VT not supported on Switch",
        runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch }
    )]
    [IgnoreGraphicsTest(
        "003-VirtualTexturing-Forward",
        "https://jira.unity3d.com/browse/UUM-51336",
        runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.GameCoreXboxSeries }
    )]
    [IgnoreGraphicsTest(
        "007-BasicAPV",
        "https://jira.unity3d.com/browse/UUM-54029",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal },
        architectures: new Architecture[] { Architecture.X64 }
    )]
    [IgnoreGraphicsTest(
        "012-SVL_Check",
        "https://jira.unity3d.com/browse/UUM-70791",
        runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.PS4 }
    )]
    [IgnoreGraphicsTest(
        "012-SVL_Check",
        "https://jira.unity3d.com/browse/UUM-70791",
        runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.PS5 }
    )]
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
