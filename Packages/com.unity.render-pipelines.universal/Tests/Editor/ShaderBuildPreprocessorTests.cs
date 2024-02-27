using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Rendering.Universal;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using RendererRequirements = UnityEditor.Rendering.Universal.ShaderBuildPreprocessor.RendererRequirements;
using DecalSettings = UnityEngine.Rendering.Universal.DecalSettings;

namespace ShaderStrippingAndPrefiltering
{
    class ShaderBuildPreprocessorTests
    {
        internal class TestHelper
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
            internal ShaderFeatures defaultURPAssetFeatures =
                  ShaderFeatures.MainLight
                | ShaderFeatures.MixedLighting
                | ShaderFeatures.TerrainHoles
                | ShaderFeatures.DrawProcedural
                | ShaderFeatures.LightCookies
                | ShaderFeatures.LODCrossFade
                | ShaderFeatures.AutoSHMode
                | ShaderFeatures.DataDrivenLensFlare
                | ShaderFeatures.ScreenSpaceLensFlare;

            internal RendererRequirements defaultRendererRequirements = new()
            {
                msaaSampleCount = 1,
                needsUnusedVariants = false,
                isUniversalRenderer = true,
                needsProcedural = true,
                needsAdditionalLightShadows = false,
                needsSoftShadows = false,
                needsShadowsOff = false,
                needsAdditionalLightsOff = false,
                needsGBufferRenderingLayers = false,
                needsGBufferAccurateNormals = false,
                needsRenderPass = false,
                needsReflectionProbeBlending = false,
                needsReflectionProbeBoxProjection = false,
                renderingMode = RenderingMode.Forward,
            };

            public TestHelper()
            {
                try
                {
                    rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                    scriptableRendererData = rendererData;
                    urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
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
                return ShaderBuildPreprocessor.GetRendererRequirements(ref urpAsset, ref ScriptableRenderer, ref scriptableRendererData, stripUnusedVariants);
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
            internal void AssertShaderFeaturesAndReset(ShaderFeatures expected, ShaderFeatures actual)
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
                ResetData();
            }

            internal void AssertRendererRequirementsAndReset(RendererRequirements expected, RendererRequirements actual)
            {
                Assert.AreEqual(expected.needsUnusedVariants, actual.needsUnusedVariants, "needsUnusedVariants mismatch");
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

                ResetData();
            }

            internal void ResetData()
            {
                urpAsset.mainLightRenderingMode = LightRenderingMode.Disabled;
                urpAsset.supportsMainLightShadows = false;
                urpAsset.additionalLightsRenderingMode = LightRenderingMode.Disabled;
                urpAsset.supportsAdditionalLightShadows = false;
                urpAsset.lightProbeSystem = LightProbeSystem.LegacyLightProbes;
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
                ScriptableObject.DestroyImmediate(urpAsset);
                ScriptableObject.DestroyImmediate(rendererData);
                rendererFeatures.Clear();
                ssaoRendererFeatures.Clear();
            }
        }

        [Test]
        public static void TestGetSupportedShaderFeaturesFromAsset_NewAsset()
        {
            TestHelper helper = new ();
            ShaderFeatures actual;
            ShaderFeatures expected;

            actual = helper.GetSupportedShaderFeaturesFromAsset();
            expected = helper.defaultURPAssetFeatures;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Clean up
            helper.Cleanup();
        }

        // ShaderFeatures.MainLightShadowsCascade - ShaderFeatures.MainLightShadowsCascade
        [Test]
        public static void TestGetSupportedShaderFeaturesFromAsset_MainLightShadowCascade()
        {
            TestHelper helper = new ();
            ShaderFeatures actual;
            ShaderFeatures expected;

            helper.urpAsset.mainLightRenderingMode = LightRenderingMode.PerVertex;
            helper.urpAsset.supportsMainLightShadows = true;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            expected = helper.defaultURPAssetFeatures;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.urpAsset.mainLightRenderingMode = LightRenderingMode.PerPixel;
            helper.urpAsset.supportsMainLightShadows = true;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            expected = helper.defaultURPAssetFeatures | ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Clean up
            helper.Cleanup();
        }

