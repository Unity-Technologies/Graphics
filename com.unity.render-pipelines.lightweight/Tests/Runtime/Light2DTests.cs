using NUnit.Framework;
using System.Collections;
using UnityEngine.Experimental.Rendering.LWRP;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.LWRP.Tests
{
    [TestFixture]
    class SingleObjectLight2DTests
    {
        Light2DManager m_LightManager;
        GameObject m_TestObject;
        Light2D m_TestLight;

        [SetUp]
        public void Setup()
        {
            m_LightManager = new Light2DManager();
            m_TestObject = new GameObject("Test Object");
            m_TestLight = m_TestObject.AddComponent<Light2D>();
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(m_TestObject);
            m_LightManager.Dispose();
        }

        [Test]
        public void SetupCullingSetsBoundingSpheresAndCullingIndices()
        {
            Light2D.SetupCulling(default(ScriptableRenderContext), Camera.main);

            Assert.NotNull(Light2DManager.boundingSpheres);
            Assert.AreEqual(1024, Light2DManager.boundingSpheres.Length);
            Assert.AreEqual(0, m_TestLight.lightCullingIndex);
        }

        [UnityTest]
        public IEnumerator ChangingBlendStyleMovesTheLightToTheCorrectListInLightManager()
        {
            m_TestLight.blendStyleIndex = 1;

            // LightManager update happens in LateUpdate(). So we must test the result in the next frame.
            yield return null;

            Assert.AreEqual(0, Light2DManager.lights[0].Count);
            Assert.AreSame(m_TestLight, Light2DManager.lights[1][0]);
        }
    }

    [TestFixture]
    class MultipleObjectLight2DTests
    {
        Light2DManager m_LightManager;
        GameObject m_TestObject1;
        GameObject m_TestObject2;
        GameObject m_TestObject3;

        [SetUp]
        public void Setup()
        {
            m_LightManager = new Light2DManager();
            m_TestObject1 = new GameObject("Test Object 1");
            m_TestObject2 = new GameObject("Test Object 2");
            m_TestObject3 = new GameObject("Test Object 3");
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(m_TestObject3);
            Object.DestroyImmediate(m_TestObject2);
            Object.DestroyImmediate(m_TestObject1);
            m_LightManager.Dispose();
        }

        [UnityTest]
        public IEnumerator LightsAreSortedByLightOrder()
        {
            var light1 = m_TestObject1.AddComponent<Light2D>();
            var light2 = m_TestObject2.AddComponent<Light2D>();
            var light3 = m_TestObject3.AddComponent<Light2D>();

            light1.lightOrder = 1;
            light2.lightOrder = 2;
            light3.lightOrder = 0;

            // Sorting happens in LateUpdate() after light order has changed.
            // So we must test the result in the next frame.
            yield return null;

            Assert.AreSame(light3, Light2DManager.lights[0][0]);
            Assert.AreSame(light1, Light2DManager.lights[0][1]);
            Assert.AreSame(light2, Light2DManager.lights[0][2]);
        }

        [Test]
        public void IsLightVisibleReturnsTrueIfInCameraView()
        {
            var camera = m_TestObject1.AddComponent<Camera>();
            var light = m_TestObject2.AddComponent<Light2D>();
            light.transform.position = camera.transform.position;
            Light2D.SetupCulling(default(ScriptableRenderContext), camera);

            // We can only verify the results after culling is done on this camera.
            camera.Render();

            Assert.IsTrue(light.IsLightVisible(camera));
        }

        [Test]
        public void IsLightVisibleReturnsFalseIfNotInCameraView()
        {
            var camera = m_TestObject1.AddComponent<Camera>();
            var light = m_TestObject2.AddComponent<Light2D>();
            light.transform.position = camera.transform.position + new Vector3(9999.0f, 0.0f, 0.0f);
            Light2D.SetupCulling(default(ScriptableRenderContext), camera);

            // We can only verify the results after culling is done on this camera.
            camera.Render();

            Assert.IsFalse(light.IsLightVisible(camera));
        }

        [Test]
        public void DestroyingLastLightAlsoDestroysCullingGroup()
        {
            var light1 = m_TestObject1.AddComponent<Light2D>();
            var light2 = m_TestObject2.AddComponent<Light2D>();

            Assert.IsNotNull(Light2DManager.cullingGroup);

            Object.Destroy(light2);

            Assert.IsNotNull(Light2DManager.cullingGroup);

            Object.Destroy(light1);

            Assert.IsNull(Light2DManager.cullingGroup);
        }
    }
}
