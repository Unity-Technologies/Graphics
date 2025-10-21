using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Contexts;
using UnityEngine.TestTools.Graphics.Platforms;
using Unity.Testing.XR.Runtime;

namespace UnityEngine.Rendering.HighDefinition.DXR_Tests
{

    // [MockHmdSetup]
    public class HDRP_Graphics_Tests
#if UNITY_EDITOR
    : IPrebuildSetup
#endif
    {
        [UnityTest]
        [SceneGraphicsTest(@"Assets/Scenes/^[0-9]+")]
        [Timeout(450 * 1000)] // Set timeout to 450 sec. to handle complex scenes with many shaders (previous timeout was 300s)
        [IgnoreGraphicsTest("5012_PathTracing_Transmission", "Fails on Yamato")]
        [IgnoreGraphicsTest(
            "1000_RaytracingQualityKeyword_PathTracer_Default|2009_Debug_RTAS_ScreenSpaceReflections_InstanceID|2009_Debug_RTAS_ScreenSpaceReflections_PrimitiveID|2009_Debug_RTAS_Shadows_PrimitiveID|2010_Debug_ClusterDebug|308_ScreenSpaceGlobalIllumination",
            "Fails on Yamato"
        )]
        [IgnoreGraphicsTest(
            "1000_RaytracingQualityKeyword_MaterialQuality_Indirect",
            "Strict Mode not supported"
        )]
        [IgnoreGraphicsTest(
            "10004_TerrainPathTracing|10004_TerrainPathTracing_NoDecalSurfaceGradient|5001_PathTracing|5001_PathTracing_Denoised_Intel|5001_PathTracing_Denoised_Optix|5002_PathTracing_GI|5003_PathTracing_transparency|5004_PathTracing_arealight|5005_PathTracing_Fog|5006_PathTracing_DoFVolume|5007_PathTracing_Materials_SG_Lit|5007_PathTracing_Materials_SG_Unlit|5007_PathTracing_Materials_StackLit|5008_PathTracing_NormalMapping|5009_PathTracing_FabricMaterial|501_RecursiveRendering|501_RecursiveRenderingTransparent|5010_PathTracingAlpha|5011_PathTracing_ShadowMatte|5012_PathTracing_Transmission|5013_PathTracing_ShadowFlags|5014_PathTracing_DoubleSidedOverride|5015_PathTracing_DoFCamera|5016_PathTracingTiledRendering|5017_PathTracing_Decals|505_RecursiveRenderingFog|506_RecursiveRenderingTransparentLayer|507_RecursiveRenderingDecal|5019_PathTracing_AmbientOcclusion|5018_PathTracing_MaterialOverrides|5021_PathTracing_Depth_2|5022_PathTracing_Depth_3|5020_PathTracing_Depth_1|5023_PathTracing_MeshInstancing_SG_Lit|5026_PathTracing_BoxLight",
            "Unsupported",
            runtimePlatforms: new RuntimePlatform[]
            {
                RuntimePlatform.GameCoreXboxSeries,
                RuntimePlatform.PS5
            }
        )]
        [IgnoreGraphicsTest(
            "2007_Debug_LightCluster",
            "issue on Yamato/HDRP-3081",
            runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.GameCoreXboxSeries }
        )]
        [IgnoreGraphicsTest(
            "2007_Debug_LightCluster",
            "issue on Yamato/HDRP-3081",
            runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.PS5 }
        )]
        [IgnoreGraphicsTest(
            "5001_PathTracing|5010_PathTracingAlpha",
            "jira: https://jira.unity3d.com/browse/GFXFEAT-1332",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Direct3D12 }
        )]
        [IgnoreGraphicsTest(
            "5005_PathTracing_Fog",
            "jira: https://jira.unity3d.com/browse/GFXFEAT-1334",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Direct3D12 }
        )]
        [IgnoreGraphicsTest(
            "5007_PathTracing_Materials_SG_Lit",
            "issue on Yamato only: jira: https://jira.unity3d.com/browse/UUM-26542",
            runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.WindowsPlayer }
        )]
        [IgnoreGraphicsTest(
            "802_SubSurfaceScatteringForward",
            "Inconsistent on Yamato",
            runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.GameCoreXboxSeries }
        )]
        [IgnoreGraphicsTest(
            "802_SubSurfaceScatteringForward",
            "Inconsistent on Yamato",
            runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.PS5 }
        )]
        [IgnoreGraphicsTest(
            "900_Materials_AlphaTest_SG",
            "issue on Yamato only: jira: https://jira.unity3d.com/browse/UUM-26542"
        )]
        [IgnoreGraphicsTest(
            "902_Materials_SG_Variants_Lit",
            "issue on Yamato only: jira: https://jira.unity3d.com/browse/UUM-26542",
            runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.WindowsPlayer }
        )]
        [IgnoreGraphicsTest(
            "10003_TerrainShadow",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-104980"
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

    [TestFixtureSource(
        typeof(HighDefinitionTestFixtureData),
        nameof(HighDefinitionTestFixtureData.FixtureParams)
    )]
    public class HDRP_GRD_DXR_Graphics_Tests
    {
        readonly GpuResidentDrawerGlobalContext gpuResidentDrawerContext;
        readonly GpuResidentDrawerContext requestedGRDContext;
        readonly GpuResidentDrawerContext previousGRDContext;

        public HDRP_GRD_DXR_Graphics_Tests(GpuResidentDrawerContext grdContext)
        {
            requestedGRDContext = grdContext;

            // Register context
            gpuResidentDrawerContext =
                GlobalContextManager.RegisterGlobalContext(typeof(GpuResidentDrawerGlobalContext))
                    as GpuResidentDrawerGlobalContext;

            // Cache previous state to avoid state leak
            previousGRDContext = (GpuResidentDrawerContext)gpuResidentDrawerContext.Context;
            gpuResidentDrawerContext.ActivateContext(requestedGRDContext);
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            SceneManager.LoadScene("GraphicsTestTransitionScene", LoadSceneMode.Single);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            SceneManager.LoadScene("GraphicsTestTransitionScene", LoadSceneMode.Single);
            gpuResidentDrawerContext.ActivateContext(previousGRDContext);
            GlobalContextManager.UnregisterGlobalContext(typeof(GpuResidentDrawerGlobalContext));
        }

        [SetUp]
        public void SetUpContext()
        {
            gpuResidentDrawerContext.ActivateContext(requestedGRDContext);
            GlobalContextManager.AssertContextIs<GpuResidentDrawerGlobalContext, GpuResidentDrawerContext>(requestedGRDContext);
        }

        [TearDown]
        public void TearDown()
        {
            Debug.ClearDeveloperConsole();
#if UNITY_EDITOR
            UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
#endif
#if ENABLE_VR
            XRGraphicsAutomatedTests.running = false;
#endif
        }

        [UnityTest]
        [SceneGraphicsTest(@"Assets/GRDScenes/^[0-9]+")]
        [Timeout(450 * 1000)] // Set timeout to 450 sec. to handle complex scenes with many shaders (previous timeout was 300s)
        public IEnumerator Run(SceneGraphicsTestCase testCase)
        {
            if (testCase.Name.Contains("11000_HDRP_Lit_Shaders"))
            {
                yield return HDRP_GraphicTestRunner.Run(testCase);
            }
            else
            {
                Assert.Ignore("https://jira.unity3d.com/browse/UUM-119893");
            }
            yield return null;
        }
    }

    public class HighDefinitionTestFixtureData
    {
        public static IEnumerable FixtureParams
        {
            get
            {
                yield return new TestFixtureData(
                    GpuResidentDrawerContext.GRDDisabled
                );

                if (GraphicsTestPlatform.Current.IsEditorPlatform)
                {
                    yield return new TestFixtureData(
                        GpuResidentDrawerContext.GRDEnabled
                    );
                }
            }
        }
    }
}