        // ShaderFeatures.AdditionalLightsVertex & ShaderFeatures.AdditionalLights
        [Test]
        public static void TestGetSupportedShaderFeaturesFromAsset_AdditionalLights()
        {
            TestHelper helper = new ();
            ShaderFeatures actual;
            ShaderFeatures expected;

            helper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerVertex;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            expected = helper.defaultURPAssetFeatures | ShaderFeatures.AdditionalLightsVertex;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerPixel;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            expected = helper.defaultURPAssetFeatures | ShaderFeatures.AdditionalLightsPixel;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Clean up
            helper.Cleanup();
        }

        // ShaderFeatures.SoftShadows - _SHADOWS_SOFT
        [Test]
        public static void TestGetSupportedShaderFeaturesFromAsset_SoftShadows()
        {
            TestHelper helper = new ();
            ShaderFeatures actual;
            ShaderFeatures expected;

            // Main Light
            helper.urpAsset.mainLightRenderingMode = LightRenderingMode.Disabled;
            helper.urpAsset.supportsSoftShadows = true;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            expected = helper.defaultURPAssetFeatures;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.urpAsset.mainLightRenderingMode = LightRenderingMode.PerVertex;
            helper.urpAsset.supportsSoftShadows = true;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            expected = helper.defaultURPAssetFeatures;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.urpAsset.mainLightRenderingMode = LightRenderingMode.PerPixel;
            helper.urpAsset.supportsMainLightShadows = true;
            helper.urpAsset.supportsSoftShadows = false;
            expected = helper.defaultURPAssetFeatures | ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.urpAsset.mainLightRenderingMode = LightRenderingMode.PerPixel;
            helper.urpAsset.supportsMainLightShadows = true;
            helper.urpAsset.supportsSoftShadows = true;
            expected = helper.defaultURPAssetFeatures | ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.SoftShadows;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Additional Light
            helper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.Disabled;
            helper.urpAsset.supportsAdditionalLightShadows = true;
            helper.urpAsset.supportsSoftShadows = true;
            expected = helper.defaultURPAssetFeatures;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerVertex;
            helper.urpAsset.supportsAdditionalLightShadows = true;
            helper.urpAsset.supportsSoftShadows = true;
            expected = helper.defaultURPAssetFeatures | ShaderFeatures.AdditionalLightsVertex;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerPixel;
            helper.urpAsset.supportsAdditionalLightShadows = true;
            helper.urpAsset.supportsSoftShadows = false;
            expected = helper.defaultURPAssetFeatures | ShaderFeatures.AdditionalLightShadows | ShaderFeatures.AdditionalLightsPixel;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerPixel;
            helper.urpAsset.supportsAdditionalLightShadows = true;
            helper.urpAsset.supportsSoftShadows = true;
            expected = helper.defaultURPAssetFeatures | ShaderFeatures.AdditionalLightShadows | ShaderFeatures.AdditionalLightsPixel | ShaderFeatures.SoftShadows;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Clean up
            helper.Cleanup();
        }

        [Test]
        public static void TestGetSupportedShaderFeaturesFromAsset_ProbeVolumes()
        {
            TestHelper helper = new ();
            ShaderFeatures actual;
            ShaderFeatures expected;

            helper.urpAsset.lightProbeSystem = LightProbeSystem.LegacyLightProbes;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            expected = helper.defaultURPAssetFeatures;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.urpAsset.lightProbeSystem = LightProbeSystem.ProbeVolumes;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            expected = helper.defaultURPAssetFeatures | ShaderFeatures.ProbeVolumeL1;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.urpAsset.lightProbeSystem = LightProbeSystem.ProbeVolumes;
            helper.urpAsset.probeVolumeSHBands = ProbeVolumeSHBands.SphericalHarmonicsL2;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            expected = helper.defaultURPAssetFeatures | ShaderFeatures.ProbeVolumeL2;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Clean up
            helper.Cleanup();
        }

