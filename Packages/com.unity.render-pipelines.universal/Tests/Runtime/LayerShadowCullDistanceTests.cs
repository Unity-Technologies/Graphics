using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Universal.Tests
{
    class LayerShadowCullDistanceTests
    {
        class ShadowCasterBoundsFeature : ScriptableRendererFeature
        {
            internal readonly ShadowCasterBoundsPass boundsPass = new ShadowCasterBoundsPass();

            public override void Create()
            {
            }

            public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
            {
                renderer.EnqueuePass(boundsPass);
            }
        }

        class ShadowCasterBoundsPass : ScriptableRenderPass
        {
            internal bool hasCasters;
            internal Bounds casterBounds;

            public ShadowCasterBoundsPass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var renderingData = frameData.Get<UniversalRenderingData>();
                int mainLightIndex = frameData.Get<UniversalLightData>().mainLightIndex;
                hasCasters = mainLightIndex >= 0 &&
                    renderingData.cullResults.GetShadowCasterBounds(mainLightIndex, out casterBounds);
            }
        }

        const int k_LayerCount = 32;
        const int k_NearLayer = 29;
        const int k_FarLayer = 30;
        const float k_ShadowDistance = 50f;
        const float k_FarLayerShadowCullDistance = 100f;
        const float k_SunElevation = 8f;
        const float k_FarCasterDistanceBehindCamera = 150f;
        static readonly Vector3 k_NearCasterPosition = new Vector3(0f, 0f, 10f);

        RenderPipelineAsset m_PreviousRenderPipelineAsset;
        Light m_PreviousSun;
        ShadowCasterBoundsFeature m_Feature;
        readonly List<Object> m_Objects = new();
        Camera m_Camera;
        RenderTexture m_Destination;

        [UnityOneTimeSetUp]
        public IEnumerator OneTimeSetUp()
        {
            m_PreviousRenderPipelineAsset = QualitySettings.renderPipeline;
            m_PreviousSun = RenderSettings.sun;

            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                Assert.Ignore("These tests only run when URP is the active render pipeline.");

            m_Feature = ScriptableObject.CreateInstance<ShadowCasterBoundsFeature>();
            m_Objects.Add(m_Feature);

            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            rendererData.rendererFeatures.Add(m_Feature);
            m_Objects.Add(rendererData);

            var asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            asset.m_RendererDataList = new ScriptableRendererData[] { rendererData };
            var volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            var shadowsVolume = volumeProfile.Add<ShadowsVolumeOverride>(overrides: true);
            shadowsVolume.maxShadowDistance.value = k_ShadowDistance;
            m_Objects.Add(shadowsVolume);
            m_Objects.Add(volumeProfile);
            asset.volumeProfile = volumeProfile;
            m_Objects.Add(asset);
            QualitySettings.renderPipeline = asset;

            var cameraGameObject = new GameObject("Layer Shadow Cull Camera");
            m_Objects.Add(cameraGameObject);
            m_Camera = cameraGameObject.AddComponent<Camera>();
            m_Camera.enabled = false;
            m_Camera.cullingMask = (1 << k_NearLayer) | (1 << k_FarLayer);
            cameraGameObject.AddComponent<UniversalAdditionalCameraData>();

            var lightGameObject = new GameObject("Low Sun Behind Camera");
            m_Objects.Add(lightGameObject);
            lightGameObject.transform.rotation = Quaternion.Euler(k_SunElevation, 0f, 0f);
            var sun = lightGameObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Hard;
            var shadowCullDistances = new float[k_LayerCount];
            shadowCullDistances[k_FarLayer] = k_FarLayerShadowCullDistance;
            sun.layerShadowCullDistances = shadowCullDistances;
            RenderSettings.sun = sun;

            CreateCaster(k_NearCasterPosition, k_NearLayer);
            CreateCaster(k_NearCasterPosition - lightGameObject.transform.forward * k_FarCasterDistanceBehindCamera, k_FarLayer);

            m_Destination = new RenderTexture(64, 64, 24);
            m_Objects.Add(m_Destination);

            yield return null;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            QualitySettings.renderPipeline = m_PreviousRenderPipelineAsset;
            RenderSettings.sun = m_PreviousSun;

            foreach (var obj in m_Objects)
            {
                Object.DestroyImmediate(obj);
            }
            m_Objects.Clear();
        }

        void CreateCaster(Vector3 position, int layer)
        {
            var caster = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_Objects.Add(caster);
            caster.layer = layer;
            caster.transform.position = position;
        }

        float RenderAndCaptureShadowCasterBoundsMinZ()
        {
            var boundsPass = m_Feature.boundsPass;
            boundsPass.hasCasters = false;
            RenderPipeline.SubmitRenderRequest(m_Camera, new RenderPipeline.StandardRequest { destination = m_Destination });

            Assert.IsTrue(boundsPass.hasCasters, "The main light has no shadow casters");
            return boundsPass.casterBounds.min.z;
        }

        [Test]
        public void LayerShadowCullDistance_WithPlanarLayerCulling_KeepsCastersBehindCamera()
        {
            m_Camera.layerCullSpherical = false;

            float casterBoundsMinZ = RenderAndCaptureShadowCasterBoundsMinZ();

            Assert.IsTrue(casterBoundsMinZ < -k_FarLayerShadowCullDistance, $"Shadow caster bounds start at z = {casterBoundsMinZ}");
        }
    }
}
