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

    [Test(Description = "Scene frustum culling test")]
    public void SceneFrustumCulling()
    {
        Setup("FrustumCullingTest", "Camera_FrustumCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        CullingResult result = new CullingResult();
        Culling.CullScene(cullingParams, result);

        Assert.AreEqual(3, result.GetVisibleObjectCount());
        TearDown();
    }

    [Test(Description = "Renderer List Test")]
    public void PrepareRendererList()
    {
        Setup("RendererListTest", "Camera_RendererListTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        CullingResult result = new CullingResult();
        Culling.CullScene(cullingParams, result);

        Assert.AreEqual(4, result.GetVisibleObjectCount());

        Culling.PrepareRendererScene(result, null, null);

        List<PrepareRendererListSettings> settingsList = new List<PrepareRendererListSettings>();
        PrepareRendererListSettings settingsOpaque = new PrepareRendererListSettings(m_TestCamera);
        settingsOpaque.renderQueueRange = RenderQueueRange.opaque;
        settingsOpaque.SetShaderTag(0, new ShaderPassName("GBuffer"));
        PrepareRendererListSettings settingsTransparent = new PrepareRendererListSettings(m_TestCamera);
        settingsTransparent.renderQueueRange = RenderQueueRange.transparent;
        settingsTransparent.SetShaderTag(0, new ShaderPassName("Forward"));
        settingsList.Add(settingsOpaque);
        settingsList.Add(settingsTransparent);

        List<RendererList> listOut = new List<RendererList>();
        var listOpaque = new RendererList();
        var listTransparent = new RendererList();
        listOut.Add(listOpaque);
        listOut.Add(listTransparent);

        RendererList.PrepareRendererLists(result, settingsList.ToArray(), listOut.ToArray());
        Assert.AreEqual(3, listOut[0].GetRendererCount());
        Assert.AreEqual(1, listOut[1].GetRendererCount());

        TearDown();
    }

    [Test(Description = "Renderer List Test")]
    public void MismatchPrepareRendererListArguments()
    {
        Setup("RendererListTest", "Camera_RendererListTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        CullingResult result = new CullingResult();
        Culling.CullScene(cullingParams, result);

        Assert.AreEqual(4, result.GetVisibleObjectCount());

        Culling.PrepareRendererScene(result, null, null);

        List<PrepareRendererListSettings> settingsList = new List<PrepareRendererListSettings>();
        PrepareRendererListSettings settings1 = new PrepareRendererListSettings(m_TestCamera);
        settingsList.Add(settings1);

        List<RendererList> listOut = new List<RendererList>();
        var list1 = new RendererList();
        var list2 = new RendererList();
        listOut.Add(list1);
        listOut.Add(list2);

        Assert.Throws<ArgumentException>(() => RendererList.PrepareRendererLists(result, settingsList.ToArray(), listOut.ToArray()));

        TearDown();
    }

    [Test(Description = "Scene not prepared before rendering error")]
    public void SceneNotPreparedError()
    {
        Setup("FrustumCullingTest", "Camera_FrustumCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        CullingResult result = new CullingResult();
        Culling.CullScene(cullingParams, result);

        List<PrepareRendererListSettings> settingsList = new List<PrepareRendererListSettings>();
        PrepareRendererListSettings settings1 = new PrepareRendererListSettings(m_TestCamera);
        settingsList.Add(settings1);

        List<RendererList> listOut = new List<RendererList>();
        var list1 = new RendererList();
        listOut.Add(list1);

        Assert.Throws<ArgumentException>(() => RendererList.PrepareRendererLists(result, settingsList.ToArray(), listOut.ToArray()));

        TearDown();
    }

    [Test(Description = "Light frustum culling test")]
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

        // TODO: The number here should actually be 1 but returns 3 because the off screen vertex light culling is wrong so we have false positives.
        Assert.AreEqual(1, result.visibleOffscreenShadowCastingVertexLights.Length);
        Assert.IsTrue(result.visibleOffscreenShadowCastingVertexLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Spot Light Vertex"));

        TearDown();
    }

    [Test(Description = "Reflection Probe frustum culling test")]
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
    public void ReuseReflectionProbeCullingResult()
    {
        SetupTestScene("ReuseCullingResultTest");

        CullingParameters cullingParams = new CullingParameters();
        ReflectionProbeCullingResult result = new ReflectionProbeCullingResult();

        SetupTestCamera("ReuseResultCamera 1");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        Culling.CullReflectionProbes(cullingParams, result);

        Assert.AreEqual(2, result.visibleReflectionProbes.Length);
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.probe.gameObject.name == "Reflection Probe 1"));
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.probe.gameObject.name == "Reflection Probe 2"));

        SetupTestCamera("ReuseResultCamera 2");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        Culling.CullReflectionProbes(cullingParams, result);

        Assert.AreEqual(1, result.visibleReflectionProbes.Length);
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.probe.gameObject.name == "Reflection Probe 2"));

        TearDown();
    }

    [Test(Description = "Reuse Lighting Result")]
    public void ReuseLightCullingResult()
    {
        SetupTestScene("ReuseCullingResultTest");

        CullingParameters cullingParams = new CullingParameters();
        LightCullingResult result = new LightCullingResult();

        SetupTestCamera("ReuseResultCamera 1");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        Culling.CullLights(cullingParams, result);

        Assert.AreEqual(3, result.visibleLights.Length);
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light 1"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light 2"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Directional Light"));

        SetupTestCamera("ReuseResultCamera 2");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        Culling.CullLights(cullingParams, result);

        Assert.AreEqual(2, result.visibleLights.Length);
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light 2"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Directional Light"));

        TearDown();
    }

    [Test(Description = "Reuse Scene Culling Result")]
    public void ReuseSceneCullingResult()
    {
        SetupTestScene("ReuseCullingResultTest");

        CullingParameters cullingParams = new CullingParameters();
        CullingResult result = new CullingResult();

        SetupTestCamera("ReuseResultCamera 1");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);
        Culling.CullScene (cullingParams, result);
        Assert.AreEqual(2, result.GetVisibleObjectCount());

        SetupTestCamera("ReuseResultCamera 2");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);
        Culling.CullScene(cullingParams, result);
        Assert.AreEqual(1, result.GetVisibleObjectCount());

        TearDown();
    }

    //[Test(Description = "Per Object Light Culling")]
    //public void PerObjectLightCulling()
    //{
    //    Setup("ReuseCullingResultTest", "ReuseResultCamera 1");

    //    CullingParameters cullingParams = new CullingParameters();
    //    ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

    //    CullingResult result = new CullingResult();
    //    Culling.CullScene(cullingParams, result);

    //    LightCullingResult lightResult = new LightCullingResult();
    //    Culling.CullLights(cullingParams, lightResult);

    //    Culling.PrepareRendererScene(result, lightResult, null);

    //     // egzserghz
    //    Assert.AreEqual(3, 2);

    //    TearDown();
    //}
}