        [Test]
        public static void TestGetSupportedShaderFeaturesFromAsset_HighDynamicRange()
        {
            TestHelper helper = new ();
            ShaderFeatures actual;
            ShaderFeatures expected;

            helper.urpAsset.colorGradingMode = ColorGradingMode.LowDynamicRange;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            expected = helper.defaultURPAssetFeatures;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.urpAsset.colorGradingMode = ColorGradingMode.HighDynamicRange;
            actual = helper.GetSupportedShaderFeaturesFromAsset();
            expected = helper.defaultURPAssetFeatures | ShaderFeatures.HdrGrading;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Clean up
            helper.Cleanup();
        }

        [Test]
        public void TestGetRendererRequirements()
        {
            TestHelper helper = new ();
            RendererRequirements actual;
            RendererRequirements expected;

            // Forward
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(helper.defaultRendererRequirements, actual);

            // MSAA Sample Count
            helper.rendererData.renderingMode = RenderingMode.ForwardPlus;
            expected = helper.defaultRendererRequirements;
            expected.renderingMode = helper.rendererData.renderingMode;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            // Forward Plus
            helper.rendererData.renderingMode = RenderingMode.ForwardPlus;
            expected = helper.defaultRendererRequirements;
            expected.renderingMode = helper.rendererData.renderingMode;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            // Deferred
            helper.rendererData.renderingMode = RenderingMode.Deferred;
            expected = helper.defaultRendererRequirements;
            expected.renderingMode = helper.rendererData.renderingMode;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            // Native Render Pass
            helper.rendererData.renderingMode = RenderingMode.Forward;
            helper.ScriptableRenderer.useRenderPassEnabled = true;
            expected = helper.defaultRendererRequirements;
            expected.renderingMode = helper.rendererData.renderingMode;
            expected.needsRenderPass = false;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            helper.rendererData.renderingMode = RenderingMode.ForwardPlus;
            helper.ScriptableRenderer.useRenderPassEnabled = true;
            expected = helper.defaultRendererRequirements;
            expected.renderingMode = helper.rendererData.renderingMode;
            expected.needsRenderPass = false;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            helper.rendererData.renderingMode = RenderingMode.Deferred;
            helper.ScriptableRenderer.useRenderPassEnabled = true;
            expected = helper.defaultRendererRequirements;
            expected.renderingMode = helper.rendererData.renderingMode;
            expected.needsRenderPass = helper.ScriptableRenderer.useRenderPassEnabled;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            // Reflection Probe Blending
            helper.urpAsset.reflectionProbeBlending = false;
            expected = helper.defaultRendererRequirements;
            expected.needsReflectionProbeBlending = helper.urpAsset.reflectionProbeBlending;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            helper.urpAsset.reflectionProbeBlending = true;
            expected = helper.defaultRendererRequirements;
            expected.needsReflectionProbeBlending = helper.urpAsset.reflectionProbeBlending;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            // Reflection Probe Box Projection
            helper.urpAsset.reflectionProbeBoxProjection = false;
            expected = helper.defaultRendererRequirements;
            expected.needsReflectionProbeBoxProjection = helper.urpAsset.reflectionProbeBoxProjection;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            helper.urpAsset.reflectionProbeBoxProjection = true;
            expected = helper.defaultRendererRequirements;
            expected.needsReflectionProbeBoxProjection = helper.urpAsset.reflectionProbeBoxProjection;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            // Soft shadows
            helper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerVertex;
            helper.urpAsset.supportsAdditionalLightShadows = false;
            helper.urpAsset.supportsSoftShadows = true;
            expected = helper.defaultRendererRequirements;
            expected.needsSoftShadows = false;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            helper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerPixel;
            helper.urpAsset.supportsAdditionalLightShadows = false;
            helper.urpAsset.supportsSoftShadows = true;
            expected = helper.defaultRendererRequirements;
            expected.needsSoftShadows = false;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            helper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerVertex;
            helper.urpAsset.supportsAdditionalLightShadows = true;
            helper.urpAsset.supportsSoftShadows = true;
            expected = helper.defaultRendererRequirements;
            expected.needsSoftShadows = false;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            helper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerPixel;
            helper.urpAsset.supportsAdditionalLightShadows = true;
            helper.urpAsset.supportsSoftShadows = true;
            expected = helper.defaultRendererRequirements;
            expected.needsAdditionalLightShadows = true;
            expected.needsSoftShadows = true;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            helper.rendererData.renderingMode = RenderingMode.ForwardPlus;
            helper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerVertex;
            helper.urpAsset.supportsAdditionalLightShadows = false;
            helper.urpAsset.supportsSoftShadows = true;
            expected = helper.defaultRendererRequirements;
            expected.renderingMode = helper.rendererData.renderingMode;
            expected.needsSoftShadows = false;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            helper.rendererData.renderingMode = RenderingMode.ForwardPlus;
            helper.urpAsset.additionalLightsRenderingMode = LightRenderingMode.PerVertex;
            helper.urpAsset.supportsAdditionalLightShadows = true;
            helper.urpAsset.supportsSoftShadows = true;
            expected = helper.defaultRendererRequirements;
            expected.renderingMode = helper.rendererData.renderingMode;
            expected.needsAdditionalLightShadows = true;
            expected.needsSoftShadows = true;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            // Shadows Off
            helper.ScriptableRenderer.stripShadowsOffVariants = false;
            expected = helper.defaultRendererRequirements;
            expected.needsShadowsOff = true;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            helper.ScriptableRenderer.stripShadowsOffVariants = true;
            expected = helper.defaultRendererRequirements;
            expected.needsShadowsOff = false;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            // Additional Lights Off
            helper.ScriptableRenderer.stripAdditionalLightOffVariants = false;
            expected = helper.defaultRendererRequirements;
            expected.needsAdditionalLightsOff = true;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            helper.ScriptableRenderer.stripAdditionalLightOffVariants = true;
            expected = helper.defaultRendererRequirements;
            expected.needsAdditionalLightsOff = false;
            actual = helper.GetRendererRequirements();
            helper.AssertRendererRequirementsAndReset(expected, actual);

            // Clean up
            helper.Cleanup();
        }

