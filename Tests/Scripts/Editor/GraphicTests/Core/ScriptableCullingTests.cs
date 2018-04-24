using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

[TestFixture]
public class ScriptableCullingTests
{
    SceneSetup[]    m_CurrentLoadedScenes;
    Camera          m_TestCamera;

    void Setup(string testName, string cameraName)
    {
        string scenePath = string.Format("Assets/ScriptableRenderLoop/Tests/GraphicsTests/Core/Scenes/{0}.unity", testName);

        BackupSceneManagerSetup();
        EditorSceneManager.OpenScene(scenePath);

        string fullCameraName = string.Format("Camera_{0}", cameraName);

        var cameras = UnityEngine.Object.FindObjectsOfType(typeof(Camera)) as Camera[];
        m_TestCamera = Array.Find(cameras, (value) => value.name == fullCameraName);

        if (m_TestCamera == null)
        {
            // Throw?
            Assert.IsTrue(false, string.Format("Cannot find camera: {0}", cameraName) );
        }
    }

    void TearDown()
    {
        RestoreSceneManagerSetup();
    }

    void BackupSceneManagerSetup()
    {
        m_CurrentLoadedScenes = EditorSceneManager.GetSceneManagerSetup();
    }

    void RestoreSceneManagerSetup()
    {
        if ((m_CurrentLoadedScenes == null) || (m_CurrentLoadedScenes.Length == 0))
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }
        else
        {
            EditorSceneManager.RestoreSceneManagerSetup(m_CurrentLoadedScenes);
        }

        m_TestCamera = null;
    }

    [Test(Description = "Object simple frustum culling test")]
    public void ObjectFrustumCulling()
    {
        Setup("FrustumCullingTest", "FrustumCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        CullingRequests requests = new CullingRequests();
        requests.AddRequest(cullingParams);

        var results = Culling.ProcessRequests(requests);
        CullingResult cullResult1 = results.GetResult(0);

        Assert.AreEqual(3, cullResult1.GetVisibleObjectCount());

        TearDown();
    }

    [Test(Description = "Light simple frustum culling test")]
    public void LightFrustumCulling()
    {
        Setup("FrustumCullingTest", "FrustumCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        CullingRequests requests = new CullingRequests();
        requests.AddRequest(cullingParams);

        var lightCullResults = Culling.CullLights(requests);
        var lightCullResult = lightCullResults.GetResult(0);
        Assert.AreEqual(4, lightCullResult.visibleLights.Length);
        Assert.IsTrue(Array.Exists(lightCullResult.visibleLights, (visibleLight) => visibleLight.light.gameObject.name == "Directional Light"));
        Assert.IsTrue(Array.Exists(lightCullResult.visibleLights, (visibleLight) => visibleLight.light.gameObject.name == "Point Light Inside"));
        Assert.IsTrue(Array.Exists(lightCullResult.visibleLights, (visibleLight) => visibleLight.light.gameObject.name == "Point Light 2 Inside"));
        Assert.IsTrue(Array.Exists(lightCullResult.visibleLights, (visibleLight) => visibleLight.light.gameObject.name == "Spot Light Inside"));

        Assert.AreEqual(1, lightCullResult.visibleShadowCastingLights.Length);
        Assert.IsTrue(Array.Exists(lightCullResult.visibleShadowCastingLights, (visibleLight) => visibleLight.light.gameObject.name == "Point Light Partial"));

        Assert.AreEqual(2, lightCullResult.visibleOffscreenVertexLights.Length);
        Assert.IsTrue(Array.Exists(lightCullResult.visibleOffscreenVertexLights, (visibleLight) => visibleLight.light.gameObject.name == "Point Light Vertex"));
        Assert.IsTrue(Array.Exists(lightCullResult.visibleOffscreenVertexLights, (visibleLight) => visibleLight.light.gameObject.name == "Point Light 2 Vertex"));

        // The number here should actually be 1 but returns 3 because the off screen vertex light culling is wrong so we have false positives.
        Assert.AreEqual(1, lightCullResult.visibleOffscreenShadowCastingVertexLights.Length);
        Assert.IsTrue(Array.Exists(lightCullResult.visibleOffscreenShadowCastingVertexLights, (visibleLight) => visibleLight.light.gameObject.name == "Spot Light Vertex"));

        TearDown();
    }

    [Test(Description = "Reflection Probe simple frustum culling test")]
    public void ReflectionProbeFrustumCulling()
    {
        Setup("FrustumCullingTest", "FrustumCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        CullingRequests requests = new CullingRequests();
        requests.AddRequest(cullingParams);

        var reflectionCullingResults = Culling.CullReflectionProbes(requests);

        var result = reflectionCullingResults.GetResult(0);
        Assert.AreEqual(2, result.visibleReflectionProbes.Length);
        
        Assert.IsTrue(Array.Exists(result.visibleReflectionProbes, (visibleProbe) => visibleProbe.probe.gameObject.name == "ReflectionProbe Inside"));
        Assert.IsTrue(Array.Exists(result.visibleReflectionProbes, (visibleProbe) => visibleProbe.probe.gameObject.name == "ReflectionProbe Partial"));

        TearDown();
    }
}
