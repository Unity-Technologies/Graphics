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
    [IgnoreGraphicsTest("1000_RaytracingQualityKeyword_MaterialQuality_Indirect", "Strict Mode not supported")]
    [IgnoreGraphicsTest("10004_TerrainPathTracing|10004_TerrainPathTracing_NoDecalSurfaceGradient|5001_PathTracing|5001_PathTracing_Denoised_Intel|5001_PathTracing_Denoised_Optix|5002_PathTracing_GI|5003_PathTracing_transparency|5004_PathTracing_arealight|5005_PathTracing_Fog|5006_PathTracing_DoFVolume|5007_PathTracing_Materials_SG_Lit|5007_PathTracing_Materials_SG_Unlit|5007_PathTracing_Materials_StackLit|5008_PathTracing_NormalMapping|5009_PathTracing_FabricMaterial|501_RecursiveRendering|501_RecursiveRenderingTransparent|5010_PathTracingAlpha|5011_PathTracing_ShadowMatte|5012_PathTracing_Transmission|5013_PathTracing_ShadowFlags|5014_PathTracing_DoubleSidedOverride|5015_PathTracing_DoFCamera|5016_PathTracingTiledRendering|5017_PathTracing_Decals|505_RecursiveRenderingFog|506_RecursiveRenderingTransparentLayer|507_RecursiveRenderingDecal|5019_PathTracing_AmbientOcclusion|5018_PathTracing_MaterialOverrides|5021_PathTracing_Depth_2|5022_PathTracing_Depth_3|5020_PathTracing_Depth_1|5023_PathTracing_MeshInstancing_SG_Lit|5026_PathTracing_BoxLight", "Unsupported", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.GameCoreXboxSeries })]
    [IgnoreGraphicsTest("10004_TerrainPathTracing|10004_TerrainPathTracing_NoDecalSurfaceGradient|5001_PathTracing|5001_PathTracing_Denoised_Intel|5001_PathTracing_Denoised_Optix|5002_PathTracing_GI|5003_PathTracing_transparency|5004_PathTracing_arealight|5005_PathTracing_Fog|5006_PathTracing_DoFVolume|5007_PathTracing_Materials_SG_Lit|5007_PathTracing_Materials_SG_Unlit|5007_PathTracing_Materials_StackLit|5008_PathTracing_NormalMapping|5009_PathTracing_FabricMaterial|501_RecursiveRenderingTransparent|5010_PathTracingAlpha|5011_PathTracing_ShadowMatte|5012_PathTracing_Transmission|5013_PathTracing_ShadowFlags|5014_PathTracing_DoubleSidedOverride|5015_PathTracing_DoFCamera|5016_PathTracingTiledRendering|5017_PathTracing_Decals|505_RecursiveRenderingFog|506_RecursiveRenderingTransparentLayer|507_RecursiveRenderingDecal|5019_PathTracing_AmbientOcclusion|5018_PathTracing_MaterialOverrides|5021_PathTracing_Depth_2|5022_PathTracing_Depth_3|5020_PathTracing_Depth_1|5023_PathTracing_MeshInstancing_SG_Lit|5026_PathTracing_BoxLight", "Unsupported", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.PS5 })]
    [IgnoreGraphicsTest("2007_Debug_LightCluster", "issue on Yamato/HDRP-3081", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.GameCoreXboxSeries })]
    [IgnoreGraphicsTest("2007_Debug_LightCluster", "issue on Yamato/HDRP-3081", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.PS5 })]
    [IgnoreGraphicsTest("5007_PathTracing_Materials_SG_Lit", "issue on Yamato only: jira: https://jira.unity3d.com/browse/UUM-26542", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.WindowsPlayer })]
    [IgnoreGraphicsTest("802_SubSurfaceScatteringForward", "Inconsistent on Yamato", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.GameCoreXboxSeries })]
    [IgnoreGraphicsTest("802_SubSurfaceScatteringForward", "Inconsistent on Yamato", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.PS5 })]
    [IgnoreGraphicsTest("900_Materials_AlphaTest_SG", "issue on Yamato only: jira: https://jira.unity3d.com/browse/UUM-26542")]
    [IgnoreGraphicsTest("902_Materials_SG_Variants_Lit", "issue on Yamato only: jira: https://jira.unity3d.com/browse/UUM-26542", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.WindowsPlayer })]
    [IgnoreGraphicsTest(
        "1000_RaytracingQualityKeyword_PathTracer_Default|2009_Debug_RTAS_ScreenSpaceReflections_InstanceID|2009_Debug_RTAS_ScreenSpaceReflections_PrimitiveID|2009_Debug_RTAS_Shadows_PrimitiveID|2010_Debug_ClusterDebug|308_ScreenSpaceGlobalIllumination",
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
