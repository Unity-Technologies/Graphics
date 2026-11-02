using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using Unity.Testing.XR.Runtime;

#if UNITY_EDITOR
using UnityEditor.TestTools.Graphics;
#endif

// [MockHmdSetup]
public class HDRP_Runtime_Graphics_Tests
#if UNITY_EDITOR
    : IPrebuildSetup
#endif
{
    [OneTimeSetUp]
    public void SetDefaultResolution()
    {
        Screen.SetResolution(1920, 1080, true);
        // Standard resolution for backbuffer capture is 1080p      
        #if UNITY_EDITOR
        GameViewSize.SetGameViewSize(1920, 1080);
        #endif
    }

    [UnityOneTimeSetUp]
    public IEnumerable UnityOneTimeSetUp()
    {
        // Required because of resolution inconsistencies on some platforms                     
        yield return GlobalResolutionSetter.SetResolutionWithRetry(1920, 1080, true);
    }

    [UnityTest]
    [SceneGraphicsTest(@"Assets/Scenes/^[0-9]+")]
    [Timeout(450 * 1000)] // Set timeout to 450 sec. to handle complex scenes with many shaders (previous timeout was 300s)
    [IgnoreGraphicsTest("005", "Was excluded from the build settings")]
    [IgnoreGraphicsTest("007|008", "Temporary ignore due to light baking")]
    [IgnoreGraphicsTest(
        "001-HDTemplate$",
        "https://jira.unity3d.com/browse/UUM-48116",
        GraphicsDeviceType.Metal
    )]
    [IgnoreGraphicsTest(
        "001-HDTemplate$",
        "Small issue with incorrect rendering on bubble. Some half overflow issue and flickering artifacts. Will need image update when fixed",
        RuntimePlatform.Switch
    )]
    [IgnoreGraphicsTest(
        "001-HDTemplate$",
        "Linux/VK: The test is a bit flaky, failing around 1/6 runs. Needs further investigation.",
        RuntimePlatform.LinuxPlayer,
        GraphicsDeviceType.Vulkan
    )]
    [IgnoreGraphicsTest(
        "001-HDTemplate$",
        "https://jira.unity3d.com/browse/UUM-105789",
        RuntimePlatform.PS5, RuntimePlatform.WindowsPlayer
    )]
    [IgnoreGraphicsTest(
        "002-HDMaterials$",
        "",
        GraphicsDeviceType.Metal
    )]
    [IgnoreGraphicsTest(
        "004-CloudsFlaresDecals$",
        "Area with cloud-coverage is blue on Intel-based MacOS (CI).",
        GraphicsDeviceType.Metal,
        Architecture.X64
    )]
    [IgnoreGraphicsTest(
        "007-BasicAPV$",
        "https://jira.unity3d.com/browse/UUM-54029",
        GraphicsDeviceType.Metal,
        Architecture.X64
    )]
    [IgnoreGraphicsTest(
        "010-BRG-Simple",
        "Unstable: https://jira.unity3d.com/browse/UUM-134572",
        RuntimePlatform.PS5
    )]
    [IgnoreGraphicsTest(
        "012-SVL_Check$",
        "https://jira.unity3d.com/browse/UUM-70791",
        RuntimePlatform.PS4, RuntimePlatform.PS5, RuntimePlatform.Switch, RuntimePlatform.Switch2
    )]
    [TestNotSupportedOn(
        "011-HighQualityLines$",
        "HQ Line Rendering needs a 1024-thread compute threadgroup; Intel Mac Metal caps below that. https://jira.unity3d.com/browse/UUM-147457",
        GraphicsDeviceType.Metal,
        Architecture.X64
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
