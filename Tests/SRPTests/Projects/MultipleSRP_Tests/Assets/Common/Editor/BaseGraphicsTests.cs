using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.Graphics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using Object = UnityEngine.Object;

public class BaseGraphicsTests
{
    const int warmupTime = 5;

    [UnityTest, Category("Base")]
    [UseGraphicsTestCases]
    [Timeout(300 * 1000)]
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        if (string.IsNullOrEmpty(testCase.ScenePath))
        {
            Assert.Ignore("Ignoring this test because the scene path is empty");
        }

        if (!testCase.ScenePath.Contains("GraphicsTest"))
        {
            Assert.Ignore("Ignoring this test because the scene is not under GraphicsTests folder, or not named with GraphicsTest");
        }

        var oldTimeScale = Time.timeScale;
        var currentRPAsset = GraphicsSettings.renderPipelineAsset;
        Time.timeScale = 0.0f;

        using (new AsyncShaderCompilationScope())
        {
            GraphicsSettings.renderPipelineAsset = testCase.SRPAsset;
            yield return null;

            EditorSceneManager.OpenScene(testCase.ScenePath);
            yield return null; // Always wait one frame for scene load

            yield return new EnterPlayMode();

            var settings = Object.FindFirstObjectByType<CrossPipelineTestsSettings>();
            Assert.IsNotNull(settings, "Invalid test scene, couldn't find CrossPipelineTestsSettings");

            var camera = GameObject.FindFirstObjectByType<Camera>();
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

            try
            {
                ImageAssert.AreEqual(testCase.ReferenceImage, camera, settings.ImageComparisonSettings, testCase.ReferenceImagePathLog);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            yield return new ExitPlayMode();
            GraphicsSettings.renderPipelineAsset = currentRPAsset;
            Time.timeScale = oldTimeScale;
        }
    }

    [TearDown]
    public void DumpImagesInEditor()
    {
        ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }

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
