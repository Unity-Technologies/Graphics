using NUnit.Framework;
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.Graphics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools.Graphics;

public class SceneViewVisibilityTests
{
    static int s_ForwardPlusQualityLevel = 7;
    int m_PreviousQualityLevelIndex = 0;

    [SetUp]
    public void SetUp()
    {
        const string scenePath = "Assets/CodeBasedTests/SceneViewVisibility.unity";
        EditorSceneManager.OpenScene(scenePath);

        m_PreviousQualityLevelIndex = QualitySettings.GetQualityLevel();
        QualitySettings.SetQualityLevel(s_ForwardPlusQualityLevel, true);
    }

    [TearDown]
    public void TearDown()
    {
        QualitySettings.SetQualityLevel(m_PreviousQualityLevelIndex, true);
    }

    async Task RunTest(GraphicsTestCase testCase, Action<SceneView> setup)
    {
        const int numFramesToWarmup = 4;

        var sceneView = EditorWindow.CreateWindow<SceneView>();
        sceneView.overlayCanvas.overlaysEnabled = false;
        sceneView.showGrid = false;
        sceneView.name = "TestSceneView";

        for (int i = 0; i < numFramesToWarmup; i++)
        {
            await Task.Yield();
        }

        setup(sceneView);

        var halfASecond = new TimeSpan(0, 0, 0, 0, 500);
        var captureSettings = new SceneViewCaptureSettings(128, 128, halfASecond, halfASecond, Camera.main.transform);

        var renderPipelineAsset = GraphicsSettings.currentRenderPipeline as IGPUResidentRenderPipeline;
        Assert.IsNotNull(renderPipelineAsset);

        Texture2D capturedTexture = null;
        Texture2D grdCapturedTexture = null;

        // GRD not supported on OpenGLCore
        if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLCore)
        {
            // Capture with GRD
            renderPipelineAsset.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;
            sceneView.Repaint();
            grdCapturedTexture = await EditorWindowCapture.CaptureAsync(sceneView, captureSettings);
        }

        // Capture without GRD
        renderPipelineAsset.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
        sceneView.Repaint();
        capturedTexture = await EditorWindowCapture.CaptureAsync(sceneView, captureSettings);

        sceneView.Close();

        ImageComparisonSettings imageComparisonSettings = new()
        {
            TargetWidth = capturedTexture.width,
            TargetHeight = capturedTexture.height,
            AverageCorrectnessThreshold = 0.01f
        };

        try
        {
            ImageAssert.AreEqual(testCase.ReferenceImage.Image, capturedTexture, imageComparisonSettings);
        }
        catch (Exception e)
        {
            Assert.Fail("Without GPU Resident Drawer : " + e.Message);
        }

        try
        {
            if (grdCapturedTexture != null)
                ImageAssert.AreEqual(testCase.ReferenceImage.Image, grdCapturedTexture, imageComparisonSettings);
        }
        catch (Exception e)
        {
            Assert.Fail("With GPU Resident Drawer : " + e.Message);
        }
    }

    void SetupGameObjectVisibility(SceneView _)
    {
        var cube = GameObject.Find("Cube");
        var sphere = GameObject.Find("Sphere");

        if (cube)
        {
            SceneVisibilityManager.instance.Hide(cube, false);
        }

        if (sphere)
        {
            SceneVisibilityManager.instance.Hide(sphere, false);
        }
    }

    void SetupSceneVisibility(SceneView sceneView)
    {
        SetupGameObjectVisibility(sceneView);
        SceneVisibilityManager.instance.Hide(SceneManager.GetActiveScene());
    }

    void SetupSceneVisibilityReset(SceneView sceneView)
    {
        SetupSceneVisibility(sceneView);
        SceneVisibilityManager.instance.Show(SceneManager.GetActiveScene());
    }

    static void SetSceneVisibilityEnabled(SceneView sceneView, bool isEnabled)
    {
        sceneView.sceneVisActive = isEnabled;
    }

    void SetupSceneVisibilityEnabled(SceneView sceneView)
    {
        SetupSceneVisibility(sceneView);
        SetSceneVisibilityEnabled(sceneView, false);
    }

    void SetupSceneVisibilityEnabledReset(SceneView sceneView)
    {
        SetupSceneVisibilityEnabled(sceneView);
        SetSceneVisibilityEnabled(sceneView, true);
    }

    [Test, Category("Graphics"), GraphicsTest]
    [Ignore("Test disabled due to needing BRG stripping level set to Keep All with increased build times, UUM-120684")]
    public async Task GameObjectVisibilityWorks(GraphicsTestCase testCase)
    {
        await RunTest(testCase, SetupGameObjectVisibility);
    }

    [Test, Category("Graphics"), GraphicsTest]
    [Ignore("Test disabled due to needing BRG stripping level set to Keep All with increased build times, UUM-120684")]
    public async Task SceneVisibilityWorks(GraphicsTestCase testCase)
    {
        await RunTest(testCase, SetupSceneVisibility);
    }

    [Test, Category("Graphics"), GraphicsTest]
    [Ignore("Test disabled due to needing BRG stripping level set to Keep All with increased build times, UUM-120684")]
    public async Task SceneVisibilityResetWorks(GraphicsTestCase testCase)
    {
        await RunTest(testCase, SetupSceneVisibilityReset);
    }

    [Test, Category("Graphics"), GraphicsTest]
    [Ignore("Test disabled due to needing BRG stripping level set to Keep All with increased build times, UUM-120684")]
    public async Task SceneVisibilityEnabledWorks(GraphicsTestCase testCase)
    {
        await RunTest(testCase, SetupSceneVisibilityEnabled);
    }

    [Test, Category("Graphics"), GraphicsTest]
    [Ignore("Test disabled due to needing BRG stripping level set to Keep All with increased build times, UUM-120684")]
    public async Task SceneVisibilityEnabledResetWorks(GraphicsTestCase testCase)
    {
        await RunTest(testCase, SetupSceneVisibilityEnabledReset);
    }
}

