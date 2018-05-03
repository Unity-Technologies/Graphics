using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using Unity.Collections;

[TestFixture]
public class ScriptableCullingTests
{
    SceneSetup[]    m_CurrentLoadedScenes;
    Camera          m_TestCamera;

    void Setup(string testName, string cameraName)
    {
        SetupTestScene(testName);
        SetupTestCamera(cameraName);
    }

    void SetupTestScene(string testSceneName)
    {
        string scenePath = string.Format("Assets/ScriptableRenderLoop/Tests/GraphicsTests/Core/Scenes/{0}.unity", testSceneName);

        BackupSceneManagerSetup();
        EditorSceneManager.OpenScene(scenePath);
    }

    void SetupTestCamera(string cameraName)
    {
        string fullCameraName = string.Format(cameraName);

        var cameras = UnityEngine.Object.FindObjectsOfType(typeof(Camera)) as Camera[];
        m_TestCamera = Array.Find(cameras, (value) => value.name == fullCameraName);

        if (m_TestCamera == null)
        {
            // Throw?
            Assert.IsTrue(false, string.Format("Cannot find camera: {0}", cameraName));
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
        Setup("FrustumCullingTest", "Camera_FrustumCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        CullingResult result = new CullingResult();
        Culling.CullScene(cullingParams, result);

        Assert.AreEqual(3, result.GetVisibleObjectCount());

        TearDown();
    }

    //[Test(Description = "Scene not prepared before rendering error")]
    //public void SceneNotPreparedError()
    //{
    //    Setup("FrustumCullingTest", "FrustumCullingTest");

    //    CullingParameters cullingParams = new CullingParameters();
    //    ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

    //    CullingResult result = new CullingResult();
    //    Culling.CullScene(cullingParams, result);

    //    Culling.PrepareScene();

    //    // DrawRenderers ?

    //    Assert.AreEqual(3, 2);

    //    TearDown();
    //}

    [Test(Description = "Light simple frustum culling test")]
    public void LightFrustumCulling()
    {
        Setup("FrustumCullingTest", "Camera_FrustumCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        LightCullingResult result = new LightCullingResult();
        Culling.CullLights(cullingParams, result);

        Assert.AreEqual(4, result.visibleLights.Length);

        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Directional Light"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light Inside"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light 2 Inside"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Spot Light Inside"));

        Assert.AreEqual(1, result.visibleShadowCastingLights.Length);
        Assert.IsTrue(result.visibleShadowCastingLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light Partial"));

        Assert.AreEqual(2, result.visibleOffscreenVertexLights.Length);
        Assert.IsTrue(result.visibleOffscreenVertexLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light Vertex"));
        Assert.IsTrue(result.visibleOffscreenVertexLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light 2 Vertex"));

        // The number here should actually be 1 but returns 3 because the off screen vertex light culling is wrong so we have false positives.
        Assert.AreEqual(1, result.visibleOffscreenShadowCastingVertexLights.Length);
        Assert.IsTrue(result.visibleOffscreenShadowCastingVertexLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Spot Light Vertex"));

        TearDown();
    }

    [Test(Description = "Reflection Probe simple frustum culling test")]
    public void ReflectionProbeFrustumCulling()
    {
        Setup("FrustumCullingTest", "Camera_FrustumCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        ReflectionProbeCullingResult result = new ReflectionProbeCullingResult();
        Culling.CullReflectionProbes(cullingParams, result);

        var visibleProbes = result.visibleReflectionProbes;

        Assert.AreEqual(2, result.visibleReflectionProbes.Length);
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.probe.gameObject.name == "ReflectionProbe Inside"));
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.probe.gameObject.name == "ReflectionProbe Partial"));

        TearDown();
    }

    [Test(Description = "Reuse Reflection Probe Result")]
    public void ReuseReflectionProbeResult()
    {
        SetupTestScene("ReuseCullingResultTest");

        CullingParameters cullingParams = new CullingParameters();
        ReflectionProbeCullingResult result = new ReflectionProbeCullingResult();

        SetupTestCamera("ReuseResultCamera 1");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        Culling.CullReflectionProbes(cullingParams, result);

        var visibleProbes = result.visibleReflectionProbes;

        Assert.AreEqual(2, result.visibleReflectionProbes.Length);
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.probe.gameObject.name == "Reflection Probe 1"));
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.probe.gameObject.name == "Reflection Probe 2"));

        SetupTestCamera("ReuseResultCamera 2");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        Culling.CullReflectionProbes(cullingParams, result);

        visibleProbes = result.visibleReflectionProbes;

        Assert.AreEqual(1, result.visibleReflectionProbes.Length);
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.probe.gameObject.name == "ReflectionProbe 2"));

        TearDown();
    }
}
