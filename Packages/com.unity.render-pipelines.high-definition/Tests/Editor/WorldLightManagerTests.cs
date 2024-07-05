using NUnit.Framework;
using System;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class WorldLightManagerTests
    {
        WorldLights m_WorldLights = null;
        WorldLightsGpu m_WorldLightsGpu = null;
        WorldLightsSettings m_WorldLightsSettings = null;

        [SetUp]
        public void Setup()
        {
            m_WorldLights = new WorldLights();
            m_WorldLightsGpu = new WorldLightsGpu();
            m_WorldLightsSettings = new WorldLightsSettings();
            m_WorldLightsSettings.enabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            m_WorldLights.Release();
            m_WorldLightsGpu.Release();
        }

        private const float magic = 1.23456f;

        private GameObject CreateLight(string name, LightType type)
        {
            var lightGameObject = new GameObject(name);
            var light1 = lightGameObject.AddComponent<Light>();
            light1.type = type;
            light1.intensity = 1f;
            light1.range = magic;
            light1.spotAngle = 1f;

            lightGameObject.AddHDLight(type);

            return lightGameObject;
        }

        // Disable the tests since they don't work outside of the editor
        // [Test]
        public void BasicCreateLightTest()
        {
            GameObject cameraGameObject = new GameObject("Camera");
            Camera camera = cameraGameObject.AddComponent<Camera>();
            HDCamera HdCamera = new HDCamera(camera);

            if (!VolumeManager.instance.isInitialized)
                VolumeManager.instance.Initialize();

            var viewBounds = new Bounds(Vector3.zero, 10.0f * Vector3.one);
            var lightGameObject1 = CreateLight("Light1", LightType.Spot);
            var lightGameObject2 = CreateLight("Light2", LightType.Rectangle);

            Func<HDCamera, HDAdditionalLightData, Light, uint> flagsFunc = delegate(HDCamera hdCamera, HDAdditionalLightData data, Light light)
            {
                uint result = light.range == magic ? 0xffu : 0u;

                return result;
            };
            HDLightRenderDatabase.instance.Cleanup();

            WorldLightManager.CollectWorldLights(HdCamera, m_WorldLightsSettings, flagsFunc, viewBounds, m_WorldLights);

            Debug.Assert(m_WorldLights.normalLightCount == 2);

            Object.DestroyImmediate(lightGameObject1);

            WorldLightManager.CollectWorldLights(HdCamera, m_WorldLightsSettings, flagsFunc, viewBounds, m_WorldLights);

            Debug.Assert(m_WorldLights.normalLightCount == 1);

            lightGameObject2.transform.position = new Vector3(100, 100, 100);

            WorldLightManager.CollectWorldLights(HdCamera, m_WorldLightsSettings, flagsFunc, viewBounds, m_WorldLights);

            Debug.Assert(m_WorldLights.normalLightCount == 0);

            Object.DestroyImmediate(lightGameObject2);

            Object.DestroyImmediate(cameraGameObject);
        }

        // Disable the tests since they don't work outside of the editor
        // [Test]
        public void BasicCreateLightGpuTest()
        {
            GameObject cameraGameObject = new GameObject("Camera");
            Camera camera = cameraGameObject.AddComponent<Camera>();
            HDCamera hdCamera = new HDCamera(camera);

            if (!VolumeManager.instance.isInitialized)
                VolumeManager.instance.Initialize();
            
            var viewBounds = new Bounds(Vector3.zero, 10.0f * Vector3.one);
            var lightGameObject1 = CreateLight("Light1", LightType.Spot);
            var lightGameObject2 = CreateLight("Light2", LightType.Rectangle);

            Func<HDCamera, HDAdditionalLightData, Light, uint> flagsFunc = delegate(HDCamera hdCamera, HDAdditionalLightData data, Light light)
            {
                uint result = light.range == magic ? 0xffu : 0u;

                return result;
            };
            HDLightRenderDatabase.instance.Cleanup();

            WorldLightManager.CollectWorldLights(hdCamera, m_WorldLightsSettings, flagsFunc, viewBounds, m_WorldLights);

            var renderPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            var cmd = CommandBufferPool.Get();

            WorldLightManager.BuildWorldLightDatas(cmd, hdCamera, renderPipeline, m_WorldLights, m_WorldLightsGpu);

            void AssertLightData(int i)
            {
                ref LightData lightData = ref m_WorldLightsGpu.GetRef(i);
                Debug.Assert(lightData.range == magic);
            }

            AssertLightData(0);
            AssertLightData(1);

            Object.DestroyImmediate(lightGameObject1);
            Object.DestroyImmediate(lightGameObject2);

            Object.DestroyImmediate(cameraGameObject);
        }
    }
}
