using NUnit.Framework;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.Graphics;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

public class GizmoRenderingTests
{
    [Test, Category("Graphics"), GraphicsTest]
    [Ignore("Test disabled due to instabilities, UUM-92518")]
    public async Task GizmoRenderingWorks(GraphicsTestCase testCase)
    {
        // Failing on OpenGL
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore)
            return;

        const string scenePath = "Assets/CodeBasedTests/GizmoRendering.unity";
        const int numFramesToWarmup = 4;

        EditorSceneManager.OpenScene(scenePath);

        var sceneView = EditorWindow.CreateWindow<SceneView>();
        sceneView.overlayCanvas.overlaysEnabled = false;
        sceneView.showGrid = false;

        for (int i = 0; i < numFramesToWarmup; i++)
        {
            await Task.Yield();
        }

        var captureSettings = new SceneViewCaptureSettings(512, 512, TimeSpan.Zero, new TimeSpan(0, 0, 0, 1, 0), Camera.main.transform);
        var capturedTexture = await EditorWindowCapture.CaptureAsync(sceneView, captureSettings);
        sceneView.Close();

        ImageComparisonSettings imageComparisonSettings = new()
        {
            TargetWidth = capturedTexture.width,
            TargetHeight = capturedTexture.height,
            AverageCorrectnessThreshold = 0.01f
        };

        ImageAssert.AreEqual(testCase.ReferenceImage.Image, capturedTexture, imageComparisonSettings);
    }
}
