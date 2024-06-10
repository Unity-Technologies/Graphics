using NUnit.Framework;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.Graphics;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;

public class GizmoRenderingTests
{
    public const string referenceImagePath = "Assets/ReferenceImages";
    public const string referenceImageBasePath = "Assets/ReferenceImagesBase";

    [Test, Category("Graphics"), CodeBasedGraphicsTest(referenceImagePath, "Assets/ActualImages")]
    public async Task GizmoRenderingWorks()
    {
        const string scenePath = "Assets/Scenes/CodeBasedTests/GizmoRendering.unity";
        EditorSceneManager.OpenScene(scenePath);

        SceneView sceneView = EditorWindow.CreateWindow<SceneView>();
        sceneView.overlayCanvas.overlaysEnabled = false;
        sceneView.showGrid = false;


        Texture2D capturedTexture = await EditorWindowCapture.CaptureAsync(sceneView, new SceneViewCaptureSettings(width: 512, height: 512, TimeSpan.Zero, new TimeSpan(0, 0, 0, 1, 0), imageComparisonViewpoint: Camera.main.transform));
        sceneView.Close();

        ImageComparisonSettings imageComparisonSettings = new()
        {
            TargetWidth = capturedTexture.width,
            TargetHeight = capturedTexture.height,
            AverageCorrectnessThreshold = 0.01f
        };

        var referenceImageFolder = Path.Combine(referenceImagePath, TestUtils.GetCurrentTestResultsFolderPath());
        var referenceImageFile  = EditorUtils.ReplaceCharacters(Path.Combine(referenceImageFolder, $"GizmoRenderingWorks.png"));
        var referenceImage = AssetDatabase.LoadAssetAtPath<Texture2D>(referenceImageFile);

        if (referenceImage == null)
        {
            var referenceImageFileBase = EditorUtils.ReplaceCharacters(Path.Combine(referenceImageBasePath, $"GizmoRenderingWorks.png"));
            referenceImage = AssetDatabase.LoadAssetAtPath<Texture2D>(referenceImageFileBase);

            if (referenceImage == null)
            {
                throw new System.Exception($"Reference image not found at '{referenceImageFile}' and also not in base at '{referenceImageFileBase}'");
            }

            Debug.Log($"Using reference image from '{referenceImageFileBase}'");
        }
        else
        {
            Debug.Log($"Using reference image from '{referenceImageFile}'");
        }

        ImageAssert.AreEqual(referenceImage, capturedTexture, imageComparisonSettings);
    }
}