        [Test]
        public void TestGetSupportedShaderFeaturesFromRenderer()
        {
            TestHelper helper = new ();
            ShaderFeatures actual;
            ShaderFeatures expected;
            RendererRequirements rendererRequirements;

            // Initial state
            rendererRequirements = helper.defaultRendererRequirements;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Procedural...
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsProcedural = false;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.None;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsProcedural = true;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Rendering Modes...
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Forward;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.ForwardPlus;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.ForwardPlus | ShaderFeatures.AdditionalLightsKeepOffVariants;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.DeferredShading;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // The Off variant for Additional Lights
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsAdditionalLightsOff = false;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsAdditionalLightsOff = true;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.AdditionalLightsKeepOffVariants;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // The Off variant for Main and Additional Light shadows
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsShadowsOff = false;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsShadowsOff = true;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.ShadowsKeepOffVariants;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Soft shadows
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsSoftShadows = false;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsSoftShadows = true;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.SoftShadows;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Deferred GBuffer Rendering Layers
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsGBufferRenderingLayers = false;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsGBufferRenderingLayers = true;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.GBufferWriteRenderingLayers;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Deferred GBuffer Accurate Normals
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsGBufferAccurateNormals = false;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsGBufferAccurateNormals = true;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.AccurateGbufferNormals;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Deferred GBuffer Native Render Pass
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsRenderPass = false;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsRenderPass = true;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.RenderPassEnabled;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Reflection Probe Blending
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsReflectionProbeBlending = false;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsReflectionProbeBlending = true;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.ReflectionProbeBlending;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Reflection Probe Box Projection
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsReflectionProbeBoxProjection = false;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsReflectionProbeBoxProjection = true;
            actual = helper.GetSupportedShaderFeaturesFromRenderer(rendererRequirements, ShaderFeatures.None);
            expected = ShaderFeatures.DrawProcedural | ShaderFeatures.ReflectionProbeBoxProjection;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.Cleanup();
        }


