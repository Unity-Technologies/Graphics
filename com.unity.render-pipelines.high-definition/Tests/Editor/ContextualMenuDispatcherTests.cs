using NUnit.Framework;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class ContextualMenuDispatcherTests
    {
        [Test]
        public void RemoveCamera()
        {
            GameObject cameraGO = new GameObject();
            var cameraComponent = cameraGO.AddComponent<Camera>();
            cameraGO.AddComponent<HDAdditionalCameraData>();

            ContextualMenuDispatcher.RemoveComponent(typeof(Camera), cameraComponent);

            Assert.True(cameraGO.GetComponent<Camera>() == null);
            Assert.True(cameraGO.GetComponent<HDAdditionalCameraData>() == null);

            GameObject.DestroyImmediate(cameraGO);
        }

        [Test]
        public void RemoveLight()
        {
            GameObject lightGO = new GameObject();
            var lightComponent = lightGO.AddComponent<Light>();
            lightGO.AddComponent<HDAdditionalLightData>();

            ContextualMenuDispatcher.RemoveComponent(typeof(Light), lightComponent);

            Assert.True(lightGO.GetComponent<Light>() == null);
            Assert.True(lightGO.GetComponent<HDAdditionalLightData>() == null);

            GameObject.DestroyImmediate(lightGO);
        }

        [Test]
        public void RemoveLightAndCamera()
        {
            GameObject GO = new GameObject();
            var lightComponent = GO.AddComponent<Light>();
            GO.AddComponent<HDAdditionalLightData>();

            var cameraComponent = GO.AddComponent<Camera>();
            GO.AddComponent<HDAdditionalCameraData>();

            ContextualMenuDispatcher.RemoveComponent(typeof(Camera), cameraComponent);

            Assert.True(GO.GetComponent<Camera>() == null);
            Assert.True(GO.GetComponent<HDAdditionalCameraData>() == null);

            Assert.True(GO.GetComponent<Light>() != null);
            Assert.True(GO.GetComponent<HDAdditionalLightData>() != null);

            ContextualMenuDispatcher.RemoveComponent(typeof(Light), lightComponent);

            Assert.True(GO.GetComponent<Light>() == null);
            Assert.True(GO.GetComponent<HDAdditionalLightData>() == null);

            GameObject.DestroyImmediate(GO);
        }
    }
}
