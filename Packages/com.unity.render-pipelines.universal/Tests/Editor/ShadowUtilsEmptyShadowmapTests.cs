using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    sealed class ShadowUtilsEmptyShadowmapKeywordTests
    {
        [Test]
        public void GivenShadowsOffVariantsStripped_WhenRealtimeShadowsAreEnabled_ThenKeywordIsEnabled()
        {
            bool enableKeyword = ShadowUtils.ShouldEnableKeywordForEmptyShadowmap(stripShadowsOffVariants: true, shadowsEnabled: true);
            Assert.That(enableKeyword, Is.True, "With shadows-off variants stripped and realtime shadows enabled, the shadows-on variant is the only one available, so the keyword must be enabled.");
        }

        [Test]
        public void GivenShadowsOffVariantsStripped_WhenRealtimeShadowsAreDisabled_ThenKeywordIsNotEnabled()
        {
            bool enableKeyword = ShadowUtils.ShouldEnableKeywordForEmptyShadowmap(stripShadowsOffVariants: true, shadowsEnabled: false);
            Assert.That(enableKeyword, Is.False, "Realtime shadows are disabled, so the realtime shadow keyword must not be enabled even when the off variants are stripped.");
        }

        [Test]
        public void GivenShadowsOffVariantsKept_WhenRealtimeShadowsAreEnabled_ThenKeywordIsNotEnabled()
        {
            bool enableKeyword = ShadowUtils.ShouldEnableKeywordForEmptyShadowmap(stripShadowsOffVariants: false, shadowsEnabled: true);
            Assert.That(enableKeyword, Is.False, "The shadows-off variant is available, so the empty shadowmap path must select it rather than enabling the shadow keyword.");
        }

        [Test]
        public void GivenShadowsOffVariantsKept_WhenRealtimeShadowsAreDisabled_ThenKeywordIsNotEnabled()
        {
            bool enableKeyword = ShadowUtils.ShouldEnableKeywordForEmptyShadowmap(stripShadowsOffVariants: false, shadowsEnabled: false);
            Assert.That(enableKeyword, Is.False, "Neither variant stripping nor realtime shadows apply, so the shadow keyword must stay disabled.");
        }
    }

    [TestFixture]
    sealed class ShadowUtilsEmptyShadowmapParamsTests
    {
        internal class EmptyShadowmapParamsCase
        {
            internal string name;
            internal bool stripShadowsOffVariants;
            internal bool shadowsEnabled;
            internal bool lightCastsBakedShadows;
            internal bool expectParamsComputed;

            public override string ToString()
            {
                return name;
            }
        }

        static IEnumerable EmptyShadowmapParamsCases()
        {
            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenStrippedOffVariantsAndRealtimeShadowsDisabled_WhenALightCastsBakedShadows_ThenParamsAreComputed",
                stripShadowsOffVariants = true,
                shadowsEnabled = false,
                lightCastsBakedShadows = true,
                expectParamsComputed = true
            };

            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenStrippedOffVariantsAndRealtimeShadowsDisabled_WhenNoLightCastsBakedShadows_ThenParamsAreNotComputed",
                stripShadowsOffVariants = true,
                shadowsEnabled = false,
                lightCastsBakedShadows = false,
                expectParamsComputed = false
            };

            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenStrippedOffVariantsAndRealtimeShadowsEnabled_WhenALightCastsBakedShadows_ThenParamsAreComputed",
                stripShadowsOffVariants = true,
                shadowsEnabled = true,
                lightCastsBakedShadows = true,
                expectParamsComputed = true
            };

            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenStrippedOffVariantsAndRealtimeShadowsEnabled_WhenNoLightCastsBakedShadows_ThenParamsAreComputed",
                stripShadowsOffVariants = true,
                shadowsEnabled = true,
                lightCastsBakedShadows = false,
                expectParamsComputed = true
            };

            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenKeptOffVariantsAndRealtimeShadowsEnabled_WhenALightCastsBakedShadows_ThenParamsAreNotComputed",
                stripShadowsOffVariants = false,
                shadowsEnabled = true,
                lightCastsBakedShadows = true,
                expectParamsComputed = false
            };

            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenKeptOffVariantsAndRealtimeShadowsEnabled_WhenNoLightCastsBakedShadows_ThenParamsAreNotComputed",
                stripShadowsOffVariants = false,
                shadowsEnabled = true,
                lightCastsBakedShadows = false,
                expectParamsComputed = false
            };

            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenKeptOffVariantsAndRealtimeShadowsDisabled_WhenALightCastsBakedShadows_ThenParamsAreNotComputed",
                stripShadowsOffVariants = false,
                shadowsEnabled = false,
                lightCastsBakedShadows = true,
                expectParamsComputed = false
            };

            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenKeptOffVariantsAndRealtimeShadowsDisabled_WhenNoLightCastsBakedShadows_ThenParamsAreNotComputed",
                stripShadowsOffVariants = false,
                shadowsEnabled = false,
                lightCastsBakedShadows = false,
                expectParamsComputed = false
            };
        }

        [Test]
        [TestCaseSource(nameof(EmptyShadowmapParamsCases))]
        public void DecideEmptyShadowmapParams(EmptyShadowmapParamsCase testCase)
        {
            bool computeParams = ShadowUtils.ShouldComputeEmptyShadowmapParams(testCase.stripShadowsOffVariants, testCase.shadowsEnabled, testCase.lightCastsBakedShadows);
            Assert.That(computeParams, Is.EqualTo(testCase.expectParamsComputed),
                $"ShouldComputeEmptyShadowmapParams(strip: {testCase.stripShadowsOffVariants}, shadowsEnabled: {testCase.shadowsEnabled}, bakedShadows: {testCase.lightCastsBakedShadows}) returned the wrong decision.");
        }
    }

    [TestFixture]
    sealed class MainLightEmptyShadowmapFadeTests
    {
        GameObject m_LightObject;

        [SetUp]
        public void SetUp()
        {
            m_LightObject = new GameObject("EmptyShadowmapFadeLight", typeof(Light));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_LightObject);
        }

        static float ReceiverShadowFade(Vector4 shadowParams, float distanceToCamera)
        {
            return Mathf.Clamp01(distanceToCamera * distanceToCamera * shadowParams.z + shadowParams.w);
        }

        [TestCase(0.2f)]
        [TestCase(0f)]
        public void GivenNoShadowsRendered_WhenComputingEmptyShadowmapParams_ThenFadeCompletesAtMaxShadowDistance(float cascadeBorder)
        {
            const float k_MaxShadowDistance = 50f;
            var cameraData = new UniversalCameraData { maxShadowDistance = k_MaxShadowDistance };
            var shadowData = new UniversalShadowData { mainLightShadowCascadeBorder = cascadeBorder };

            Vector4 shadowParams = MainLightShadowCasterPass.ComputeShadowParamsForEmptyRendering(m_LightObject.GetComponent<Light>(), cameraData, shadowData);

            Assert.That(ReceiverShadowFade(shadowParams, 0.5f * k_MaxShadowDistance), Is.EqualTo(0f),
                "Half way to the max shadow distance, the realtime shadow must not have started fading.");
            Assert.That(ReceiverShadowFade(shadowParams, 0.99f * k_MaxShadowDistance), Is.LessThan(1f),
                "Just inside the max shadow distance, the realtime shadow must not be fully faded.");
            Assert.That(ReceiverShadowFade(shadowParams, 1.01f * k_MaxShadowDistance), Is.EqualTo(1f),
                "Just past the max shadow distance, the realtime shadow must be fully faded.");
        }
    }

    [TestFixture]
    sealed class TransparentsNotReceivingShadowsParamsTests
    {
        const float k_ShadowDistance = 50f;
        const float k_LightShadowStrength = 0.6f;
        static readonly int k_MainLightShadowParamsId = Shader.PropertyToID("_MainLightShadowParams");

        RenderPipelineAsset m_PreviousQualityPipeline;
        bool m_PipelineOverridden;
        UniversalRenderPipelineAsset m_PipelineAsset;
        UniversalRendererData m_RendererData;
        Material m_TransparentMaterial;
        RenderTexture m_ColorTarget;
        RenderTexture m_DepthOnlyTarget;
        GameObject m_LightObject;
        GameObject m_CasterObject;
        GameObject m_TransparentObject;
        Camera m_ColorCamera;
        Camera m_DepthOnlyCamera;

        [SetUp]
        public void SetUp()
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                Assert.Ignore("Requires URP to be the active render pipeline.");

            m_PreviousQualityPipeline = QualitySettings.renderPipeline;
            m_RendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            m_RendererData.shadowTransparentReceive = false;
            m_PipelineAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            m_PipelineAsset.m_RendererDataList[0] = m_RendererData;
            m_PipelineAsset.mainLightRenderingMode = LightRenderingMode.PerPixel;
            m_PipelineAsset.supportsMainLightShadows = true;
            m_PipelineAsset.shadowDistance = k_ShadowDistance;
            m_PipelineAsset.shadowCascadeCount = 1;
            QualitySettings.renderPipeline = m_PipelineAsset;
            m_PipelineOverridden = true;

            m_LightObject = new GameObject("TransparentShadowParamsLight") { hideFlags = HideFlags.HideAndDontSave };
            m_LightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = m_LightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = k_LightShadowStrength;

            m_CasterObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            m_CasterObject.hideFlags = HideFlags.HideAndDontSave;

            m_TransparentMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { renderQueue = (int)RenderQueue.Transparent };
            m_TransparentObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_TransparentObject.hideFlags = HideFlags.HideAndDontSave;
            m_TransparentObject.transform.position = new Vector3(0f, 1f, 0f);
            m_TransparentObject.GetComponent<MeshRenderer>().sharedMaterial = m_TransparentMaterial;

            m_ColorTarget = new RenderTexture(64, 64, 24);
            m_DepthOnlyTarget = new RenderTexture(64, 64, 24, RenderTextureFormat.Depth);
            m_ColorCamera = CreateCamera("TransparentShadowParamsColorCamera", m_ColorTarget);
            m_DepthOnlyCamera = CreateCamera("TransparentShadowParamsDepthOnlyCamera", m_DepthOnlyTarget);
        }

        static Camera CreateCamera(string name, RenderTexture target)
        {
            var cameraObject = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 10f, -10f), Quaternion.Euler(45f, 0f, 0f));
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.targetTexture = target;
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            return camera;
        }

        [TearDown]
        public void TearDown()
        {
            if (m_PipelineOverridden)
            {
                QualitySettings.renderPipeline = m_PreviousQualityPipeline;
                m_PipelineOverridden = false;
            }

            if (m_ColorCamera != null)
                Object.DestroyImmediate(m_ColorCamera.gameObject);
            if (m_DepthOnlyCamera != null)
                Object.DestroyImmediate(m_DepthOnlyCamera.gameObject);
            if (m_ColorTarget != null)
            {
                m_ColorTarget.Release();
                Object.DestroyImmediate(m_ColorTarget);
            }
            if (m_DepthOnlyTarget != null)
            {
                m_DepthOnlyTarget.Release();
                Object.DestroyImmediate(m_DepthOnlyTarget);
            }
            if (m_TransparentObject != null)
                Object.DestroyImmediate(m_TransparentObject);
            if (m_TransparentMaterial != null)
                Object.DestroyImmediate(m_TransparentMaterial);
            if (m_CasterObject != null)
                Object.DestroyImmediate(m_CasterObject);
            if (m_LightObject != null)
                Object.DestroyImmediate(m_LightObject);
            if (m_PipelineAsset != null)
                Object.DestroyImmediate(m_PipelineAsset);
            if (m_RendererData != null)
                Object.DestroyImmediate(m_RendererData);
        }

        Vector4 RenderAfterDepthOnlyCameraAndReadShadowParams(float depthOnlyCameraFarPlane)
        {
            m_DepthOnlyCamera.farClipPlane = depthOnlyCameraFarPlane;
            m_DepthOnlyCamera.Render();
            m_ColorCamera.Render();
            return Shader.GetGlobalVector(k_MainLightShadowParamsId);
        }

        [Test]
        public void GivenShadowsRendered_WhenTransparentsDoNotReceiveShadows_ThenMainLightShadowStrengthIsZeroWhateverRenderedBefore()
        {
            Vector4 afterNearDepthOnlyCamera = RenderAfterDepthOnlyCameraAndReadShadowParams(10f);
            Vector4 afterFarDepthOnlyCamera = RenderAfterDepthOnlyCameraAndReadShadowParams(1000f);

            Assert.That(afterNearDepthOnlyCamera, Is.EqualTo(afterFarDepthOnlyCamera),
                "The shadow params for transparents must not depend on a camera that rendered earlier.");
            Assert.That(afterFarDepthOnlyCamera.x, Is.EqualTo(0f),
                "Transparents that do not receive shadows must get zero main light shadow strength, so no main light shadow applies to them.");
        }
    }
}