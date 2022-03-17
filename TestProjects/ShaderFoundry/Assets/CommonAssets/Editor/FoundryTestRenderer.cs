using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using NUnit.Framework;

public class FoundryTestRenderer
{
    internal const int defaultResolution = 128;
    internal GraphicsFormat defaultFormat = GraphicsFormat.R8G8B8A8_SRGB;

    static internal PreviewSceneResources previewScene = new PreviewSceneResources();

    internal delegate void SetupMaterialDelegate(Material m);

    internal int wrongImageCount;
    internal int mostWrongPixels;
    internal string mostWrongString;
    internal void ResetTestReporting()
    {
        wrongImageCount = 0;
        mostWrongPixels = 0;
        mostWrongString = string.Empty;
    }

    internal void ReportTests()
    {
        if (wrongImageCount > 0)
        {
            Assert.That(false, $"{wrongImageCount} images failed, worst was: {mostWrongString}");
        }
    }

    // we apply a transform to the test setup, so that the transform matrices are non-trivial
    internal Vector3 testPosition = new Vector3(0.24699998f, 0.51900005f, 0.328999996f);
    internal Quaternion testRotation = new Quaternion(-0.164710045f, -0.0826543793f, -0.220811233f, 0.957748055f);

    internal void CheckForShaderErrors(Shader shader)
    {
        if (ShaderUtil.ShaderHasError(shader))
        {
            var messages = ShaderUtil.GetShaderMessages(shader);
            foreach (var message in messages)
            {
                // TODO @ SHADERS: We should probably check for warnings at some point
                if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                    throw new Exception($"{message.file} {message.line}: {message.message}");
            }
        }
    }

    internal int TestShaderIsConstantColor(Shader shader, string filePrefix, Color expectedColor, SetupMaterialDelegate setupMaterial = null, int expectedIncorrectPixels = 0, int errorThreshold = 0, bool compareAlpha = true, bool reportArtifacts = true)
    {
        CheckForShaderErrors(shader);

        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(defaultResolution, defaultResolution, defaultFormat, depthBufferBits: 32);
        var target = RenderTexture.GetTemporary(descriptor);

        RenderQuadWithShader(shader, target, testPosition, testRotation, setupMaterial, useSRP: true);

        // default expected color is green (test shaders should be set up to return green on success)
        int incorrectPixels = CountPixelsNotEqual(target, expectedColor, compareAlpha, out float averageMismatchDelta, errorThreshold);

        if (incorrectPixels != expectedIncorrectPixels)
        {
            if (reportArtifacts)
            {
                SaveToPNG(target, $"test-results/NodeTests/{filePrefix}.png");
            }

            // record failure
            var wrongString = $"{filePrefix}: expected color: {expectedColor}, incorrect pixels expected: {expectedIncorrectPixels} actual: {incorrectPixels}, delta: {averageMismatchDelta}";

            wrongImageCount++;
            int wrongPixels = Math.Abs(incorrectPixels - expectedIncorrectPixels);
            if (wrongPixels > mostWrongPixels)
            {
                mostWrongPixels = wrongPixels;
                mostWrongString = wrongString;
            }

            Debug.Log(wrongString);
        }

        RenderTexture.ReleaseTemporary(target);
        return incorrectPixels;
    }

    // for a DIFF test we assume the Shader is set up to have three keywords:  _MODE_DIFF, _MODE_EXPECTED, _MODE_ACTUAL
    // it is expecting the _MODE_DIFF shader to be green (0,255,0) when _MODE_EXPECTED and _MODE_ACTUAL are identical (and red otherwise)
    internal int TestShaderDiffIsGreen(Shader shader, string filePrefix, SetupMaterialDelegate setupMaterial = null, int expectedIncorrectPixels = 0, int errorThreshold = 0, bool reportArtifacts = true)
    {
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(defaultResolution, defaultResolution, defaultFormat, depthBufferBits: 32);
        var target = RenderTexture.GetTemporary(descriptor);

        RenderQuadWithShader(shader, target, testPosition, testRotation, setupMaterial, Mode.DIFF, useSRP: true);

        // default expected color is green (test shaders should be set up to return green on success)
        var expectedColor = new Color32(0, 255, 0, 255);
        int incorrectPixels = CountPixelsNotEqual(target, expectedColor, false, out float averageMismatchDelta, errorThreshold);

        if (incorrectPixels != expectedIncorrectPixels)
        {
            if (reportArtifacts)
            {
                SaveToPNG(target, $"test-results/NodeTests/{filePrefix}.diff.png");

                RenderQuadWithShader(shader, target, testPosition, testRotation, setupMaterial, Mode.EXPECTED, useSRP: true);
                SaveToPNG(target, $"test-results/NodeTests/{filePrefix}.expected.png");

                RenderQuadWithShader(shader, target, testPosition, testRotation, setupMaterial, Mode.ACTUAL, useSRP: true);
                SaveToPNG(target, $"test-results/NodeTests/{filePrefix}.png");
            }

            // record failure
            var wrongString = $"{filePrefix}: expected color: {expectedColor}, incorrect pixels expected: {expectedIncorrectPixels} actual: {incorrectPixels}, delta: {averageMismatchDelta}";

            wrongImageCount++;
            int wrongPixels = Math.Abs(incorrectPixels - expectedIncorrectPixels);
            if (wrongPixels > mostWrongPixels)
            {
                mostWrongPixels = wrongPixels;
                mostWrongString = wrongString;
            }

            Debug.Log(wrongString);
        }

        RenderTexture.ReleaseTemporary(target);
        return incorrectPixels;
    }

    internal static void CreateDirectoriesForFilePath(string systemFilePath)
    {
        var dirPath = Path.GetDirectoryName(systemFilePath);
        CreateDirectories(dirPath);
    }

