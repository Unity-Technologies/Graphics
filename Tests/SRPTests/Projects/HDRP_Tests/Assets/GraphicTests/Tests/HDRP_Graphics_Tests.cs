using System.Collections;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine.SceneManagement;
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

        [UnityTest]
        [SceneGraphicsTest(@"Assets/GraphicTests/Scenes/^[0-9]+")]
        [Timeout(450 * 1000)] // Set timeout to 450 sec. to handle complex scenes with many shaders (previous timeout was 300s)
        // TODO: Add/Create Jira tickets for all of these.
        [IgnoreGraphicsTest(
            "1104_Unlit_Distortion_Compose$",
            "Need to update DX/VK/Intel-Mac ref-images."
        )]
        [IgnoreGraphicsTest(
            "2319_Mixed_Cached_ShadowMap_Area|1705_Decals-stress-test",
            "Fails when GRD is enabled.",
            contextTypes: new[] { typeof(GpuResidentDrawerGlobalContext) },
            contextMasks: new[] { (int)GpuResidentDrawerContext.GpuResidentDrawerInstancedDrawing }
        )]
        [IgnoreGraphicsTest(
            "1206_Lit_Transparent_Distortion$",
            "Outdated (Lit doesn't support distortion anymore). Should be removed."
        )]
        [IgnoreGraphicsTest(
            "1215_Lit_SubSurfaceScattering$",
            "Outdated ref-image + Slight noise divergence in the SSS.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "1217_Lit_SSS_Pre-Post$",
            "Slight noise divergence in the SSS + Some bigger difference near the top of the image.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "1221_Lit_POM_Emission$",
            "There seems to be differences in texture-sampling between DX/VK and Metal GPUs. DX seems to be forcing trilinear filtering when using anisotropy, according to: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Texture-anisoLevel.html",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "1227_Lit_Planar_Triplanar_ObjectSpace$",
            "Similar sampling issue to 1221.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal, GraphicsDeviceType.Vulkan }
        )]
        [IgnoreGraphicsTest(
            "1351_Fabric$",
            "(Intel Mac) Slight divergence on the right-most materials.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal },
            architectures: new[] { Architecture.X64 }
        )]
        [IgnoreGraphicsTest(
            "1710_Decals_Normal_Patch$",
            "(Intel Mac) Decals missing on top of StackLit and Fabric planes.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal },
            architectures: new[] { Architecture.X64 }
        )]
        [IgnoreGraphicsTest(
            "1805_Depth_Pre_Post_Unlit$",
            "(Intel Mac) Certain overlapping areas diverge, though not too apparent to the naked eye.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal },
            architectures: new[] { Architecture.X64 }
        )]
        [IgnoreGraphicsTest("1806_BatchCount$", "Fails everywhere (seems to be missing CPU markers).")]
        [IgnoreGraphicsTest(
            "2120_APV_Baking$",
            "Incorrect on DX12/Metal.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "2121_APV_Baking_Sky_Occlusion$",
            "Incorrect on Metal/VK.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal, GraphicsDeviceType.Vulkan }
        )]
        [IgnoreGraphicsTest(
            "2123_APV_Baking_Shadowmask$",
            "Seems to be missing ref-image on Metal.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "2220_SmoothPlanarReflection$",
            "Rough reflections seem to be very different in Metal compared to other APIs.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "2222_ReflectionProbeDistanceBased$",
            "Need to update Metal ref-image.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "2301_Shadow_Mask$",
            "Rendered results are too bright on Metal and DX11(instability?).",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal, GraphicsDeviceType.Direct3D11 }
        )]
        [IgnoreGraphicsTest(
            "2316_ShadowTint$",
            "Very small divergence on Metal. Maybe it needs a ref-image update?",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "2323_Shadow_Interlaced_Cascades_Update$",
            "(Intel Mac) Slight divergence on the right area of the image.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal },
            architectures: new[] { Architecture.X64 }
        )]
        [IgnoreGraphicsTest(
            "2405_EnlightenDynamicAreaLights$",
            "Results on CI are very close to DX/VK ref-images. Maybe it just need a ref-image update?",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "2405_EnlightenDynamicAreaLights$",
            "Results on CI are very close to Windows ref-images. Maybe it just need a ref-image update?",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Vulkan },
            runtimePlatforms: new[] { RuntimePlatform.LinuxEditor }
        )]
        [IgnoreGraphicsTest(
            "3006_TileCluster_Cluster$",
            "Outdated ref-images.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "3008_ShadowDebugMode$",
            "(Intel Mac) Clear color of the debug-view seems to be black instead of white. Probably just an outdated ref-image.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal },
            architectures: new[] { Architecture.X64 }
        )]
        [IgnoreGraphicsTest("4012_MotionBlur_CameraOnly$", "Missing ref-image.")]
        [IgnoreGraphicsTest(
            "4075_PhysicalCamera-gateFit$",
            "Noisy result in Linux + VK..",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Vulkan },
            runtimePlatforms: new[] { RuntimePlatform.LinuxEditor }
        )]
        [IgnoreGraphicsTest(
            "4096_DRS-TAAU-Hardware$",
            "Very small fringing across edges. Maybe a sampling artifact?",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "4101_FP16Alpha$",
            "Outdated ref-image.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "4105_LensFlareScreenSpace$",
            "(Intel Mac) Lens-flare behaviour seems to be different from all the other platforms.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal },
            architectures: new[] { Architecture.X64 }
        )]
        [IgnoreGraphicsTest(
            "4106_DRS-TAAU-AfterPost$",
            "Very small fringing across edges. Maybe a sampling artifact?",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "5006_Pbr_Sky_Low_Altitude$",
            "Differences in banding around the horizon.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "5007_Exponential_Fog$",
            "Small divergence. Needs further investigation.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "5010_CloudLayer$",
            "Very minor divergence. Maybe it's an outdated ref-image.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "5011_VolumetricClouds$",
            "Outdated ref-image.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "5011_VolumetricCloudsShadows$",
            "Sky and planar reflections diverge too much from the reference image.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "5011_VolumetricCloudsShadowsBake$",
            "(Intel Mac) Rendered image is completely black.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal },
            architectures: new[] { Architecture.X64 }
        )]
        [IgnoreGraphicsTest(
            "5013_VolumetricCloudsShadowsNoExposureControl$",
            "(Intel Mac) Rendered image is completely black.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal },
            architectures: new[] { Architecture.X64 }
        )]
        [IgnoreGraphicsTest(
            "8101_Opaque$",
            "(Intel Mac) Small divergence around 'Iridescence Specular Occlusion from Bent Normal' material. Might need a ref-image update.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal },
            architectures: new[] { Architecture.X64 }
        )]
        [IgnoreGraphicsTest("8207_InstanceIDWithKeywords$", "Missing ref-image.")]
        [IgnoreGraphicsTest(
            "8212_Fullscreen",
            "Unstable - see https://jira.unity3d.com/browse/UUM-100863",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "8213_Thickness$",
            "(Intel Mac) Bunny in the middle should be translucent-pink, not black.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal },
            architectures: new[] { Architecture.X64 }
        )]
        [IgnoreGraphicsTest("9006_StencilUsage$", "Missing ref-image.")]
        [IgnoreGraphicsTest("9601_SkinnedMeshBatching-Off$", "Outdated ref-image.")]
        [IgnoreGraphicsTest("9602_SkinnedMeshBatching-On$", "Outdated ref-image.")]
        [IgnoreGraphicsTest(
            "9703_SampleColorBuffer_InjectionPoints_Scaling$",
            "Small differences in texture-sampling. Could be related to the forced-trilinear issue?",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "9704_CustomPass_VRS$",
            "VRS not supported",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "9910_GlobalMipBias$",
            "Needs further investigation.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "9920_WaterSurface$",
            "Diverges in the wave-modifier edges.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "9922_WaterPrefab$",
            "Minor divergence across the waves' crests.",
            graphicsDeviceTypes: new[] { GraphicsDeviceType.Metal }
        )]
        [IgnoreGraphicsTest(
            "9001_LODTransition",
            "Failing with GpuResidentDrawer",
            runtimePlatforms: new[] { RuntimePlatform.OSXEditor },
            architectures: new[] { Architecture.X64 },
            contextTypes: new[] { typeof(GpuResidentDrawerGlobalContext) },
            contextMasks: new[] { (int)GpuResidentDrawerContext.GpuResidentDrawerInstancedDrawing }
        )]
        [IgnoreGraphicsTest(
            "9950-LineRendering",
            "Failing with GpuResidentDrawer",
            runtimePlatforms: new[] { RuntimePlatform.OSXEditor },
            architectures: new[] { Architecture.Arm64 },
            contextTypes: new[] { typeof(GpuResidentDrawerGlobalContext) },
            contextMasks: new[] { (int)GpuResidentDrawerContext.GpuResidentDrawerInstancedDrawing }
        )]
        [IgnoreGraphicsTest(
            "2120_APV_Baking",
            "Failing with GpuResidentDrawer",
            runtimePlatforms: new[] { RuntimePlatform.WindowsEditor },
            architectures: new[] { Architecture.X64 },
            contextTypes: new[] { typeof(GpuResidentDrawerGlobalContext) },
            contextMasks: new[] { (int)GpuResidentDrawerContext.GpuResidentDrawerInstancedDrawing }
        )]
        public IEnumerator Run(SceneGraphicsTestCase testCase)
        {
            yield return HDRP_GraphicTestRunner.Run(testCase);
        }

        [TearDown]
        public void TearDown()
        {


            Debug.ClearDeveloperConsole();

            UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(
                TestContext.CurrentContext.Test
            );
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
                    GpuResidentDrawerContext.GpuResidentDrawerDisabled
                );

                if (GraphicsTestPlatform.Current.IsEditorPlatform)
                {
                    yield return new TestFixtureData(
                        GpuResidentDrawerContext.GpuResidentDrawerInstancedDrawing
                    );
                }
            }
        }
    }
}
