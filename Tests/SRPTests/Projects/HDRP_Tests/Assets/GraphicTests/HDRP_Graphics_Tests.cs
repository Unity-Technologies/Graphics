using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Contexts;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    [MockHmdSetup]
    [TestFixtureSource(
        typeof(HighDefinitionTestFixtureData),
        nameof(HighDefinitionTestFixtureData.FixtureParams)
    )]
    public class HDRP_Graphics_Tests
    {
        readonly GpuResidentDrawerGlobalContext gpuResidentDrawerContext;
        readonly GpuResidentDrawerContext requestedGRDContext;
        readonly GpuResidentDrawerContext previousGRDContext;

        readonly GpuOcclusionCullingGlobalContext gpuOcclusionContext;
        readonly GpuOcclusionCullingContext requestedOccContext;
        readonly GpuOcclusionCullingContext previousOccContext;

        public HDRP_Graphics_Tests(GpuResidentDrawerContext grdContext)
        {
            requestedGRDContext = grdContext;
            requestedOccContext = GpuOcclusionCullingGlobalContext.ChooseContext(grdContext);

            // Register context
            gpuResidentDrawerContext =
                GlobalContextManager.RegisterGlobalContext(typeof(GpuResidentDrawerGlobalContext))
                    as GpuResidentDrawerGlobalContext;

            gpuOcclusionContext =
                GlobalContextManager.RegisterGlobalContext(typeof(GpuOcclusionCullingGlobalContext))
                    as GpuOcclusionCullingGlobalContext;

            // Cache previous state to avoid state leak
            previousGRDContext = (GpuResidentDrawerContext)gpuResidentDrawerContext.Context;
            previousOccContext = (GpuOcclusionCullingContext)gpuOcclusionContext.Context;

            gpuResidentDrawerContext.ActivateContext(requestedGRDContext);
            gpuOcclusionContext.ActivateContext(requestedOccContext);
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            SceneManager.LoadScene("GraphicsTestTransitionScene", LoadSceneMode.Single);
        }

        [SetUp]
        public void SetUpContext()
        {
            gpuResidentDrawerContext.ActivateContext(requestedGRDContext);
            gpuOcclusionContext.ActivateContext(requestedOccContext);

            GlobalContextManager.AssertContextIs<GpuResidentDrawerGlobalContext, GpuResidentDrawerContext>(requestedGRDContext);
            GlobalContextManager.AssertContextIs<GpuOcclusionCullingGlobalContext, GpuOcclusionCullingContext>(requestedOccContext);
        }

        [IgnoreGraphicsTest("1710_Decals_Normal_Patch", "Disable this test as we disable the feature for Yamto on Mac", colorSpaces: new ColorSpace[] { ColorSpace.Linear }, graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("2120_APV_Baking", "incorrect on metal", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("2121_APV_Baking_Sky_Occlusion", "incorrect on metal", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("2122_APV_Baking_Sky_Occlusion_And_Direction", "incorrect on metal", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("2123_APV_Baking_Shadowmask", "incorrect on metal", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("2210_ReflectionProbes_CaptureAtVolumeAnchor", "crash on yamato", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("2212_ReflectionProbes_Skies", "incorrect on metal", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("2220_SmoothPlanarReflection", "incorrect on metal", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("2222_ReflectionProbeDistanceBased", "incorrect on metal", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("2316_ShadowTint", "incorrect on metal", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("2405_EnlightenDynamicAreaLights", "incorrect on metal", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("2505_Area_Light_ShadowMask_Baking", "artifact on intel", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("3006_TileCluster_Cluster", "corrupt on yamato", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("3008_ShadowDebugMode", "corrupt on yamato", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("4090_DRS-Hardware", "crash on yamato", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("4096_DRS-TAAU-Hardware", "crash on yamato", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("4101_FP16Alpha", "artifact on intel", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("4105_LensFlareScreenSpace", "artifact on intel", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("5006_Pbr_Sky_Low_Altitude", "artifact on intel", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("5007_Exponential_Fog", "crash on yamato", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("5008_FogFiltering", "crash on yamato", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("5010_CloudLayer", "artifact on intel", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("5011_VolumetricClouds", "crash on yamato", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("5011_VolumetricCloudsShadows", "crash on yamato", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("5011_VolumetricCloudsShadowsBake", "crash on yamato", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("8207_InstanceIDWithKeywords", "platform independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D11 })]
        [IgnoreGraphicsTest("8207_InstanceIDWithKeywords", "platform independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("8207_InstanceIDWithKeywords", "platform independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Vulkan })]
        [IgnoreGraphicsTest("8213_Thickness", "artifact on intel", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("9910_GlobalMipBias", "crash on yamato", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("9921_UnderWater", "crash on yamato", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("1806_BatchCount", "missing CPU marker", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D12 })]
        [IgnoreGraphicsTest("3007_TileCluster_Tile", "image difference", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
        [IgnoreGraphicsTest("4075_PhysicalCamera-gateFit", "Instability https://jira.unity3d.com/browse/UUM-100651", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.WindowsPlayer }, graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D11 })]
        [IgnoreGraphicsTest("4075_PhysicalCamera-gateFit", "Unstable: https://jira.unity3d.com/browse/UUM-100651")]
        [IgnoreGraphicsTest(
            "4060_CustomPostProcess|8207_InstancingAPIs|8212_Fullscreen|9700_CustomPass_FullScreen|9702_CustomPass_API|9703_SampleColorBuffer_InjectionPoints_Scaling|9931-ScreenCoordOverrideCustomPostProcess|1104_Unlit_Distortion_Compose|1206_Lit_Transparent_Distortion|1805_Depth_Pre_Post_Unlit|4012_MotionBlur_CameraOnly|4038_Bloom|5014_VolumetricCloudsBanding|9006_StencilUsage|9304_MotionVectorsPrecomputedAndCustomVelocity|9601_SkinnedMeshBatching-Off|9602_SkinnedMeshBatching-On|",
            "Image difference when updating from GTF8 to GTF9",
            runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.WindowsEditor })]
        [UnityTest, SceneGraphicsTest(@"Assets/GraphicTests/Scenes/^[0-9]+")]
        [Timeout(450 * 1000)] // Set timeout to 450 sec. to handle complex scenes with many shaders (previous timeout was 300s)
        public IEnumerator Run(SceneGraphicsTestCase testCase)
        {
            yield return HDRP_GraphicTestRunner.Run(testCase);
        }

        [TearDown]
        public void TearDown()
        {


            Debug.ClearDeveloperConsole();
#if UNITY_EDITOR
            UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(
                TestContext.CurrentContext.Test
            );
#endif
#if ENABLE_VR
            XRGraphicsAutomatedTests.running = false;
#endif
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            SceneManager.LoadScene("GraphicsTestTransitionScene", LoadSceneMode.Single);

            gpuResidentDrawerContext.ActivateContext(previousGRDContext);
            gpuOcclusionContext.ActivateContext(previousOccContext);

            GlobalContextManager.UnregisterGlobalContext(typeof(GpuResidentDrawerGlobalContext));
            GlobalContextManager.UnregisterGlobalContext(typeof(GpuOcclusionCullingGlobalContext));
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
