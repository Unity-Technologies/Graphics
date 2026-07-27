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
using Object = UnityEngine.Object;
using UnityEngine.MultipleSRPGraphicsTest;

public class BaseGraphicsTests
{
    const int warmupTime = 5;

    RenderPipelineAsset m_SavedDefaultRP;
    RenderPipelineAsset m_SavedQualityRP;
    string m_SavedScenePath;

    [SetUp]
    public void CaptureGlobalState()
    {
        m_SavedDefaultRP = GraphicsSettings.defaultRenderPipeline;
        m_SavedQualityRP = QualitySettings.renderPipeline;
        m_SavedScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
    }

    [TearDown]
    public void RestoreGlobalState()
    {
        // These tests switch the active pipeline (incl. to Built-In/null) and open a
        // scene; restore it all so later tests in the run aren't affected by the leak.
        GraphicsSettings.defaultRenderPipeline = m_SavedDefaultRP;
        QualitySettings.renderPipeline = m_SavedQualityRP;
        Time.timeScale = 1.0f;
        if (!string.IsNullOrEmpty(m_SavedScenePath) && System.IO.File.Exists(m_SavedScenePath))
            EditorSceneManager.OpenScene(m_SavedScenePath, OpenSceneMode.Single);
        else
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [UnityTest, Category("Base")]
    [MultipleSRPGraphicsTest("Assets/CrossPipelineFallbackTests")]
    [Timeout(300 * 1000)]
    [Ignore("Leaves GPU/compute state that fails CustomAlphaTestedPickingPass_URP on Windows/DX11 in the same session. UUM-147593.")]
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
        var oldTimeScale = Time.timeScale;
        var currentRPAsset = GraphicsSettings.defaultRenderPipeline;
        Time.timeScale = 0.0f;

        using (new AsyncShaderCompilationScope())
        {
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

            // Defer the failure so play mode is exited and pipeline state restored first
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
            GraphicsSettings.defaultRenderPipeline = currentRPAsset;
            Time.timeScale = oldTimeScale;

            imageAssertFailure?.Throw();
        }
    }

#if UNITY_EDITOR
    [TearDown]
    public void DumpImagesInEditor()
    {
        ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }
#endif

    class AsyncShaderCompilationScope : IDisposable
    {
        readonly bool m_OldValueShaderUtil = ShaderUtil.allowAsyncCompilation;
        readonly bool m_OldValueEditorSettings = EditorSettings.asyncShaderCompilation;

        public AsyncShaderCompilationScope()
        {
            ShaderUtil.allowAsyncCompilation = true;
            EditorSettings.asyncShaderCompilation = true;
        }

        public void Dispose()
        {
            ShaderUtil.allowAsyncCompilation = m_OldValueShaderUtil;
            EditorSettings.asyncShaderCompilation = m_OldValueEditorSettings;
        }
    }
}
