using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using NUnit.Framework;
using UnityEditor.SceneManagement;

[TestFixture]
public class ScriptableRendererListTests
{
    SceneSetup[]    m_CurrentLoadedScenes;
    Camera          m_TestCamera;
    Culler          m_Culler;

    void Setup(string testName, string cameraName)
    {
        m_Culler = new Culler();

        SetupTestScene(testName);
        SetupTestCamera(cameraName);
    }

    void SetupTestScene(string testSceneName)
    {
        string scenePath = string.Format("Assets/Scenes/{0}.unity", testSceneName);

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

    [Test(Description = "Renderer List Test")]
    public void PrepareRendererList()
    {
        Setup("RendererListTest", "Camera_RendererListTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        RenderersCullingResult result = new RenderersCullingResult();
        m_Culler.CullRenderers(cullingParams, result);

        Assert.AreEqual(4, result.GetVisibleObjectCount());

        List<RendererListSettings> settingsList = new List<RendererListSettings>();
        RendererListSettings settingsOpaque = new RendererListSettings(camera: m_TestCamera);
        settingsOpaque.renderQueueRange = RenderQueueRange.opaque;
        settingsOpaque.SetShaderTag(0, new ShaderTagId("Forward"));
        RendererListSettings settingsTransparent = new RendererListSettings(camera: m_TestCamera);
        settingsTransparent.renderQueueRange = RenderQueueRange.transparent;
        settingsTransparent.SetShaderTag(0, new ShaderTagId("Forward"));
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

        RenderersCullingResult result = new RenderersCullingResult();
        m_Culler.CullRenderers(cullingParams, result);

        Assert.AreEqual(4, result.GetVisibleObjectCount());

        List<RendererListSettings> settingsList = new List<RendererListSettings>();
        RendererListSettings settings1 = new RendererListSettings(camera: m_TestCamera);
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

        RenderersCullingResult result = new RenderersCullingResult();
        m_Culler.CullRenderers(cullingParams, result);

        List<RendererListSettings> settingsList = new List<RendererListSettings>();
        RendererListSettings settings1 = new RendererListSettings(camera: m_TestCamera);
        settingsList.Add(settings1);

        List<RendererList> listOut = new List<RendererList>();
        var list1 = new RendererList();
        listOut.Add(list1);

        Assert.Throws<ArgumentException>(() => RendererList.PrepareRendererLists(result, settingsList.ToArray(), listOut.ToArray()));

        TearDown();
    }

    //[Test(Description = "Per Object Light Culling")]
    //public void PerObjectLightCulling()
    //{
    //    Setup("ReuseCullingResultTest", "ReuseResultCamera 1");

    //    CullingParameters cullingParams = new CullingParameters();
    //    ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

    //    RenderersCullingResult result = new RenderersCullingResult();
    //    Culling.CullRenderers(cullingParams, result);

    //    LightCullingResult lightResult = new LightCullingResult();
    //    Culling.CullLights(cullingParams, lightResult);

    //    Culling.PrepareRendererScene(result, lightResult, null);

    //     // egzserghz
    //    Assert.AreEqual(3, 2);

    //    TearDown();
    //}
}
