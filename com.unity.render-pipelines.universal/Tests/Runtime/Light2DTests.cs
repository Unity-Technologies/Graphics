using System.Collections;
using NUnit.Framework;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Universal.Tests
{
    [TestFixture]
    class MultipleObjectLight2DTests
    {
        GameObject m_TestObject1;
        GameObject m_TestObject2;
        GameObject m_TestObject3;
        GameObject m_TestObject4;
        GameObject m_TestObjectCached;
        GameObject m_TestObjectBatched1;
        GameObject m_TestObjectBatched2;
        GameObject m_Camera;

        Renderer2DData m_Data;
        RenderPipelineAsset currentAsset;
            
        [SetUp]
        public void Setup()
        {
            currentAsset = GraphicsSettings.renderPipelineAsset;
            
            m_Data = ScriptableObject.CreateInstance<Renderer2DData>();
            GraphicsSettings.renderPipelineAsset = UniversalRenderPipelineAsset.Create(m_Data);

            m_TestObject1 = new GameObject("Test Object 1");
            m_TestObject2 = new GameObject("Test Object 2");
            m_TestObject3 = new GameObject("Test Object 3");
            m_TestObject4 = new GameObject("Test Object 4");
            m_TestObjectCached = new GameObject("Test Object Cached");
            m_TestObjectBatched1 = new GameObject("Test Object Batched 1");
            m_TestObjectBatched2 = new GameObject("Test Object Batched 2");

            m_Camera = new GameObject("Main Camera");
            m_Camera.AddComponent<Camera>();
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(m_Camera);
            Object.DestroyImmediate(m_TestObjectBatched2);
            Object.DestroyImmediate(m_TestObjectBatched1);
            Object.DestroyImmediate(m_TestObjectCached);
            Object.DestroyImmediate(m_TestObject4);
            Object.DestroyImmediate(m_TestObject3);
            Object.DestroyImmediate(m_TestObject2);
            Object.DestroyImmediate(m_TestObject1);
            
            GraphicsSettings.renderPipelineAsset = currentAsset;
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

            light1.UpdateMesh(true);
            light1.UpdateBoundingSphere();
            light2.UpdateMesh(true);
            light2.UpdateBoundingSphere();
            light3.UpdateMesh(true);
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
            light.UpdateMesh(true);
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
            light.UpdateMesh(true);
            light.UpdateBoundingSphere();

            var cullResult = new Light2DCullResult();
            var cullingParams = new ScriptableCullingParameters();
            camera.TryGetCullingParameters(out cullingParams);
            cullResult.SetupCulling(ref cullingParams, camera);

            Assert.IsFalse(cullResult.visibleLights.Contains(light));
        }

        [Test]
        public void CachedMeshDataIsUpdatedOnChange()
        {
            var shapePath = new Vector3[4] { new Vector3( 0, 0, 0), new Vector3(1,0,0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
            var light = m_TestObjectCached.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Freeform;

            light.SetShapePath(shapePath);
            light.UpdateMesh(true);

            Assert.AreEqual(true, light.hasCachedMesh);
        }

        [Test]
        public void CachedMeshDataIsOverriddenByRuntimeChanges()
        {
            var shapePath = new Vector3[4] { new Vector3( 0, 0, 0), new Vector3(1,0,0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
            var light = m_TestObjectCached.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Freeform;
            light.SetShapePath(shapePath);
            light.UpdateMesh(true);

            int vertexCount = 0, triangleCount = 0;

            // Check if Cached Data and the actual data are the same.
            Assert.AreEqual(true, light.hasCachedMesh);
            vertexCount = light.lightMesh.triangles.Length;
            triangleCount = light.lightMesh.vertices.Length;

            // Simulate Runtime Behavior.
            var shapePathChanged = new Vector3[5] { new Vector3( 0, 0, 0), new Vector3(1,0,0), new Vector3(1, 1, 0), new Vector3(0.5f, 1.5f, 0), new Vector3(0, 1, 0) };
            light.SetShapePath(shapePathChanged);
            light.UpdateMesh(true);

            // Check if Cached Data and the actual data are no longer the same. (We don't save cache on Runtime)
            Assert.AreNotEqual(vertexCount, light.lightMesh.triangles.Length);
            Assert.AreNotEqual(triangleCount, light.lightMesh.vertices.Length);
        }
        
        [UnityTest]
        public IEnumerator OnDisableRendererDataBatching_DisablesBatching()
        {
            var asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            var rendererData = asset.scriptableRendererData as Renderer2DData;
            
            var shapePath = new Vector3[4] { new Vector3( 0, 0, 0), new Vector3(1,0,0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
            var light1 = m_TestObjectBatched1.AddComponent<Light2D>();
            var light2 = m_TestObjectBatched2.AddComponent<Light2D>();
            
            rendererData.enableBatching = false;
            light1.lightType = Light2D.LightType.Freeform;
            light1.SetShapePath(shapePath);
            light1.UpdateMesh(true);
            light2.lightType = Light2D.LightType.Freeform;
            light2.SetShapePath(shapePath);
            light2.UpdateMesh(true);
            
            m_Camera.GetComponent<Camera>().Render();
            yield return null;            

            Assert.AreEqual(0, Light2DBatch.s_Batches);
        }
        
        [UnityTest]
        public IEnumerator OnEnableRendererDataBatching_EnablesBatching_AdditiveStyle()
        {
            var asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            var rendererData = asset.scriptableRendererData as Renderer2DData;
            
            var shapePath = new Vector3[4] { new Vector3( 0, 0, 0), new Vector3(1,0,0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
            var light1 = m_TestObjectBatched1.AddComponent<Light2D>();
            var light2 = m_TestObjectBatched2.AddComponent<Light2D>();
            
            rendererData.enableBatching = true;
            light1.lightType = Light2D.LightType.Freeform;
            light1.SetShapePath(shapePath);
            light1.UpdateMesh(true);
            light2.lightType = Light2D.LightType.Freeform;
            light2.SetShapePath(shapePath);
            light2.UpdateMesh(true);
            
            Light2DBlendStyle style = m_Data.lightBlendStyles[0];
            style.blendMode = Light2DBlendStyle.BlendMode.Additive;
            m_Data.SetLightBlendStyle(style, 0);            

            m_Camera.GetComponent<Camera>().Render();
            yield return null;            
            
            Assert.AreEqual(1, Light2DBatch.s_Batches);
        }     
        
        [UnityTest]
        public IEnumerator OnEnableRendererDataBatching_DisablesBatching_MultiplicativeStyle()
        {
            var asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            var rendererData = asset.scriptableRendererData as Renderer2DData;
            
            var shapePath = new Vector3[4] { new Vector3( 0, 0, 0), new Vector3(1,0,0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
            var light1 = m_TestObjectBatched1.AddComponent<Light2D>();
            var light2 = m_TestObjectBatched2.AddComponent<Light2D>();
            
            rendererData.enableBatching = true;
            light1.lightType = Light2D.LightType.Freeform;
            light1.SetShapePath(shapePath);
            light1.UpdateMesh(true);
            light2.lightType = Light2D.LightType.Freeform;
            light2.SetShapePath(shapePath);
            light2.UpdateMesh(true);
            
            Light2DBlendStyle style = m_Data.lightBlendStyles[0];
            style.blendMode = Light2DBlendStyle.BlendMode.Multiply;
            m_Data.SetLightBlendStyle(style, 0);            

            m_Camera.GetComponent<Camera>().Render();
            yield return null;            
            
            Assert.AreEqual(0, Light2DBatch.s_Batches);
        }             
     
        [UnityTest]
        public IEnumerator BatchingCachesMesh_IfThereAreNoChanges()
        {
            var asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            var rendererData = asset.scriptableRendererData as Renderer2DData;
            
            var shapePath = new Vector3[4] { new Vector3( 0, 0, 0), new Vector3(1,0,0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
            var light1 = m_TestObjectBatched1.AddComponent<Light2D>();
            var light2 = m_TestObjectBatched2.AddComponent<Light2D>();
            int meshCount = 0;
            
            rendererData.enableBatching = true;
            light1.lightType = Light2D.LightType.Freeform;
            light1.SetShapePath(shapePath);
            light1.UpdateMesh(true);
            light2.lightType = Light2D.LightType.Freeform;
            light2.SetShapePath(shapePath);
            light2.UpdateMesh(true);
            
            Light2DBlendStyle style = m_Data.lightBlendStyles[0];
            style.blendMode = Light2DBlendStyle.BlendMode.Additive;
            m_Data.SetLightBlendStyle(style, 0);

            
            m_Camera.GetComponent<Camera>().Render();
            yield return null;

            meshCount = Light2DBatch.s_CombineOperations;
            
            m_Camera.GetComponent<Camera>().Render();
            yield return null;
            
            Assert.AreEqual(meshCount, Light2DBatch.s_CombineOperations);
        }           
        
        [UnityTest]
        public IEnumerator BatchingRegeneratesMesh_IfThereAreChangesInTransform()
        {
            var asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            var rendererData = asset.scriptableRendererData as Renderer2DData;
            
            var shapePath = new Vector3[4] { new Vector3( 0, 0, 0), new Vector3(1,0,0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
            var light1 = m_TestObjectBatched1.AddComponent<Light2D>();
            var light2 = m_TestObjectBatched2.AddComponent<Light2D>();
            int meshCount = 0;
            
            rendererData.enableBatching = true;
            light1.lightType = Light2D.LightType.Freeform;
            light1.SetShapePath(shapePath);
            light1.UpdateMesh(true);
            light2.lightType = Light2D.LightType.Freeform;
            light2.SetShapePath(shapePath);
            light2.UpdateMesh(true);
            
            Light2DBlendStyle style = m_Data.lightBlendStyles[0];
            style.blendMode = Light2DBlendStyle.BlendMode.Additive;
            m_Data.SetLightBlendStyle(style, 0);

            m_Camera.GetComponent<Camera>().Render();
            yield return null;

            light2.transform.position = new Vector3(0, 0, 100.0f);
            meshCount = Light2DBatch.s_CombineOperations;
            
            m_Camera.GetComponent<Camera>().Render();
            yield return null;
            
            Assert.AreNotEqual(meshCount, Light2DBatch.s_CombineOperations);
            meshCount = Light2DBatch.s_CombineOperations;
            
            m_Camera.GetComponent<Camera>().Render();
            yield return null;
            
            Assert.AreEqual(meshCount, Light2DBatch.s_CombineOperations);            
        }              
        
        [UnityTest]
        public IEnumerator BatchingRegeneratesMesh_IfThereAreChangesOnPath()
        {
            var asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            var rendererData = asset.scriptableRendererData as Renderer2DData;
            
            var shapePath = new Vector3[4] { new Vector3( 0, 0, 0), new Vector3(1,0,0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
            var light1 = m_TestObjectBatched1.AddComponent<Light2D>();
            var light2 = m_TestObjectBatched2.AddComponent<Light2D>();
            int meshCount = 0;
            
            rendererData.enableBatching = true;
            light1.lightType = Light2D.LightType.Freeform;
            light1.SetShapePath(shapePath);
            light1.UpdateMesh(true);
            light2.lightType = Light2D.LightType.Freeform;
            light2.SetShapePath(shapePath);
            light2.UpdateMesh(true);
            
            Light2DBlendStyle style = m_Data.lightBlendStyles[0];
            style.blendMode = Light2DBlendStyle.BlendMode.Additive;
            m_Data.SetLightBlendStyle(style, 0);

            m_Camera.GetComponent<Camera>().Render();
            yield return null;

            var shapePath2 = new Vector3[4] { new Vector3( 0, 0, 0), new Vector3(1,0,0), new Vector3(1, 1, 0), new Vector3(0, 2, 0) };
            light2.SetShapePath(shapePath2);
            light2.UpdateMesh(true);
            meshCount = Light2DBatch.s_CombineOperations;
            
            m_Camera.GetComponent<Camera>().Render();
            yield return null;
            
            Assert.AreNotEqual(meshCount, Light2DBatch.s_CombineOperations);
            meshCount = Light2DBatch.s_CombineOperations;
            
            m_Camera.GetComponent<Camera>().Render();
            yield return null;
            
            Assert.AreEqual(meshCount, Light2DBatch.s_CombineOperations);            
        }           
        
        [UnityTest]
        public IEnumerator BatchingRegeneratesMesh_IfThereAreChangesInParameters()
        {
            var asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            var rendererData = asset.scriptableRendererData as Renderer2DData;
            
            var shapePath = new Vector3[4] { new Vector3( 0, 0, 0), new Vector3(1,0,0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
            var light1 = m_TestObjectBatched1.AddComponent<Light2D>();
            var light2 = m_TestObjectBatched2.AddComponent<Light2D>();
            int meshCount = 0;
            
            rendererData.enableBatching = true;
            light1.lightType = Light2D.LightType.Freeform;
            light1.SetShapePath(shapePath);
            light1.UpdateMesh(true);
            light2.lightType = Light2D.LightType.Freeform;
            light2.SetShapePath(shapePath);
            light2.UpdateMesh(true);
            
            Light2DBlendStyle style = m_Data.lightBlendStyles[0];
            style.blendMode = Light2DBlendStyle.BlendMode.Additive;
            m_Data.SetLightBlendStyle(style, 0);

            m_Camera.GetComponent<Camera>().Render();
            yield return null;

            light2.color = Color.grey;
            meshCount = Light2DBatch.s_CombineOperations;
            
            m_Camera.GetComponent<Camera>().Render();
            yield return null;
            
            Assert.AreNotEqual(meshCount, Light2DBatch.s_CombineOperations);
            meshCount = Light2DBatch.s_CombineOperations;
            
            m_Camera.GetComponent<Camera>().Render();
            yield return null;
            
            Assert.AreEqual(meshCount, Light2DBatch.s_CombineOperations);            
        }                   
        
    }

}