        [Test]
        public static void TestGetSupportedShaderFeaturesFromRendererFeatures_NoFeatures()
        {
            TestHelper helper = new ();
            ShaderFeatures actual;
            ShaderFeatures expected;
            RendererRequirements rendererRequirements;

            // Initial state
            rendererRequirements = helper.defaultRendererRequirements;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.None;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.Cleanup();
        }

        // Tests if we handle Renderer Features that are null
        [Test]
        public static void TestGetSupportedShaderFeaturesFromRendererFeatures_Null()
        {
            TestHelper helper = new ();
            ShaderFeatures actual;
            ShaderFeatures expected;
            RendererRequirements rendererRequirements;

            rendererRequirements = helper.defaultRendererRequirements;

            helper.rendererFeatures.Add(null);
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.None;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.rendererFeatures.Add(null);
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.None;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.Cleanup();
        }

        [Test]
        public  static void TestGetSupportedShaderFeaturesFromRendererFeatures_RenderingLayers()
        {
            TestHelper helper = new ();
            RendererRequirements rendererRequirements;
            ShaderFeatures actual;
            ShaderFeatures expected;
            ShaderFeatures expectedWithUnused = ShaderFeatures.DBufferMRT1
                                              | ShaderFeatures.DBufferMRT1
                                              | ShaderFeatures.DBufferMRT2
                                              | ShaderFeatures.DBufferMRT3
                                              | ShaderFeatures.DecalScreenSpace
                                              | ShaderFeatures.DecalNormalBlendLow
                                              | ShaderFeatures.DecalNormalBlendMedium
                                              | ShaderFeatures.DecalNormalBlendHigh
                                              | ShaderFeatures.DecalGBuffer
                                              | ShaderFeatures.DecalLayers;

            DecalRendererFeature decalFeature = ScriptableObject.CreateInstance<DecalRendererFeature>();
            decalFeature.settings = new DecalSettings()
            {
                technique = DecalTechniqueOption.DBuffer,
                maxDrawDistance = 1000f,
                decalLayers = false,
                dBufferSettings = new DBufferSettings(),
                screenSpaceSettings = new DecalScreenSpaceSettings(),
            };
            helper.rendererFeatures.Add(decalFeature);

            // Needs unused variants
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.DBuffer;
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.decalLayers = false;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = expectedWithUnused;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // DBuffer
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.DBuffer;
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.decalLayers = false;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DBufferMRT3;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.DBuffer;
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.decalLayers = true;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalLayers | ShaderFeatures.DepthNormalPassRenderingLayers | ShaderFeatures.DBufferMRT3;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.DBuffer;
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.decalLayers = true;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalLayers | ShaderFeatures.DepthNormalPassRenderingLayers | ShaderFeatures.GBufferWriteRenderingLayers | ShaderFeatures.DBufferMRT3;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Screenspace
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.ScreenSpace;
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.decalLayers = false;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalScreenSpace | ShaderFeatures.DecalNormalBlendLow;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.ScreenSpace;
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.decalLayers = true;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalScreenSpace | ShaderFeatures.DecalLayers | ShaderFeatures.OpaqueWriteRenderingLayers | ShaderFeatures.DecalNormalBlendLow;;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.ScreenSpace;
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.decalLayers = true;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalGBuffer | ShaderFeatures.DecalNormalBlendLow | ShaderFeatures.DecalLayers | ShaderFeatures.DepthNormalPassRenderingLayers | ShaderFeatures.GBufferWriteRenderingLayers;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Clean up
            helper.Cleanup();
        }

