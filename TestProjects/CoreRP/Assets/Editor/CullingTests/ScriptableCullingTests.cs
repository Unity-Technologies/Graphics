using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using NUnit.Framework;
using UnityEditor.SceneManagement;

[TestFixture]
public class ScriptableCullingTests
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

    [Test(Description = "Renderers frustum culling test")]
    public void RenderersFrustumCulling()
    {
        Setup("FrustumCullingTest", "Camera_FrustumCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        RenderersCullingResult result = new RenderersCullingResult();
        m_Culler.CullRenderers(cullingParams, result);

        Assert.AreEqual(3, result.GetVisibleObjectCount());
        TearDown();
    }

    [Test(Description = "Light frustum culling test")]
    public void LightFrustumCulling()
    {
        Setup("FrustumCullingTest", "Camera_FrustumCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        LightCullingResult result = new LightCullingResult();
        m_Culler.CullLights(cullingParams, result);

        Assert.AreEqual(5, result.visibleLights.Length);

        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Directional Light"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light Inside"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light 2 Inside"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Spot Light Inside"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light Partial"));

        TearDown();
    }

    [Test(Description = "Reflection Probe frustum culling test")]
    public void ReflectionProbeFrustumCulling()
    {
        Setup("FrustumCullingTest", "Camera_FrustumCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        ReflectionProbeCullingResult result = new ReflectionProbeCullingResult();
        m_Culler.CullReflectionProbes(cullingParams, result);

        var visibleProbes = result.visibleReflectionProbes;

        Assert.AreEqual(2, result.visibleReflectionProbes.Length);
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.reflectionProbe.gameObject.name == "ReflectionProbe Inside"));
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.reflectionProbe.gameObject.name == "ReflectionProbe Partial"));

        TearDown();
    }

    [Test(Description = "Renderers occlusion culling test")]
    public void RenderersOcclusionCulling()
    {
        Setup("OcclusionCullingTest", "Camera_OcclusionCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);
        //cullingParams.parameters.cullingFlags |= CullFlag.OcclusionCull;

        RenderersCullingResult result = new RenderersCullingResult();
        m_Culler.CullRenderers(cullingParams, result);

        Assert.AreEqual(3, result.GetVisibleObjectCount());
        TearDown();
    }

    [Test(Description = "Light occlusion culling test")]
    public void LightOcclusionCulling()
    {
        Setup("OcclusionCullingTest", "Camera_OcclusionCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);
        //cullingParams.parameters.cullingFlags |= CullFlag.OcclusionCull;

        RenderersCullingResult renderersResult = new RenderersCullingResult();
        m_Culler.CullRenderers(cullingParams, renderersResult);

        LightCullingResult result = new LightCullingResult();
        m_Culler.CullLights(cullingParams, result);

        Assert.AreEqual(3, result.visibleLights.Length);

        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Directional Light"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Spot Light"));

        TearDown();
    }

    [Test(Description = "Reflection Probe occlusion culling test")]
    public void ReflectionProbeOcclusionCulling()
    {
        Setup("OcclusionCullingTest", "Camera_OcclusionCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);
        //cullingParams.parameters.cullingFlags |= CullFlag.OcclusionCull;

        ReflectionProbeCullingResult result = new ReflectionProbeCullingResult();
        m_Culler.CullReflectionProbes(cullingParams, result);

        var visibleProbes = result.visibleReflectionProbes;

        Assert.AreEqual(1, result.visibleReflectionProbes.Length);
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.reflectionProbe.gameObject.name == "Reflection Probe"));

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

        m_Culler.CullReflectionProbes(cullingParams, result);

        Assert.AreEqual(2, result.visibleReflectionProbes.Length);
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.reflectionProbe.gameObject.name == "Reflection Probe 1"));
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.reflectionProbe.gameObject.name == "Reflection Probe 2"));

        SetupTestCamera("ReuseResultCamera 2");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        m_Culler.CullReflectionProbes(cullingParams, result);

        Assert.AreEqual(1, result.visibleReflectionProbes.Length);
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.reflectionProbe.gameObject.name == "Reflection Probe 2"));

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

        m_Culler.CullLights(cullingParams, result);

        Assert.AreEqual(3, result.visibleLights.Length);
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light 1"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light 2"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Directional Light"));

        SetupTestCamera("ReuseResultCamera 2");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        m_Culler.CullLights(cullingParams, result);

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
        RenderersCullingResult result = new RenderersCullingResult();

        SetupTestCamera("ReuseResultCamera 1");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);
        m_Culler.CullRenderers (cullingParams, result);
        Assert.AreEqual(2, result.GetVisibleObjectCount());

        SetupTestCamera("ReuseResultCamera 2");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);
        m_Culler.CullRenderers(cullingParams, result);
        Assert.AreEqual(1, result.GetVisibleObjectCount());

        TearDown();
    }

    [Test(Description = "Culling Groups")]
    public void CullingGroups()
    {
        Setup("CullingGroupTest", "Camera_CullingGroupTest");

        CullingGroup cullingGroup = new CullingGroup();
        cullingGroup.targetCamera = m_TestCamera;

        BoundingSphere[] spheres = new BoundingSphere[3];
        int[] resultIndices = new int[3];
        // Also set up an int for storing the actual number of results that have been placed into the array
        int numResults;

        spheres[0] = new BoundingSphere(new Vector3(0.0f, 1.0f, -3.0f), 1f);
        spheres[1] = new BoundingSphere(new Vector3(0.0f, 1.0f, -1.0f), 1f);
        spheres[2] = new BoundingSphere(new Vector3(0.0f, 1.0f, -15.0f), 1f);
        cullingGroup.SetBoundingSpheres(spheres);
        cullingGroup.SetBoundingSphereCount(3);

        CullingParameters cullingParams = new CullingParameters();
        RenderersCullingResult result = new RenderersCullingResult();

        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);
        m_Culler.CullRenderers(cullingParams, result);

        numResults = cullingGroup.QueryIndices(true, resultIndices, 0);
        Assert.AreEqual(2, numResults);
        Assert.AreEqual(0, resultIndices[0]);
        Assert.AreEqual(1, resultIndices[1]);

        numResults = cullingGroup.QueryIndices(false, resultIndices, 0);
        Assert.AreEqual(1, numResults);
        Assert.AreEqual(2, resultIndices[0]);

        // Move spheres;
        spheres[1] = new BoundingSphere(new Vector3(0.0f, 1.0f, -15.0f), 1f);
        spheres[2] = new BoundingSphere(new Vector3(0.0f, 1.0f, -1.0f), 1f);

        m_Culler.CullRenderers(cullingParams, result);

        numResults = cullingGroup.QueryIndices(true, resultIndices, 0);
        Assert.AreEqual(2, numResults);
        Assert.AreEqual(0, resultIndices[0]);
        Assert.AreEqual(2, resultIndices[1]);

        numResults = cullingGroup.QueryIndices(false, resultIndices, 0);
        Assert.AreEqual(1, numResults);
        Assert.AreEqual(1, resultIndices[0]);

        cullingGroup.Dispose();
        cullingGroup = null;

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