    internal static void CreateDirectories(string systemDirectoryPath)
    {
        Directory.CreateDirectory(systemDirectoryPath);
    }

    // sends a message to the test infrastructure to report the artifact in the test results
    internal static void ReportArtifact(string artifactPath)
    {
        var fullpath = Path.GetFullPath(artifactPath);
        var message = Unity.TestProtocol.Messages.ArtifactPublishMessage.Create(fullpath);
        Debug.Log(Unity.TestProtocol.UnityTestProtocolMessageBuilder.Serialize(message));
    }

    internal static void SaveToPNG(RenderTexture target, string path, bool createDirectory = true, bool reportArtifact = true)
    {
        if (createDirectory)
            CreateDirectoriesForFilePath(path);

        Texture2D temp = new Texture2D(target.width, target.height, TextureFormat.RGBA32, mipChain: false, linear: false);

        var previousRenderTexture = RenderTexture.active;
        RenderTexture.active = target;
        temp.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        RenderTexture.active = previousRenderTexture;

        var pngData = temp.EncodeToPNG();
        if (pngData != null)
        {
            File.WriteAllBytes(path, pngData);
        }
        UnityEngine.Object.DestroyImmediate(temp);

        if (reportArtifact)
            ReportArtifact(path);
    }

    internal static int CountPixelsNotEqual(RenderTexture target, Color value, bool compareAlpha, out float averageMismatchDelta, int errorThreshold = 0)
    {
        Texture2D temp = new Texture2D(target.width, target.height, TextureFormat.RGBAFloat, mipChain: false, linear: false);

        var previousRenderTexture = RenderTexture.active;
        RenderTexture.active = target;
        temp.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        RenderTexture.active = previousRenderTexture;

        // Convert from RGB32 error threshold to floats. If the old error threshold was 0, use a half pixel to account for rounding.
        float floatErrorThreshold = (errorThreshold != 0) ? errorThreshold / 256.0f : 0.5f;
        int mismatchCount = 0;
        float deltaSum = 0;
        var pixels = temp.GetPixels(0);
        foreach (var pixel in pixels)
        {
            float deltaR = Mathf.Abs(pixel.r - value.r);
            float deltaG = Mathf.Abs(pixel.g - value.g);
            float deltaB = Mathf.Abs(pixel.b - value.b);
            float deltaA = Mathf.Abs(pixel.a - value.a);
            float deltaRGB = Mathf.Max(Mathf.Max(deltaR, deltaG), deltaB);
            float delta = Mathf.Max(deltaRGB, compareAlpha ? deltaA : 0);
            if (delta > floatErrorThreshold)
            {
                deltaSum += delta;
                mismatchCount++;
            }
        }

        UnityEngine.Object.DestroyImmediate(temp);
        averageMismatchDelta = ((mismatchCount > 0) ? (deltaSum / mismatchCount) : 0.0f);
        return mismatchCount;
    }

    internal enum Mode
    {
        DIFF,
        EXPECTED,
        ACTUAL
    }

    internal void SetKeyword(Material mat, string keyword, bool enabled)
    {
        if (enabled)
            mat.EnableKeyword(keyword);
        else
            mat.DisableKeyword(keyword);
    }

    internal void RenderQuadWithShader(Shader shader, RenderTexture target, Vector3 scenePosition, Quaternion sceneRotation, SetupMaterialDelegate setupMaterial = null, Mode? mode = null, bool useSRP = false)
    {
        // build the material
        var mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

        if (mode.HasValue)
        {
            SetKeyword(mat, "_MODE_DIFF", (mode == Mode.DIFF));
            SetKeyword(mat, "_MODE_EXPECTED", (mode == Mode.EXPECTED));
            SetKeyword(mat, "_MODE_ACTUAL", (mode == Mode.ACTUAL));
        }

        if (setupMaterial != null)
            setupMaterial(mat);

        // now render the quad with the material
        var camXform = previewScene.camera.transform;

        // setup 2D quad render: camera
        camXform.position = -Vector3.forward * 2 + scenePosition;
        camXform.rotation = sceneRotation;
        previewScene.camera.orthographicSize = 0.5f;
        previewScene.camera.orthographic = true;

        // and quad mesh transform
        var quadMatrix = Matrix4x4.TRS(camXform.position + camXform.forward * 2, camXform.rotation, Vector3.one);

        // render with it
        RenderMeshWithMaterial(previewScene.camera, previewScene.quad, quadMatrix, mat, target, useSRP);
        UnityEngine.Object.DestroyImmediate(mat);
    }

    internal static void RenderMeshWithMaterial(Camera cam, Mesh mesh, Matrix4x4 transform, Material mat, RenderTexture target, bool useSRP = true)
    {
        // Force async compile OFF
        var wasAsyncAllowed = ShaderUtil.allowAsyncCompilation;
        ShaderUtil.allowAsyncCompilation = false;

        var previousRenderTexture = RenderTexture.active;
        RenderTexture.active = target;

        GL.Clear(true, true, Color.black);

        cam.targetTexture = target;
        Graphics.DrawMesh(
            mesh: mesh,
            matrix: transform,
            material: mat,
            layer: 1,
            camera: cam,
            submeshIndex: 0,
            properties: null,
            castShadows: ShadowCastingMode.Off,
            receiveShadows: false,
            probeAnchor: null,
            useLightProbes: false);

        var previousUseSRP = Unsupported.useScriptableRenderPipeline;
        Unsupported.useScriptableRenderPipeline = useSRP;
        cam.Render();
        Unsupported.useScriptableRenderPipeline = previousUseSRP;

        RenderTexture.active = previousRenderTexture;
        ShaderUtil.allowAsyncCompilation = wasAsyncAllowed;
    }
}
