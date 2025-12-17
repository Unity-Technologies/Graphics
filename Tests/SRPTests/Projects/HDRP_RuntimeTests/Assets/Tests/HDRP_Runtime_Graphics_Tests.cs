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
        "001-HDTemplate$",
        "https://jira.unity3d.com/browse/UUM-48116",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "001-HDTemplate$",
        "Small issue with incorrect rendering on bubble. Some half overflow issue and flickering artifacts. Will need image update when fixed",
        runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Switch }
    )]
    [IgnoreGraphicsTest(
        "001-HDTemplate$",
        "Linux/VK: The test is a bit flaky, failing around 1/6 runs. Needs further investigation.",
        runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.LinuxPlayer },
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Vulkan }
    )]
    [IgnoreGraphicsTest(
        "001-HDTemplate$",
        "https://jira.unity3d.com/browse/UUM-105789",
        runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.PS5, RuntimePlatform.WindowsPlayer }
    )]
    [IgnoreGraphicsTest(
        "002-HDMaterials$",
        "",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "004-CloudsFlaresDecals$",
        "Area with cloud-coverage is blue on Intel-based MacOS (CI).",
        graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal },
        architectures: new [] { Architecture.X64 }
    )]
    [IgnoreGraphicsTest(
        "007-BasicAPV$",
        "https://jira.unity3d.com/browse/UUM-54029",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal },
        architectures: new Architecture[] { Architecture.X64 }
    )]
    [IgnoreGraphicsTest(
        "012-SVL_Check$",
        "https://jira.unity3d.com/browse/UUM-70791",
        runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.PS4, RuntimePlatform.PS5, RuntimePlatform.Switch, RuntimePlatform.Switch2 }
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
