using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.Graphics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Contexts;
using Object = UnityEngine.Object;
using UnityEngine.MultipleSRPGraphicsTest;

[Serializable]
public class BaseGraphicsTests
{
    const int warmupTime = 5;

    // EnterPlayMode reloads the domain. The test runner then restores only the serialized fields of
    // this object and the enumerator position, and runs SetUp again, so the state to restore lives in
    // serialized fields and is captured once per test.
    [SerializeField] RenderPipelineAsset m_SavedDefaultRP;
    [SerializeField] RenderPipelineAsset m_SavedQualityRP;
    [SerializeField] string m_SavedScenePath;
    [SerializeField] float m_SavedTimeScale;
    [SerializeField] bool m_SavedAllowAsyncCompilation;
    [SerializeField] bool m_SavedAsyncShaderCompilation;
    [SerializeField] bool m_StateCaptured;

    [SetUp]
    public void CaptureGlobalState()
    {
        if (m_StateCaptured)
            return;
        m_StateCaptured = true;

        m_SavedDefaultRP = GraphicsSettings.defaultRenderPipeline;
        m_SavedQualityRP = QualitySettings.renderPipeline;
        m_SavedScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        m_SavedTimeScale = Time.timeScale;
        m_SavedAllowAsyncCompilation = ShaderUtil.allowAsyncCompilation;
        m_SavedAsyncShaderCompilation = EditorSettings.asyncShaderCompilation;

        ShaderUtil.allowAsyncCompilation = true;
        EditorSettings.asyncShaderCompilation = true;
    }

    [TearDown]
    public void RestoreGlobalState()
    {
        m_StateCaptured = false;
        GraphicsSettings.defaultRenderPipeline = m_SavedDefaultRP;
        QualitySettings.renderPipeline = m_SavedQualityRP;
        Time.timeScale = m_SavedTimeScale;
        ShaderUtil.allowAsyncCompilation = m_SavedAllowAsyncCompilation;
        EditorSettings.asyncShaderCompilation = m_SavedAsyncShaderCompilation;
        if (!string.IsNullOrEmpty(m_SavedScenePath) && System.IO.File.Exists(m_SavedScenePath))
            EditorSceneManager.OpenScene(m_SavedScenePath, OpenSceneMode.Single);
        else
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [UnityTest, Category("Base")]
    [MultipleSRPGraphicsTest("Assets/CrossPipelineFallbackTests")]
    [Timeout(300 * 1000)]
    [TestNotSupportedOn("0001_SwitchPipeline|0002_FallbackTest", "XR-reuse capture reads the screen at window size, so it cannot honour the test's target resolution", StereoRenderingContext.StereoEnabled)]
    public IEnumerator Run(SceneGraphicsTestCase testCase)
    {
        if (string.IsNullOrEmpty(testCase.ScenePath))
        {
            Assert.Ignore("Ignoring this test because the scene path is empty");
        }

        if (!testCase.ScenePath.Contains("CrossPipelineFallback"))
        {
            Assert.Ignore("Ignoring this test because the scene is not under the CrossPipelineFallbackTests folder");
        }

        GraphicsTestLogger.Log($"Running test case '{testCase}' with scene '{testCase.ScenePath}' {testCase.ReferenceImage.LoadMessage}.");
        Time.timeScale = 0.0f;

        var srpTestSceneAsset = Resources.Load<SRPTestSceneAsset>("SRPTestSceneSO");
        var srpAssets = new List<RenderPipelineAsset>();
        foreach (var testDatas in srpTestSceneAsset.testDatas)
        {
            foreach (var srpAsset in testDatas.srpAssets)
            {
                if(!srpAssets.Contains(srpAsset))
                {
                    srpAssets.Add(srpAsset);
                }
            }
        }

        var parsedTestCaseName = testCase.Name.Split("_");
        var parsedRenderPipelineAsset = parsedTestCaseName[parsedTestCaseName.Length - 1];
        if (parsedRenderPipelineAsset == "BuiltIn")
        {
            GraphicsSettings.defaultRenderPipeline = null;
            yield return null;
        }
        else
        {
            foreach (var srpAsset in srpAssets)
            {
                if (srpAsset != null && srpAsset.name == parsedRenderPipelineAsset)
                {
                    GraphicsSettings.defaultRenderPipeline = srpAsset;
                    yield return null;
                }
            }
        }

        EditorSceneManager.OpenScene(testCase.ScenePath);
        yield return null; // Always wait one frame for scene load

        yield return new EnterPlayMode();

        var settings = Object.FindAnyObjectByType<CrossPipelineTestsSettings>();
        Assert.IsNotNull(settings, "Invalid test scene, couldn't find CrossPipelineTestsSettings");

        var camera = GameObject.FindAnyObjectByType<Camera>();
        Assert.IsNotNull(camera, "Missing camera for graphic tests.");

        //Adjust camera to be sure that everything required by the test fits in the FOV
        var tempRT = RenderTexture.GetTemporary(settings.ImageComparisonSettings.TargetWidth, settings.ImageComparisonSettings.TargetHeight);
        camera.targetTexture = tempRT;

        // Wait for all compilation to end.
        var waitFrame = Mathf.Max(warmupTime, settings.WaitFrames);
        do
        {
            for (var i = 0; i < waitFrame; i++)
                yield return null;
        } while (ShaderUtil.anythingCompiling);

        camera.targetTexture = null;
        tempRT.Release();

        // Defer the failure so play mode is exited before the test fails
        ExceptionDispatchInfo imageAssertFailure = null;
        try
        {
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, camera, settings.ImageComparisonSettings, testCase.ReferenceImage.LoadMessage);
        }
        catch (Exception e)
        {
            imageAssertFailure = ExceptionDispatchInfo.Capture(e);
        }

        yield return new ExitPlayMode();

        imageAssertFailure?.Throw();
    }

#if UNITY_EDITOR
    [TearDown]
    public void DumpImagesInEditor()
    {
        ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }
#endif
}
