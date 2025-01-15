using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RendererRequirements = UnityEditor.Rendering.Universal.ShaderBuildPreprocessor.RendererRequirements;
using DecalSettings = UnityEngine.Rendering.Universal.DecalSettings;
using Object = UnityEngine.Object;

namespace ShaderStrippingAndPrefiltering
{
    class ShaderBuildPreprocessorTests
    {
        private class TestHelper
        {
            internal UniversalRendererData rendererData;
            internal ScriptableRendererData scriptableRendererData;
            internal UniversalRenderPipelineAsset urpAsset;
            internal ScriptableRenderer ScriptableRenderer;
            internal UniversalRenderer universalRenderer;
            internal List<ShaderFeatures> rendererShaderFeatures;
            internal List<ScreenSpaceAmbientOcclusionSettings> ssaoRendererFeatures;
            internal List<ScriptableRendererFeature> rendererFeatures;
            internal bool stripUnusedVariants;
            internal bool containsForwardRenderer;
            internal bool everyRendererHasSSAO;

            internal ShaderFeatures defaultURPAssetFeatures
            {
                get
                {
                    ShaderFeatures defaultFeatures =
                              ShaderFeatures.MainLight
                            | ShaderFeatures.MixedLighting
                            | ShaderFeatures.TerrainHoles
                            | ShaderFeatures.LightCookies
                            | ShaderFeatures.LODCrossFade
                            | ShaderFeatures.AutoSHMode
                            | ShaderFeatures.DataDrivenLensFlare
                            | ShaderFeatures.DrawProcedural;

                    if (PlayerSettings.allowHDRDisplaySupport)
                        defaultFeatures |= ShaderFeatures.HdrGrading;

                    return defaultFeatures;
                }
            }

            internal RendererRequirements defaultRendererRequirements;

            public TestHelper()
            {
                try
                {
                    rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                    scriptableRendererData = rendererData;

                    urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                    urpAsset.name = "TestHelper_URPAsset";
                    GraphicsSettings.defaultRenderPipeline = urpAsset;

                    ScriptableRenderer = urpAsset.GetRenderer(0);
                    universalRenderer = ScriptableRenderer as UniversalRenderer;
                    stripUnusedVariants = true;
                    Assert.AreNotEqual(null, universalRenderer);

                    ssaoRendererFeatures = new List<ScreenSpaceAmbientOcclusionSettings>();
                    rendererFeatures = new List<ScriptableRendererFeature>();

                    ResetData();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.StackTrace);
                    Cleanup();
                }
            }

            internal RendererRequirements GetRendererRequirements()
            {
                return ShaderBuildPreprocessor.GetRendererRequirements(ref urpAsset, ref ScriptableRenderer, ref scriptableRendererData);
            }

            internal void GetEveryVolumeFeatures(ref VolumeFeatures volumeFeatures)
            {
                ShaderBuildPreprocessor.GetEveryVolumeFeatures(ref volumeFeatures);
            }

            internal void GetEveryShaderFeatureAndPrefilteringData(List<ShaderFeatures> rendererFeaturesList, ref UniversalRenderPipelineAsset.ShaderPrefilteringData spd)
            {
                ShaderBuildPreprocessor.GetEveryShaderFeatureAndPrefilteringData(rendererFeaturesList, ref spd);
            }

            internal ShaderFeatures GetSupportedShaderFeaturesFromAsset()
            {
                return ShaderBuildPreprocessor.GetSupportedShaderFeaturesFromAsset(ref urpAsset, ref rendererShaderFeatures, ref ssaoRendererFeatures, stripUnusedVariants, out bool containsForwardRenderer, out bool everyRendererHasSSAO);
            }

            internal ShaderFeatures GetSupportedShaderFeaturesFromRenderer(RendererRequirements rendererRequirements, ShaderFeatures urpAssetShaderFeatures)
            {
                return ShaderBuildPreprocessor.GetSupportedShaderFeaturesFromRenderer(ref rendererRequirements, ref scriptableRendererData, ref ssaoRendererFeatures, ref containsForwardRenderer, urpAssetShaderFeatures);
            }