        [Test]
        public  static void TestGetSupportedShaderFeaturesFromRendererFeatures_ScreenSpaceShadows()
        {
            TestHelper helper = new ();
            RendererRequirements rendererRequirements;
            ShaderFeatures actual;
            ShaderFeatures expected;

            ScreenSpaceShadows screenSpaceShadowsFeature = ScriptableObject.CreateInstance<ScreenSpaceShadows>();
            helper.rendererFeatures.Add(screenSpaceShadowsFeature);

            // Initial
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.ScreenSpaceShadows;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.ScreenSpaceShadows;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.rendererFeatures[0].SetActive(false);
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.None;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.rendererFeatures[0].SetActive(false);
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.ScreenSpaceShadows;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.Cleanup();
        }

        // Screen Space Ambient Occlusion (SSAO)...
        [Test]
        public  static void TestGetSupportedShaderFeaturesFromRendererFeatures_SSAO()
        {
            TestHelper helper = new ();
            RendererRequirements rendererRequirements;
            ShaderFeatures actual;
            ShaderFeatures expected;

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
            helper.rendererFeatures.Add(ssaoFeature);

            // Enabled feature
            helper.rendererFeatures[0].SetActive(true);

            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.ScreenSpaceOcclusion;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.ScreenSpaceOcclusion | ShaderFeatures.ScreenSpaceOcclusionAfterOpaque;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((ScreenSpaceAmbientOcclusion)helper.rendererFeatures[0]).settings.AfterOpaque = true;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.ScreenSpaceOcclusionAfterOpaque;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((ScreenSpaceAmbientOcclusion)helper.rendererFeatures[0]).settings.AfterOpaque = true;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.ScreenSpaceOcclusion | ShaderFeatures.ScreenSpaceOcclusionAfterOpaque;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // Disabled feature
            helper.rendererFeatures[0].SetActive(false);
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.None;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.rendererFeatures[0].SetActive(false);
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.ScreenSpaceOcclusion | ShaderFeatures.ScreenSpaceOcclusionAfterOpaque;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.rendererFeatures[0].SetActive(false);
            ((ScreenSpaceAmbientOcclusion)helper.rendererFeatures[0]).settings.AfterOpaque = true;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.None;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.rendererFeatures[0].SetActive(false);
            ((ScreenSpaceAmbientOcclusion)helper.rendererFeatures[0]).settings.AfterOpaque = true;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.ScreenSpaceOcclusion | ShaderFeatures.ScreenSpaceOcclusionAfterOpaque;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ScriptableObject.DestroyImmediate(ssaoFeature);
            helper.Cleanup();
        }

