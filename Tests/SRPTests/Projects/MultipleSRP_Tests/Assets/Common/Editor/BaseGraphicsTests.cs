using System;
using System.Collections;
using System.Collections.Generic;
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

#if UNITY_WEBGL || UNITY_ANDROID
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return RuntimeGraphicsTestCaseProvider.EnsureGetReferenceImageBundlesAsync();
    }
#endif

    [IgnoreGraphicsTest("0001_SwitchPipeline_UniversalRenderPipelineAsset", "Failed from the start when introducing DX12 coverage", runtimePlatforms: new[] { RuntimePlatform.WindowsEditor }, graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D12 })]
    [IgnoreGraphicsTest("0002_FallbackTest_UniversalRenderPipelineAsset", "Failed from the start when introducing DX12 coverage", runtimePlatforms: new[] { RuntimePlatform.WindowsEditor }, graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D12 })]
    [UnityTest, Category("Base")]
    [MultipleSRPGraphicsTest("Assets/GraphicsTests")]
    [Timeout(300 * 1000)]
    public IEnumerator Run(SceneGraphicsTestCase testCase)
    {
        if (string.IsNullOrEmpty(testCase.ScenePath))
        {
            Assert.Ignore("Ignoring this test because the scene path is empty");
        }

        if (!testCase.ScenePath.Contains("GraphicsTest"))
        {
            Assert.Ignore("Ignoring this test because the scene is not under GraphicsTests folder, or not named with GraphicsTest");
        }

        GraphicsTestLogger.Log($"Running test case {testCase.ScenePath} with reference image {testCase.ScenePath}. {testCase.ReferenceImage.LoadMessage}.");
#if UNITY_WEBGL || UNITY_ANDROID
        RuntimeGraphicsTestCaseProvider.AssociateReferenceImageWithTest(testCase);
#endif
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
            foreach (var srpAsset in srpAssets)
            {
                if (srpAsset.name == parsedRenderPipelineAsset)
                {
                    GraphicsSettings.defaultRenderPipeline = srpAsset;
                    yield return null;
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

            try
            {
                ImageAssert.AreEqual(testCase.ReferenceImage.Image, camera, settings.ImageComparisonSettings, testCase.ReferenceImage.LoadMessage);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            yield return new ExitPlayMode();
            GraphicsSettings.defaultRenderPipeline = currentRPAsset;
            Time.timeScale = oldTimeScale;
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
