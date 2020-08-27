using NUnit.Framework;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.Tests
{
    [TestFixture]
    class MultipleObjectLight2DTests
    {
        GameObject m_TestObject1;
        GameObject m_TestObject2;
        GameObject m_TestObject3;
        GameObject m_TestObject4;

        [SetUp]
        public void Setup()
        {
            m_TestObject1 = new GameObject("Test Object 1");
            m_TestObject2 = new GameObject("Test Object 2");
            m_TestObject3 = new GameObject("Test Object 3");
            m_TestObject4 = new GameObject("Test Object 4");
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(m_TestObject4);
            Object.DestroyImmediate(m_TestObject3);
            Object.DestroyImmediate(m_TestObject2);
            Object.DestroyImmediate(m_TestObject1);
        }

        [Test]
        public void LightsAreSortedByLightOrder()
        {
            var light1 = m_TestObject1.AddComponent<Light2D>();
            var light2 = m_TestObject2.AddComponent<Light2D>();
            var light3 = m_TestObject3.AddComponent<Light2D>();
            
            light1.lightOrder = 1;
            light2.lightOrder = 2;
            light3.lightOrder = 0;

            var camera = m_TestObject4.AddComponent<Camera>();
            var cameraPos = camera.transform.position;
            light1.transform.position = cameraPos;
            light2.transform.position = cameraPos;
            light3.transform.position = cameraPos;

            light1.UpdateMesh();
            light1.UpdateBoundingSphere();
            light2.UpdateMesh();
            light2.UpdateBoundingSphere();
            light3.UpdateMesh();
            light3.UpdateBoundingSphere();

            var cullResult = new Light2DCullResult();
            var cullingParams = new ScriptableCullingParameters();
            camera.TryGetCullingParameters(out cullingParams);
            cullResult.SetupCulling(ref cullingParams, camera);

            Assert.AreSame(light3, cullResult.visibleLights[0]);
            Assert.AreSame(light1, cullResult.visibleLights[1]);
            Assert.AreSame(light2, cullResult.visibleLights[2]);
        }

        [Test]
        public void LightIsInVisibleListIfInCameraView()
        {
            var camera = m_TestObject1.AddComponent<Camera>();
            var light = m_TestObject2.AddComponent<Light2D>();
            light.transform.position = camera.transform.position;
            light.UpdateMesh();
            light.UpdateBoundingSphere();

            var cullResult = new Light2DCullResult();
            var cullingParams = new ScriptableCullingParameters();
            camera.TryGetCullingParameters(out cullingParams);
            cullResult.SetupCulling(ref cullingParams, camera);

            Assert.Contains(light, cullResult.visibleLights);
        }

        [Test]
        public void LightIsNotInVisibleListIfNotInCameraView()
        {
            var camera = m_TestObject1.AddComponent<Camera>();
            var light = m_TestObject2.AddComponent<Light2D>();
            light.transform.position = camera.transform.position + new Vector3(9999.0f, 0.0f, 0.0f);
            light.UpdateMesh();
            light.UpdateBoundingSphere();

            var cullResult = new Light2DCullResult();
            var cullingParams = new ScriptableCullingParameters();
            camera.TryGetCullingParameters(out cullingParams);
            cullResult.SetupCulling(ref cullingParams, camera);

            Assert.IsFalse(cullResult.visibleLights.Contains(light));
        }
    }
}