        [Test]
        public  static void TestGetSupportedShaderFeaturesFromRendererFeatures_Decals()
        {
            TestHelper helper = new ();
            RendererRequirements rendererRequirements;
            ShaderFeatures actual;
            ShaderFeatures expected;
            ShaderFeatures expectedWithUnused = ShaderFeatures.DBufferMRT1
                                              | ShaderFeatures.DBufferMRT1
                                              | ShaderFeatures.DBufferMRT2
                                              | ShaderFeatures.DBufferMRT3
                                              | ShaderFeatures.DecalScreenSpace
                                              | ShaderFeatures.DecalNormalBlendLow
                                              | ShaderFeatures.DecalNormalBlendMedium
                                              | ShaderFeatures.DecalNormalBlendHigh
                                              | ShaderFeatures.DecalGBuffer
                                              | ShaderFeatures.DecalLayers;

            DecalRendererFeature decalFeature = ScriptableObject.CreateInstance<DecalRendererFeature>();
            decalFeature.settings = new DecalSettings()
            {
                technique = DecalTechniqueOption.DBuffer,
                maxDrawDistance = 1000f,
                decalLayers = false,
                dBufferSettings = new DBufferSettings(),
                screenSpaceSettings = new DecalScreenSpaceSettings(),
            };
            helper.rendererFeatures.Add(decalFeature);

            // TODO Tests for Automatic

            // Initial
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DBufferMRT3;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = expectedWithUnused;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.rendererFeatures[0].SetActive(false);
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.None;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.rendererFeatures[0].SetActive(false);
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = expectedWithUnused;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // DecalTechniqueOption - DBuffer
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.DBuffer;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DBufferMRT3;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.DBuffer;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = expectedWithUnused;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.DBuffer;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DBufferMRT3;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.DBuffer;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = expectedWithUnused;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // DecalTechniqueOption - ScreenSpace
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.ScreenSpace;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalScreenSpace | ShaderFeatures.DecalNormalBlendLow;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.ScreenSpace;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = expectedWithUnused;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.ScreenSpace;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalNormalBlendLow | ShaderFeatures.DecalGBuffer;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.ScreenSpace;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = expectedWithUnused;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // DBuffer - DecalSurfaceData Albedo
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.DBuffer;

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.dBufferSettings.surfaceData = DecalSurfaceData.Albedo;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DBufferMRT1;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.dBufferSettings.surfaceData = DecalSurfaceData.Albedo;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = expectedWithUnused;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // DBuffer - DecalSurfaceData AlbedoNormal
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.dBufferSettings.surfaceData = DecalSurfaceData.AlbedoNormal;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DBufferMRT2;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.dBufferSettings.surfaceData = DecalSurfaceData.AlbedoNormal;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = expectedWithUnused;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // DBuffer - DecalSurfaceData AlbedoNormalMAOS
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.dBufferSettings.surfaceData = DecalSurfaceData.AlbedoNormalMAOS;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DBufferMRT3;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.dBufferSettings.surfaceData = DecalSurfaceData.AlbedoNormalMAOS;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = expectedWithUnused;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // ScreenSpace - DecalNormalBlend Low
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.technique = DecalTechniqueOption.ScreenSpace;

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.screenSpaceSettings.normalBlend = DecalNormalBlend.Low;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalScreenSpace | ShaderFeatures.DecalNormalBlendLow;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.screenSpaceSettings.normalBlend = DecalNormalBlend.Low;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = expectedWithUnused;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.screenSpaceSettings.normalBlend = DecalNormalBlend.Low;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalGBuffer | ShaderFeatures.DecalNormalBlendLow;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // ScreenSpace - DecalNormalBlend Medium
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.screenSpaceSettings.normalBlend = DecalNormalBlend.Medium;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalScreenSpace | ShaderFeatures.DecalNormalBlendMedium;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.screenSpaceSettings.normalBlend = DecalNormalBlend.Medium;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = expectedWithUnused;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.screenSpaceSettings.normalBlend = DecalNormalBlend.Medium;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalGBuffer | ShaderFeatures.DecalNormalBlendMedium;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            // ScreenSpace - DecalNormalBlend High
            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.screenSpaceSettings.normalBlend = DecalNormalBlend.High;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalScreenSpace | ShaderFeatures.DecalNormalBlendHigh;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.screenSpaceSettings.normalBlend = DecalNormalBlend.High;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.needsUnusedVariants = true;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = expectedWithUnused;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            ((DecalRendererFeature)helper.rendererFeatures[0]).settings.screenSpaceSettings.normalBlend = DecalNormalBlend.High;
            rendererRequirements = helper.defaultRendererRequirements;
            rendererRequirements.renderingMode = RenderingMode.Deferred;
            rendererRequirements.needsUnusedVariants = false;
            actual = helper.GetSupportedShaderFeaturesFromRendererFeatures(rendererRequirements);
            expected = ShaderFeatures.DecalGBuffer | ShaderFeatures.DecalNormalBlendHigh;
            helper.AssertShaderFeaturesAndReset(expected, actual);

            helper.Cleanup();
        }
    }
}
