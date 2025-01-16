using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using Unity.Testing.XR.Runtime;

// [MockHmdSetup]
public class HDRP_Graphics_Tests
#if UNITY_EDITOR
    : IPrebuildSetup
#endif
{
    [UnityTest]
    [SceneGraphicsTest(@"Assets/GraphicTests/Scenes/^[0-9]+")]
    [Timeout(450 * 1000)] // Set timeout to 450 sec. to handle complex scenes with many shaders (previous timeout was 300s)
    [IgnoreGraphicsTest("1104_Unlit_Distortion_Compose", "Fails")]
    [IgnoreGraphicsTest("1201_Lit_Features", "Fails")]
    [IgnoreGraphicsTest("1206_Lit_Transparent_Distortion", "Fails")]
    [IgnoreGraphicsTest("1221_Lit_POM_Emission", "Fails")]
    [IgnoreGraphicsTest("1805_Depth_Pre_Post_Unlit", "Fails: No reference image was provided.")]
    [IgnoreGraphicsTest("1806_BatchCount", "Fails")]
    [IgnoreGraphicsTest("2120_APV_Baking", "Fails")]
    [IgnoreGraphicsTest("2301_Shadow_Mask", "Fails")]
    [IgnoreGraphicsTest("4012_MotionBlur_CameraOnly", "Fails: No reference image was provided.")]
    [IgnoreGraphicsTest("4038_Bloom", "Fails")]
    [IgnoreGraphicsTest("4103_DRS-DLSS-AfterPost", "Fails")]
    [IgnoreGraphicsTest("5014_VolumetricCloudsBanding", "Fails")]
    [IgnoreGraphicsTest("8203_Emission", "Fails")]
    [IgnoreGraphicsTest("9006_StencilUsage", "Fails: No reference image was provided.")]
    [IgnoreGraphicsTest("9304_MotionVectorsPrecomputedAndCustomVelocity", "Fails")]
    [IgnoreGraphicsTest("9601_SkinnedMeshBatching-Off", "Fails")]
    [IgnoreGraphicsTest("9602_SkinnedMeshBatching-On", "Fails")]
    [IgnoreGraphicsTest(
        "1710_Decals_Normal_Patch",
        "Disable this test as we disable the feature for Yamto on Mac",
        colorSpaces: new ColorSpace[] { ColorSpace.Linear },
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "1806",
        "Editor-only test",
        runtimePlatforms: new RuntimePlatform[]
        {
            RuntimePlatform.WindowsEditor,
            RuntimePlatform.OSXEditor,
            RuntimePlatform.LinuxEditor
        },
        isInclusive: true
    )]
    [IgnoreGraphicsTest(
        "2120_APV_Baking",
        "incorrect on metal",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "2121_APV_Baking_Sky_Occlusion",
        "incorrect on metal",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "2122_APV_Baking_Sky_Occlusion_And_Direction",
        "incorrect on metal",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "2123_APV_Baking_Shadowmask",
        "incorrect on metal",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "2210_ReflectionProbes_CaptureAtVolumeAnchor",
        "crash on yamato",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "2212_ReflectionProbes_Skies",
        "incorrect on metal",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "2220_SmoothPlanarReflection",
        "incorrect on metal",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "2222_ReflectionProbeDistanceBased",
        "incorrect on metal",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "2316_ShadowTint",
        "incorrect on metal",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "2405_EnlightenDynamicAreaLights",
        "incorrect on metal",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "2505_Area_Light_ShadowMask_Baking",
        "artifact on intel",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "3006_TileCluster_Cluster",
        "corrupt on yamato",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "3008_ShadowDebugMode",
        "corrupt on yamato",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "4090_DRS-Hardware",
        "crash on yamato",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "4096_DRS-TAAU-Hardware",
        "crash on yamato",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "4101_FP16Alpha",
        "artifact on intel",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "4105_LensFlareScreenSpace",
        "artifact on intel",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "5006_Pbr_Sky_Low_Altitude",
        "artifact on intel",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "5007_Exponential_Fog",
        "crash on yamato",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "5008_FogFiltering",
        "crash on yamato",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "5010_CloudLayer",
        "artifact on intel",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "5011_VolumetricClouds",
        "crash on yamato",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "5011_VolumetricCloudsShadows",
        "crash on yamato",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "5011_VolumetricCloudsShadowsBake",
        "crash on yamato",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "8207_InstanceIDWithKeywords",
        "platform independent",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D11 }
    )]
    [IgnoreGraphicsTest(
        "8207_InstanceIDWithKeywords",
        "platform independent",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "8207_InstanceIDWithKeywords",
        "platform independent",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Vulkan }
    )]
    [IgnoreGraphicsTest(
        "8213_Thickness",
        "artifact on intel",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "9910_GlobalMipBias",
        "crash on yamato",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "9921_UnderWater",
        "crash on yamato",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "1806_BatchCount",
        "missing CPU marker",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D12 }
    )]
    [IgnoreGraphicsTest(
        "3007_TileCluster_Tile",
        "image difference",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "4088_DRS-DLSS-Hardware",
        "instability https://jira.unity3d.com/browse/UUM-75549",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D12 }
    )]
    [IgnoreGraphicsTest(
        "4089_DRS-DLSS-Software",
        "instability https://jira.unity3d.com/browse/UUM-75549",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D12 }
    )]
    [IgnoreGraphicsTest(
        "9704_CustomPass_VRS",
        "VRS not supported",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal }
    )]
    [IgnoreGraphicsTest(
        "9704_CustomPass_VRS",
        "VRS not supported",
        graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D11 }
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