            internal ShaderFeatures GetSupportedShaderFeaturesFromRendererFeatures(RendererRequirements rendererRequirements)
            {
                return ShaderBuildPreprocessor.GetSupportedShaderFeaturesFromRendererFeatures(ref rendererRequirements, ref rendererFeatures, ref ssaoRendererFeatures);
            }


            private const string k_OnText = "enabled";
            private const string k_OffText = "disabled";

            internal void AssertVolumeFeatures(VolumeFeatures expected, VolumeFeatures actual)
            {
                foreach (Enum value in Enum.GetValues(typeof(VolumeFeatures)))
                {
                    bool expectedResult = expected.HasFlag(value);
                    string expectsText = expectedResult ? k_OnText : k_OffText;
                    bool actualResult = actual.HasFlag(value);
                    string actualText = actualResult ? k_OnText : k_OffText;

                    Assert.AreEqual(
                        expectedResult,
                        actualResult,
                        $"Incorrect Feature flag for ShaderFeatures.{(VolumeFeatures) value}\nThe test expected it to {expectsText} but it was {actualText}\n");
                }
            }

            internal void AssertShaderFeatures(ShaderFeatures expected, ShaderFeatures actual)
            {
                foreach (Enum value in Enum.GetValues(typeof(ShaderFeatures)))
                {
                    bool expectedResult = expected.HasFlag(value);
                    string expectsText = expectedResult ? k_OnText : k_OffText;
                    bool actualResult = actual.HasFlag(value);
                    string actualText = actualResult ? k_OnText : k_OffText;

                    Assert.AreEqual(
                        expectedResult,
                        actualResult,
                        $"Incorrect Feature flag for ShaderFeatures.{(ShaderFeatures) value}\nThe test expected it to {expectsText} but it was {actualText}\n");
                }
            }

            internal void AssertShaderFeaturesAndReset(ShaderFeatures expected, ShaderFeatures actual)
            {
                AssertShaderFeatures(expected, actual);
                ResetData();
            }

            internal void AssertRendererRequirements(RendererRequirements expected, RendererRequirements actual)
            {
                Assert.AreEqual(expected.msaaSampleCount, actual.msaaSampleCount, "msaaSampleCount mismatch");
                Assert.AreEqual(expected.isUniversalRenderer, actual.isUniversalRenderer, "isUniversalRenderer mismatch");
                Assert.AreEqual(expected.needsProcedural, actual.needsProcedural, "needsProcedural mismatch");
                Assert.AreEqual(expected.needsSoftShadows, actual.needsSoftShadows, "needsSoftShadows mismatch");
                Assert.AreEqual(expected.needsAdditionalLightShadows, actual.needsAdditionalLightShadows, "needsAdditionalLightShadows mismatch");
                Assert.AreEqual(expected.needsShadowsOff, actual.needsShadowsOff, "needsShadowsOff mismatch");
                Assert.AreEqual(expected.needsAdditionalLightsOff, actual.needsAdditionalLightsOff, "needsAdditionalLightsOff mismatch");
                Assert.AreEqual(expected.needsGBufferRenderingLayers, actual.needsGBufferRenderingLayers, "needsGBufferRenderingLayers mismatch");
                Assert.AreEqual(expected.needsGBufferAccurateNormals, actual.needsGBufferAccurateNormals, "needsGBufferAccurateNormals mismatch");
                Assert.AreEqual(expected.needsRenderPass, actual.needsRenderPass, "needsRenderPass mismatch");
                Assert.AreEqual(expected.needsReflectionProbeBlending, actual.needsReflectionProbeBlending, "needsReflectionProbeBlending mismatch");
                Assert.AreEqual(expected.needsReflectionProbeBoxProjection, actual.needsReflectionProbeBoxProjection, "needsReflectionProbeBoxProjection mismatch");
                Assert.AreEqual(expected.renderingMode, actual.renderingMode, "renderingMode mismatch");
                Assert.AreEqual(expected, actual, "Some mismatch between the renderer requirements that is not covered in the previous tests.");
            }
            internal void AssertRendererRequirementsAndReset(RendererRequirements expected, RendererRequirements actual)
            {
                AssertRendererRequirements(expected, actual);
                ResetData();
            }

