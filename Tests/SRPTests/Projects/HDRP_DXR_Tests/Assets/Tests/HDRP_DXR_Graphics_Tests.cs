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

#if UNITY_EDITOR
using UnityEditor.TestTools.Graphics;
#endif

namespace UnityEngine.Rendering.HighDefinition.DXR_Tests
{

    public abstract class HDRP_DXR_GraphicsTestBase
#if UNITY_EDITOR
        : IPrebuildSetup
#endif
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Screen.SetResolution(1920, 1080, true);

            // Standard resolution for backbuffer capture is 1080p
            #if UNITY_EDITOR
            GameViewSize.SetGameViewSize(1920, 1080);
            #endif
        }

        [UnityOneTimeSetUp]
        public IEnumerator UnityOneTimeSetUp()
        {
            // Some platforms seem to require setting the resolution after play mode, otherwise resolution is not consistently set.
            yield return GlobalResolutionSetter.SetResolutionWithRetry(1920, 1080, true);
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

    // Scenes 10_, 100-115, 1000-1001: Reflections and GI keyword tests
    // [MockHmdSetup]
    public class HDRP_DXR_Reflections : HDRP_DXR_GraphicsTestBase
    {
        [UnityTest]
        [SceneGraphicsTest(@"Assets/Scenes/^1[0-9](?!00[1-4])")]
        [Timeout(450 * 1000)] // Set timeout to 450 sec. to handle complex scenes with many shaders (previous timeout was 300s)
        [IgnoreGraphicsTest(
            "1000_RaytracingQualityKeyword_PathTracer_Default",
            "Fails on Yamato"
        )]
        [TestNotSupportedOn(
            "1000_RaytracingQualityKeyword_MaterialQuality_Indirect",
            "Strict Mode not supported"
        )]
        [IgnoreGraphicsTest(
            "107_ReflectionsHybridFullRes_OnTransparent",
            "Disabled for Instability ",
            RuntimePlatform.GameCoreXboxSeries
        )]
        [TestNotSupportedOn(
            "10_BackBufferCapture|105_ReflectionsDenoised|114|1000_RaytracingQualityKeyword_PathTracer",
            "Not XR compatible",
            StereoRenderingContext.StereoEnabled
        )]
        [TestNotSupportedOn(
            "10_BackBufferCapture|105_ReflectionsDenoised|114|1000_RaytracingQualityKeyword_PathTracer",
            "Not XR compatible", XrDevice.Any
        )]
        public IEnumerator GraphicsTest(SceneGraphicsTestCase testCase)
        {
            yield return HDRP_GraphicTestRunner.Run(testCase);
        }
    }

    // Scenes 2xx, 2xxx, 3xx, 3xxx, 4xx, 4xxx, 6xx, 6xxx: Debug views, AO, GI, SSR, shadows, vertex formats
    // [MockHmdSetup]
    public class HDRP_DXR_Debug : HDRP_DXR_GraphicsTestBase
    {
        [UnityTest]
        [SceneGraphicsTest(@"Assets/Scenes/^[234]|^6[0-9]")]
        [Timeout(450 * 1000)] // Set timeout to 450 sec. to handle complex scenes with many shaders (previous timeout was 300s)
        [IgnoreGraphicsTest(
            "2009_Debug_RTAS_ScreenSpaceReflections_InstanceID|2009_Debug_RTAS_ScreenSpaceReflections_PrimitiveID|2009_Debug_RTAS_Shadows_PrimitiveID|2010_Debug_ClusterDebug|308_ScreenSpaceGlobalIllumination",
            "Fails on Yamato"
        )]
        [IgnoreGraphicsTest(
            "2007_Debug_LightCluster",
            "issue on Yamato/HDRP-3081",
            RuntimePlatform.GameCoreXboxSeries
        )]
        [IgnoreGraphicsTest(
            "2007_Debug_LightCluster",
            "issue on Yamato/HDRP-3081",
            RuntimePlatform.PS5
        )]
        [TestNotSupportedOn(
            "200[1457]",
            "Not XR compatible",
            StereoRenderingContext.StereoEnabled
        )]
        [TestNotSupportedOn(
            "200[1457]",
            "Not XR compatible", XrDevice.Any
        )]
        public IEnumerator GraphicsTest(SceneGraphicsTestCase testCase)
        {
            yield return HDRP_GraphicTestRunner.Run(testCase);
        }
    }

    // Scenes 5xx, 5xxx (path tracing, recursive rendering) and 10004 (terrain path tracing)
    // [MockHmdSetup]
    public class HDRP_DXR_PathTracing : HDRP_DXR_GraphicsTestBase
    {
        [UnityTest]
        [SceneGraphicsTest(@"Assets/Scenes/^5|^10004")]
        [Timeout(450 * 1000)] // Set timeout to 450 sec. to handle complex scenes with many shaders (previous timeout was 300s)
        [IgnoreGraphicsTest("5012_PathTracing_Transmission", "Fails on Yamato")]
        [TestNotSupportedOn(
            "10004_TerrainPathTracing|10004_TerrainPathTracing_NoDecalSurfaceGradient|5001_PathTracing|5001_PathTracing_Denoised_Intel|5001_PathTracing_Denoised_Optix|5002_PathTracing_GI|5003_PathTracing_transparency|5004_PathTracing_arealight|5005_PathTracing_Fog|5006_PathTracing_DoFVolume|5007_PathTracing_Materials_SG_Lit|5007_PathTracing_Materials_SG_Unlit|5007_PathTracing_Materials_StackLit|5008_PathTracing_NormalMapping|5009_PathTracing_FabricMaterial|501_RecursiveRendering|501_RecursiveRenderingTransparent|5010_PathTracingAlpha|5011_PathTracing_ShadowMatte|5012_PathTracing_Transmission|5013_PathTracing_ShadowFlags|5014_PathTracing_DoubleSidedOverride|5015_PathTracing_DoFCamera|5016_PathTracingTiledRendering|5017_PathTracing_Decals|505_RecursiveRenderingFog|506_RecursiveRenderingTransparentLayer|507_RecursiveRenderingDecal|5019_PathTracing_AmbientOcclusion|5018_PathTracing_MaterialOverrides|5021_PathTracing_Depth_2|5022_PathTracing_Depth_3|5020_PathTracing_Depth_1|5023_PathTracing_MeshInstancing_SG_Lit|5026_PathTracing_BoxLight",
            "Unsupported",
            RuntimePlatform.GameCoreXboxSeries,
            RuntimePlatform.PS5
        )]
        [IgnoreGraphicsTest(
            "5001_PathTracing|5010_PathTracingAlpha",
            "jira: https://jira.unity3d.com/browse/GFXFEAT-1332",
            GraphicsDeviceType.Direct3D12
        )]
        [IgnoreGraphicsTest(
            "5005_PathTracing_Fog",
            "jira: https://jira.unity3d.com/browse/GFXFEAT-1334",
            GraphicsDeviceType.Direct3D12
        )]
        [IgnoreGraphicsTest(
            "5007_PathTracing_Materials_SG_Lit",
            "issue on Yamato only: jira: https://jira.unity3d.com/browse/UUM-26542",
            RuntimePlatform.WindowsPlayer
        )]
        [TestNotSupportedOn(
            "500[1-8]|501[0-9]|502[01236]|5009|10004",
            "Not XR compatible",
            StereoRenderingContext.StereoEnabled
        )]
        [TestNotSupportedOn(
            "500[1-8]|501[0-9]|502[01236]|5009|10004",
            "Not XR compatible", XrDevice.Any
        )]
        public IEnumerator GraphicsTest(SceneGraphicsTestCase testCase)
        {
            yield return HDRP_GraphicTestRunner.Run(testCase);
        }
    }

    // Scenes 7xx, 7xxx, 8xx, 8xxx, 9xx, 9xxx (shadows, SSS, materials, VFX) and 10001-10003 (terrain)
    // [MockHmdSetup]
    public class HDRP_DXR_Other : HDRP_DXR_GraphicsTestBase
    {
        [UnityTest]
        [SceneGraphicsTest(@"Assets/Scenes/^[789]|^1000[1-3]")]
        [Timeout(450 * 1000)] // Set timeout to 450 sec. to handle complex scenes with many shaders (previous timeout was 300s)
        [IgnoreGraphicsTest(
            "802_SubSurfaceScatteringForward",
            "Inconsistent on Yamato",
            RuntimePlatform.GameCoreXboxSeries
        )]
        [IgnoreGraphicsTest(
            "802_SubSurfaceScatteringForward",
            "Inconsistent on Yamato",
            RuntimePlatform.PS5
        )]
        [IgnoreGraphicsTest(
            "900_Materials_AlphaTest_SG",
            "issue on Yamato only: jira: https://jira.unity3d.com/browse/UUM-26542"
        )]
        [IgnoreGraphicsTest(
            "902_Materials_SG_Variants_Lit",
            "issue on Yamato only: jira: https://jira.unity3d.com/browse/UUM-26542",
            RuntimePlatform.WindowsPlayer
        )]
        [IgnoreGraphicsTest(
            "10003_TerrainShadow",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-104980"
        )]
        [IgnoreGraphicsTest(
            "10001_TerrainReflectionPerf",
            "Unstable: https://jira.unity3d.com/browse/UUM-132736",
            RuntimePlatform.GameCoreXboxSeries
        )]
        [IgnoreGraphicsTest(
            "10002_TerrainReflectionQuality",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-134774",
            RuntimePlatform.GameCoreXboxSeries
        )]
        [IgnoreGraphicsTest(
            "9003_VFX_PointShadow",
            "Unstable: https://jira.unity3d.com/browse/UUM-143755",
            RuntimePlatform.GameCoreXboxSeries
        )]
        [IgnoreGraphicsTest(
            "902_Materials_SG_Variants_Eye",
            "Unstable: https://jira.unity3d.com/browse/UUM-144615",
            RuntimePlatform.GameCoreXboxSeries
        )]
        [IgnoreGraphicsTest(
            "902_Materials_SG_Variants_Fabric",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-144615",
            RuntimePlatform.GameCoreXboxSeries
        )]
        [IgnoreGraphicsTest(
            "9004_VFX_RecursiveRayTracing",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-144615",
            RuntimePlatform.GameCoreXboxSeries
        )]
        [IgnoreGraphicsTest(
            "9002_VFX_DirectionalShadow",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-144615",
            RuntimePlatform.GameCoreXboxSeries
        )]
        public IEnumerator GraphicsTest(SceneGraphicsTestCase testCase)
        {
            yield return HDRP_GraphicTestRunner.Run(testCase);
        }
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

            gpuResidentDrawerContext = GlobalContextManager.Get<GpuResidentDrawerGlobalContext>();

            previousGRDContext = (GpuResidentDrawerContext)gpuResidentDrawerContext.Current;
            gpuResidentDrawerContext.Activate(requestedGRDContext);
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Standard resolution for backbuffer capture is 1080p
            Screen.SetResolution(1920, 1080, true);
            SceneManager.LoadScene("GraphicsTestTransitionScene", LoadSceneMode.Single);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            SceneManager.LoadScene("GraphicsTestTransitionScene", LoadSceneMode.Single);
            gpuResidentDrawerContext.Activate(previousGRDContext);
        }

        [SetUp]
        public void SetUpContext()
        {
            gpuResidentDrawerContext.Activate(requestedGRDContext);
            GlobalContextManager.AssertContextIs<GpuResidentDrawerGlobalContext>(requestedGRDContext);
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