            internal void ResetData()
            {
                defaultRendererRequirements = new();
                defaultRendererRequirements.msaaSampleCount = 1;
                defaultRendererRequirements.isUniversalRenderer = true;
                defaultRendererRequirements.needsAdditionalLightShadows = false;
                defaultRendererRequirements.needsSoftShadows = false;
                defaultRendererRequirements.needsShadowsOff = false;
                defaultRendererRequirements.needsAdditionalLightsOff = false;
                defaultRendererRequirements.needsGBufferRenderingLayers = false;
                defaultRendererRequirements.needsGBufferAccurateNormals = false;
                defaultRendererRequirements.needsRenderPass = false;
                defaultRendererRequirements.needsReflectionProbeBlending = false;
                defaultRendererRequirements.needsReflectionProbeBoxProjection = false;
                defaultRendererRequirements.renderingMode = RenderingMode.Forward;
                defaultRendererRequirements.needsProcedural = true;

                urpAsset.mainLightRenderingMode = LightRenderingMode.Disabled;
                urpAsset.supportsMainLightShadows = false;
                urpAsset.additionalLightsRenderingMode = LightRenderingMode.Disabled;
                urpAsset.supportsAdditionalLightShadows = false;
                urpAsset.reflectionProbeBlending = false;
                urpAsset.reflectionProbeBoxProjection = false;
                urpAsset.supportsSoftShadows = false;

                rendererData.renderingMode = RenderingMode.Forward;

                ScriptableRenderer.useRenderPassEnabled = false;
                ScriptableRenderer.stripAdditionalLightOffVariants = true;
                ScriptableRenderer.stripShadowsOffVariants = true;

                for (int i = 0; i < rendererFeatures.Count; i++)
                {
                    if (rendererFeatures[i] == null)
                        continue;

                    rendererFeatures[i].SetActive(true);
                }
            }

            internal void Cleanup()
            {
                Object.DestroyImmediate(urpAsset);
                Object.DestroyImmediate(rendererData);
                rendererFeatures.Clear();
                ssaoRendererFeatures.Clear();
            }
        }

        private RenderPipelineAsset m_PreviousRenderPipelineAssetGraphicsSettings;
        private RenderPipelineAsset m_PreviousRenderPipelineAssetQualitySettings;
        private TestHelper m_TestHelper;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            UniversalRenderPipelineGlobalSettings.Ensure();

            m_PreviousRenderPipelineAssetGraphicsSettings = GraphicsSettings.defaultRenderPipeline;
            m_PreviousRenderPipelineAssetQualitySettings = QualitySettings.renderPipeline;

            GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
        }

        [SetUp]
        public void Setup()
        {
            m_TestHelper = new();
            m_TestHelper.ResetData();
        }

        [TearDown]
        public void TearDown()
        {
            m_TestHelper.Cleanup();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            GraphicsSettings.defaultRenderPipeline = m_PreviousRenderPipelineAssetGraphicsSettings;
            QualitySettings.renderPipeline = m_PreviousRenderPipelineAssetQualitySettings;
        }

        [Test]
        public void TestGetSupportedShaderFeaturesFromAsset_NewAsset()
        {
            ShaderFeatures actual = m_TestHelper.GetSupportedShaderFeaturesFromAsset();
            ShaderFeatures expected = m_TestHelper.defaultURPAssetFeatures;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);
        }

        // ShaderFeatures.MainLightShadowsCascade - ShaderFeatures.MainLightShadowsCascade
        [Test]
        public void TestGetSupportedShaderFeaturesFromAsset_MainLightShadowCascade()
        {
            m_TestHelper.urpAsset.mainLightRenderingMode = LightRenderingMode.PerVertex;
            m_TestHelper.urpAsset.supportsMainLightShadows = true;
            ShaderFeatures actual = m_TestHelper.GetSupportedShaderFeaturesFromAsset();
            ShaderFeatures expected = m_TestHelper.defaultURPAssetFeatures;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            m_TestHelper.urpAsset.mainLightRenderingMode = LightRenderingMode.PerPixel;
            m_TestHelper.urpAsset.supportsMainLightShadows = true;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromAsset();
            expected = m_TestHelper.defaultURPAssetFeatures | ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);
        }

        // ShaderFeatures.AdditionalLightsVertex & ShaderFeatures.AdditionalLights
        [Test]
        public void TestGetSupportedShaderFeaturesFromAsset_AdditionalLights()
        {
            m_TestHelper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerVertex;
            ShaderFeatures actual = m_TestHelper.GetSupportedShaderFeaturesFromAsset();
            ShaderFeatures expected = m_TestHelper.defaultURPAssetFeatures | ShaderFeatures.AdditionalLightsVertex;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            m_TestHelper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerPixel;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromAsset();
            expected = m_TestHelper.defaultURPAssetFeatures | ShaderFeatures.AdditionalLightsPixel;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);
        }

        // ShaderFeatures.SoftShadows - _SHADOWS_SOFT
        [Test]
        public void TestGetSupportedShaderFeaturesFromAsset_SoftShadows()
        {
            // Main Light
            m_TestHelper.urpAsset.mainLightRenderingMode = LightRenderingMode.Disabled;
            m_TestHelper.urpAsset.supportsSoftShadows = true;
            ShaderFeatures actual = m_TestHelper.GetSupportedShaderFeaturesFromAsset();
            ShaderFeatures expected = m_TestHelper.defaultURPAssetFeatures;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            m_TestHelper.urpAsset.mainLightRenderingMode = LightRenderingMode.PerVertex;
            m_TestHelper.urpAsset.supportsSoftShadows = true;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromAsset();
            expected = m_TestHelper.defaultURPAssetFeatures;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            m_TestHelper.urpAsset.mainLightRenderingMode = LightRenderingMode.PerPixel;
            m_TestHelper.urpAsset.supportsMainLightShadows = true;
            m_TestHelper.urpAsset.supportsSoftShadows = false;
            expected = m_TestHelper.defaultURPAssetFeatures | ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromAsset();
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            m_TestHelper.urpAsset.mainLightRenderingMode = LightRenderingMode.PerPixel;
            m_TestHelper.urpAsset.supportsMainLightShadows = true;
            m_TestHelper.urpAsset.supportsSoftShadows = true;
            expected = m_TestHelper.defaultURPAssetFeatures | ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.SoftShadows;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromAsset();
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // Additional Light
            m_TestHelper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.Disabled;
            m_TestHelper.urpAsset.supportsAdditionalLightShadows = true;
            m_TestHelper.urpAsset.supportsSoftShadows = true;
            expected = m_TestHelper.defaultURPAssetFeatures;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromAsset();
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            m_TestHelper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerVertex;
            m_TestHelper.urpAsset.supportsAdditionalLightShadows = true;
            m_TestHelper.urpAsset.supportsSoftShadows = true;
            expected = m_TestHelper.defaultURPAssetFeatures | ShaderFeatures.AdditionalLightsVertex;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromAsset();
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            m_TestHelper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerPixel;
            m_TestHelper.urpAsset.supportsAdditionalLightShadows = true;
            m_TestHelper.urpAsset.supportsSoftShadows = false;
            expected = m_TestHelper.defaultURPAssetFeatures | ShaderFeatures.AdditionalLightShadows | ShaderFeatures.AdditionalLightsPixel;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromAsset();
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            m_TestHelper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerPixel;
            m_TestHelper.urpAsset.supportsAdditionalLightShadows = true;
            m_TestHelper.urpAsset.supportsSoftShadows = true;
            expected = m_TestHelper.defaultURPAssetFeatures | ShaderFeatures.AdditionalLightShadows | ShaderFeatures.AdditionalLightsPixel | ShaderFeatures.SoftShadows;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromAsset();
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);
        }

        [Test]
        public void TestGetSupportedShaderFeaturesFromAsset_HighDynamicRange()
        {
            m_TestHelper.urpAsset.colorGradingMode = ColorGradingMode.LowDynamicRange;
            ShaderFeatures actual = m_TestHelper.GetSupportedShaderFeaturesFromAsset();
            ShaderFeatures expected = m_TestHelper.defaultURPAssetFeatures;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            m_TestHelper.urpAsset.colorGradingMode = ColorGradingMode.HighDynamicRange;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromAsset();
            expected = m_TestHelper.defaultURPAssetFeatures | ShaderFeatures.HdrGrading;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);
        }

        [Test]
        public void TestGetRendererRequirements()
        {
            // Forward
            RendererRequirements actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(m_TestHelper.defaultRendererRequirements, actual);

            // MSAA Sample Count
            m_TestHelper.rendererData.renderingMode = RenderingMode.ForwardPlus;
            RendererRequirements expected = m_TestHelper.defaultRendererRequirements;
            expected.renderingMode = m_TestHelper.rendererData.renderingMode;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            // Forward Plus
            m_TestHelper.rendererData.renderingMode = RenderingMode.ForwardPlus;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.renderingMode = m_TestHelper.rendererData.renderingMode;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            // Deferred
            m_TestHelper.rendererData.renderingMode = RenderingMode.Deferred;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.renderingMode = m_TestHelper.rendererData.renderingMode;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            // Native Render Pass
            m_TestHelper.rendererData.renderingMode = RenderingMode.Forward;
            m_TestHelper.ScriptableRenderer.useRenderPassEnabled = true;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.renderingMode = m_TestHelper.rendererData.renderingMode;
            expected.needsRenderPass = false;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            m_TestHelper.rendererData.renderingMode = RenderingMode.ForwardPlus;
            m_TestHelper.ScriptableRenderer.useRenderPassEnabled = true;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.renderingMode = m_TestHelper.rendererData.renderingMode;
            expected.needsRenderPass = false;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            m_TestHelper.rendererData.renderingMode = RenderingMode.Deferred;
            m_TestHelper.ScriptableRenderer.useRenderPassEnabled = true;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.renderingMode = m_TestHelper.rendererData.renderingMode;
            expected.needsRenderPass = m_TestHelper.ScriptableRenderer.useRenderPassEnabled;;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            // Reflection Probe Blending
            m_TestHelper.urpAsset.reflectionProbeBlending = false;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.needsReflectionProbeBlending = m_TestHelper.urpAsset.reflectionProbeBlending;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            m_TestHelper.urpAsset.reflectionProbeBlending = true;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.needsReflectionProbeBlending = m_TestHelper.urpAsset.reflectionProbeBlending;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            // Reflection Probe Box Projection
            m_TestHelper.urpAsset.reflectionProbeBoxProjection = false;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.needsReflectionProbeBoxProjection = m_TestHelper.urpAsset.reflectionProbeBoxProjection;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            m_TestHelper.urpAsset.reflectionProbeBoxProjection = true;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.needsReflectionProbeBoxProjection = m_TestHelper.urpAsset.reflectionProbeBoxProjection;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            // Soft shadows
            m_TestHelper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerVertex;
            m_TestHelper.urpAsset.supportsAdditionalLightShadows = false;
            m_TestHelper.urpAsset.supportsSoftShadows = true;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.needsSoftShadows = false;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            m_TestHelper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerPixel;
            m_TestHelper.urpAsset.supportsAdditionalLightShadows = false;
            m_TestHelper.urpAsset.supportsSoftShadows = true;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.needsSoftShadows = false;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            m_TestHelper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerVertex;
            m_TestHelper.urpAsset.supportsAdditionalLightShadows = true;
            m_TestHelper.urpAsset.supportsSoftShadows = true;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.needsSoftShadows = false;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            m_TestHelper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerPixel;
            m_TestHelper.urpAsset.supportsAdditionalLightShadows = true;
            m_TestHelper.urpAsset.supportsSoftShadows = true;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.needsAdditionalLightShadows = true;
            expected.needsSoftShadows = true;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            m_TestHelper.rendererData.renderingMode = RenderingMode.ForwardPlus;
            m_TestHelper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerVertex;
            m_TestHelper.urpAsset.supportsAdditionalLightShadows = false;
            m_TestHelper.urpAsset.supportsSoftShadows = true;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.renderingMode = m_TestHelper.rendererData.renderingMode;
            expected.needsSoftShadows = false;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            m_TestHelper.rendererData.renderingMode = RenderingMode.ForwardPlus;
            m_TestHelper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerVertex;
            m_TestHelper.urpAsset.supportsAdditionalLightShadows = true;
            m_TestHelper.urpAsset.supportsSoftShadows = true;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.renderingMode = m_TestHelper.rendererData.renderingMode;
            expected.needsAdditionalLightShadows = true;
            expected.needsSoftShadows = true;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            // Shadows Off
            m_TestHelper.ScriptableRenderer.stripShadowsOffVariants = false;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.needsShadowsOff = true;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            m_TestHelper.ScriptableRenderer.stripShadowsOffVariants = true;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.needsShadowsOff = false;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            // Additional Lights Off
            m_TestHelper.ScriptableRenderer.stripAdditionalLightOffVariants = false;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.needsAdditionalLightsOff = true;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);

            m_TestHelper.ScriptableRenderer.stripAdditionalLightOffVariants = true;
            expected = m_TestHelper.defaultRendererRequirements;
            expected.needsAdditionalLightsOff = false;
            actual = m_TestHelper.GetRendererRequirements();
            m_TestHelper.AssertRendererRequirementsAndReset(expected, actual);
        }

        [Test]
        public void TestGetSupportedShaderFeaturesFromRenderer()
        {
            // Initial state
            RendererRequirements rendererRequirements = m_TestHelper.defaultRendererRequirements;
            ShaderFeatures actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            ShaderFeatures expected = ShaderFeatures.DrawProcedural;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // Procedural...
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsProcedural = false;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.None;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsProcedural = true;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // Rendering Modes...
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Forward;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.ForwardPlus;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.ForwardPlus | ShaderFeatures.AdditionalLightsKeepOffVariants;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.DeferredShading;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // The Off variant for Additional Lights
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsAdditionalLightsOff = false;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsAdditionalLightsOff = true;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.AdditionalLightsKeepOffVariants;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // The Off variant for Main and Additional Light shadows
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsShadowsOff = false;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsShadowsOff = true;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.ShadowsKeepOffVariants;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // Soft shadows
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsSoftShadows = false;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsSoftShadows = true;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.SoftShadows;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // Deferred GBuffer Rendering Layers
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsGBufferRenderingLayers = false;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsGBufferRenderingLayers = true;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.GBufferWriteRenderingLayers;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // Deferred GBuffer Accurate Normals
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsGBufferAccurateNormals = false;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsGBufferAccurateNormals = true;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.AccurateGbufferNormals;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // Deferred GBuffer Native Render Pass
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsRenderPass = false;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsRenderPass = true;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.RenderPassEnabled;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // Reflection Probe Blending
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsReflectionProbeBlending = false;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsReflectionProbeBlending = true;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.ReflectionProbeBlending;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // Reflection Probe Box Projection
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsReflectionProbeBoxProjection = false;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.needsReflectionProbeBoxProjection = true;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.ReflectionProbeBoxProjection;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);
        }

        [Test]
        public void TestStripUnusedPostProcessingVariants_ReturnsAll()
        {
            VolumeFeatures volumeFeatures = VolumeFeatures.None;
            m_TestHelper.GetEveryVolumeFeatures(ref volumeFeatures);
            m_TestHelper.AssertVolumeFeatures(VolumeFeatures.All, volumeFeatures);
        }
        [Test]
        public void TestStripUnusedVariants_ReturnsAll()
        {
            List<ShaderFeatures> rendererFeaturesList = new List<ShaderFeatures>(1);
            UniversalRenderPipelineAsset.ShaderPrefilteringData prefilteringData = new();
            m_TestHelper.GetEveryShaderFeatureAndPrefilteringData(rendererFeaturesList, ref prefilteringData);

            Assert.AreEqual(1, rendererFeaturesList.Count);
            m_TestHelper.AssertShaderFeatures(ShaderFeatures.All, rendererFeaturesList[0]);
        }

        [Test]
        public void TestGetSupportedShaderFeaturesFromRendererFeatures_NoFeatures()
        {
            // Initial state
            RendererRequirements rendererRequirements = m_TestHelper.defaultRendererRequirements;
            ShaderFeatures actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            ShaderFeatures expected = ShaderFeatures.None;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);
        }

        // Tests if we handle Renderer Features that are null
        [Test]
        public void TestGetSupportedShaderFeaturesFromRendererFeatures_Null()
        {
            m_TestHelper.rendererFeatures.Add(null);
            RendererRequirements rendererRequirements = m_TestHelper.defaultRendererRequirements;
            ShaderFeatures actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            ShaderFeatures expected = ShaderFeatures.None;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);
        }

        [Test]
        public  void TestGetSupportedShaderFeaturesFromRendererFeatures_RenderingLayers()
        {
            DecalRendererFeature decalFeature = ScriptableObject.CreateInstance<DecalRendererFeature>();
            decalFeature.settings = new DecalSettings()
            {
                technique = DecalTechniqueOption.DBuffer,
                maxDrawDistance = 1000f,
                decalLayers = false,
                dBufferSettings = new DBufferSettings(),
                screenSpaceSettings = new DecalScreenSpaceSettings(),
            };
            m_TestHelper.rendererFeatures.Add(decalFeature);

            // DBuffer
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.DBuffer;
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.decalLayers = false;
            RendererRequirements rendererRequirements = m_TestHelper.defaultRendererRequirements;
            ShaderFeatures actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            ShaderFeatures expected = ShaderFeatures.DBufferMRT3;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.DBuffer;
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.decalLayers = true;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalLayers | ShaderFeatures.DepthNormalPassRenderingLayers | ShaderFeatures.DBufferMRT3;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.DBuffer;
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.decalLayers = true;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalLayers | ShaderFeatures.DepthNormalPassRenderingLayers | ShaderFeatures.GBufferWriteRenderingLayers | ShaderFeatures.DBufferMRT3;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // Screenspace
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.ScreenSpace;
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.decalLayers = false;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalScreenSpace | ShaderFeatures.DecalNormalBlendLow;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.ScreenSpace;
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.decalLayers = true;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalScreenSpace | ShaderFeatures.DecalLayers | ShaderFeatures.OpaqueWriteRenderingLayers | ShaderFeatures.DecalNormalBlendLow;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.ScreenSpace;
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.decalLayers = true;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalGBuffer | ShaderFeatures.DecalNormalBlendLow | ShaderFeatures.DecalLayers | ShaderFeatures.DepthNormalPassRenderingLayers | ShaderFeatures.GBufferWriteRenderingLayers;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // Test with DepthNormal prepass enabled (Is enabled implicitly by SSAO f.ex)
            ScreenSpaceAmbientOcclusion ssaoFeature = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            ssaoFeature.settings = new ScreenSpaceAmbientOcclusionSettings()
            {
                AOMethod = ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise,
                Downsample = false,
                AfterOpaque = false,
                Source = ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals,
                NormalSamples = ScreenSpaceAmbientOcclusionSettings.NormalQuality.Medium,
                Intensity = 3.0f,
                DirectLightingStrength = 0.25f,
                Radius = 0.035f,
                Samples = ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Medium,
                BlurQuality = ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.High,
                Falloff = 100f,
            };
            ssaoFeature.SetActive(true);
            m_TestHelper.rendererFeatures.Add(ssaoFeature);

            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.ScreenSpace;
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.decalLayers = true;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.ScreenSpaceOcclusion | ShaderFeatures.DecalScreenSpace |
                       ShaderFeatures.DecalNormalBlendLow | ShaderFeatures.DecalLayers |
                       ShaderFeatures.OpaqueWriteRenderingLayers;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            m_TestHelper.rendererFeatures.Remove(ssaoFeature);
        }

        [Test]
        public void TestGetSupportedShaderFeaturesFromRendererFeatures_ScreenSpaceShadows()
        {
            ScreenSpaceShadows screenSpaceShadowsFeature = ScriptableObject.CreateInstance<ScreenSpaceShadows>();
            m_TestHelper.rendererFeatures.Add(screenSpaceShadowsFeature);

            // Initial
            RendererRequirements rendererRequirements = m_TestHelper.defaultRendererRequirements;
            ShaderFeatures actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            ShaderFeatures expected = ShaderFeatures.ScreenSpaceShadows;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            m_TestHelper.rendererFeatures[0].SetActive(false);
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.None;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);
        }

        // Screen Space Ambient Occlusion (SSAO)...
        [Test]
        public void TestGetSupportedShaderFeaturesFromRendererFeatures_SSAO()
        {
            ScreenSpaceAmbientOcclusion ssaoFeature = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            ssaoFeature.settings = new ScreenSpaceAmbientOcclusionSettings()
            {
                AOMethod = ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise,
                Downsample = false,
                AfterOpaque = false,
                Source = ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals,
                NormalSamples = ScreenSpaceAmbientOcclusionSettings.NormalQuality.Medium,
                Intensity = 3.0f,
                DirectLightingStrength = 0.25f,
                Radius = 0.035f,
                Samples = ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Medium,
                BlurQuality = ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.High,
                Falloff = 100f,
            };
            m_TestHelper.rendererFeatures.Add(ssaoFeature);

            // Enabled feature
            m_TestHelper.rendererFeatures[0].SetActive(true);

            RendererRequirements rendererRequirements = m_TestHelper.defaultRendererRequirements;
            ShaderFeatures actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            ShaderFeatures expected = ShaderFeatures.ScreenSpaceOcclusion;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            ((ScreenSpaceAmbientOcclusion)m_TestHelper.rendererFeatures[0]).settings.AfterOpaque = true;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.ScreenSpaceOcclusionAfterOpaque;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // Disabled feature
            m_TestHelper.rendererFeatures[0].SetActive(false);
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.None;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            m_TestHelper.rendererFeatures[0].SetActive(false);
            ((ScreenSpaceAmbientOcclusion)m_TestHelper.rendererFeatures[0]).settings.AfterOpaque = true;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.None;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            Object.DestroyImmediate(ssaoFeature);
        }

        [Test]
        public void TestGetSupportedShaderFeaturesFromRendererFeatures_Decals()
        {
            DecalRendererFeature decalFeature = ScriptableObject.CreateInstance<DecalRendererFeature>();
            decalFeature.settings = new DecalSettings()
            {
                technique = DecalTechniqueOption.DBuffer,
                maxDrawDistance = 1000f,
                decalLayers = false,
                dBufferSettings = new DBufferSettings(),
                screenSpaceSettings = new DecalScreenSpaceSettings(),
            };
            m_TestHelper.rendererFeatures.Add(decalFeature);

            // TODO Tests for Automatic

            // Initial
            RendererRequirements rendererRequirements = m_TestHelper.defaultRendererRequirements;
            ShaderFeatures actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            ShaderFeatures expected = ShaderFeatures.DBufferMRT3;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            m_TestHelper.rendererFeatures[0].SetActive(false);
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.None;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // DecalTechniqueOption - DBuffer
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.DBuffer;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DBufferMRT3;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.DBuffer;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DBufferMRT3;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // DecalTechniqueOption - ScreenSpace
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.ScreenSpace;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalScreenSpace | ShaderFeatures.DecalNormalBlendLow;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.ScreenSpace;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalNormalBlendLow | ShaderFeatures.DecalGBuffer;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // DBuffer - DecalSurfaceData Albedo
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.DBuffer;

            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.dBufferSettings.surfaceData = DecalSurfaceData.Albedo;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DBufferMRT1;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // DBuffer - DecalSurfaceData AlbedoNormal
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.dBufferSettings.surfaceData = DecalSurfaceData.AlbedoNormal;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DBufferMRT2;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // DBuffer - DecalSurfaceData AlbedoNormalMAOS
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.dBufferSettings.surfaceData = DecalSurfaceData.AlbedoNormalMAOS;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DBufferMRT3;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // ScreenSpace - DecalNormalBlend Low
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.ScreenSpace;

            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.screenSpaceSettings.normalBlend = DecalNormalBlend.Low;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalScreenSpace | ShaderFeatures.DecalNormalBlendLow;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.screenSpaceSettings.normalBlend = DecalNormalBlend.Low;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalGBuffer | ShaderFeatures.DecalNormalBlendLow;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // ScreenSpace - DecalNormalBlend Medium
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.screenSpaceSettings.normalBlend = DecalNormalBlend.Medium;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalScreenSpace | ShaderFeatures.DecalNormalBlendMedium;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.screenSpaceSettings.normalBlend = DecalNormalBlend.Medium;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalGBuffer | ShaderFeatures.DecalNormalBlendMedium;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            // ScreenSpace - DecalNormalBlend High
            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.screenSpaceSettings.normalBlend = DecalNormalBlend.High;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalScreenSpace | ShaderFeatures.DecalNormalBlendHigh;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)m_TestHelper.rendererFeatures[0]).settings.screenSpaceSettings.normalBlend = DecalNormalBlend.High;
            rendererRequirements = m_TestHelper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            actual = m_TestHelper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalGBuffer | ShaderFeatures.DecalNormalBlendHigh;
            m_TestHelper.AssertShaderFeaturesAndReset(expected, actual);
        }
    }
}
