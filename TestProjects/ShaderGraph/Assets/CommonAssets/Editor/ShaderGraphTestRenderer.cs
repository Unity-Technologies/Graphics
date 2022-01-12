using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using NUnit.Framework;

public class ShaderGraphTestRenderer
{
    internal const int defaultResolution = 128;

    internal PreviewSceneResources previewScene = new PreviewSceneResources();

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

    internal GraphData LoadGraph(string graphPath)
    {
        List<PropertyCollector.TextureInfo> lti;
        var assetCollection = new AssetCollection();
        ShaderGraphImporter.GetShaderText(graphPath, out lti, assetCollection, out var graph);
        Assert.NotNull(graph, $"Invalid graph data found for {graphPath}");
        graph.OnEnable();
        graph.ValidateGraph();
        return graph;
    }

    // we apply a transform to the test setup, so that the transform matrices are non-trivial
    internal Vector3 testPosition = new Vector3(0.24699998f, 0.51900005f, 0.328999996f);
    internal Quaternion testRotation = new Quaternion(-0.164710045f, -0.0826543793f, -0.220811233f, 0.957748055f);

    internal int RunNodeTest(GraphData graph, string filePrefix, SetupMaterialDelegate setupMaterial = null, Color32? expectedColor = null, int expectedIncorrectPixels = 0, int errorThreshold = 0)
    {
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(defaultResolution, defaultResolution, GraphicsFormat.R8G8B8A8_SRGB, depthBufferBits: 32);
        var target = RenderTexture.GetTemporary(descriptor);

        // use a non-standard transform, so that view, object, etc. transforms are non trivial
        RenderQuadPreview(graph, target, testPosition, testRotation, setupMaterial, Mode.DIFF, useSRP: true);

        // default expected color is green (test shaders should be set up to return green on success)
        int incorrectPixels = CountPixelsNotEqual(target, expectedColor ?? new Color32(0, 255, 0, 255), false, errorThreshold);

        if (incorrectPixels != expectedIncorrectPixels)
        {
            // report images
            SaveToPNG(target, $"test-results/NodeTests/{filePrefix}.diff.png");

            RenderQuadPreview(graph, target, testPosition, testRotation, setupMaterial, Mode.EXPECTED, useSRP: true);
            SaveToPNG(target, $"test-results/NodeTests/{filePrefix}.expected.png");

            RenderQuadPreview(graph, target, testPosition, testRotation, setupMaterial, Mode.ACTUAL, useSRP: true);
            SaveToPNG(target, $"test-results/NodeTests/{filePrefix}.png");

            // record failure
            wrongImageCount++;
            int wrongPixels = Math.Abs(incorrectPixels - expectedIncorrectPixels);
            if (wrongPixels > mostWrongPixels)
            {
                mostWrongPixels = wrongPixels;
                mostWrongString = $"{filePrefix} incorrect pixels expected: {expectedIncorrectPixels} actual: {incorrectPixels}";
            }
        }

        RenderTexture.ReleaseTemporary(target);
        return incorrectPixels;
    }

    internal static Shader BuildShaderGraph(GraphData graph, string name, bool hide = true)
    {
        var generator = new Generator(graph, graph.outputNode, GenerationMode.ForReals, "TransformGraph", null);
        string shaderString = generator.generatedShader;

        var shader = ShaderUtil.CreateShaderAsset(shaderString, false);
        if (hide)
            shader.hideFlags = HideFlags.HideAndDontSave;

        return shader;
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

    internal static void CreateDirectoriesForFilePath(string systemFilePath)
    {
        var dirPath = Path.GetDirectoryName(systemFilePath);
        CreateDirectories(dirPath);
    }

    internal static void CreateDirectories(string systemDirectoryPath)
    {
        Directory.CreateDirectory(systemDirectoryPath);
    }

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
            ShaderGraphTestRenderer.ReportArtifact(path);
    }

    internal static int CountPixelsNotEqual(RenderTexture target, Color32 value, bool compareAlpha, int errorThreshold = 0)
    {
        Texture2D temp = new Texture2D(target.width, target.height, TextureFormat.RGBA32, mipChain: false, linear: false);

        var previousRenderTexture = RenderTexture.active;
        RenderTexture.active = target;
        temp.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        RenderTexture.active = previousRenderTexture;

        int mismatchCount = 0;
        var pixels = temp.GetPixels32(0);
        foreach (var pixel in pixels)
        {
            if ((Math.Abs(pixel.r - value.r) > errorThreshold) ||
                (Math.Abs(pixel.g - value.g) > errorThreshold) ||
                (Math.Abs(pixel.b - value.b) > errorThreshold) ||
                (compareAlpha && (Math.Abs(pixel.a - value.a) > errorThreshold)))
            {
                mismatchCount++;
            }
        }

        UnityEngine.Object.DestroyImmediate(temp);
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

    internal void RenderQuadPreview(GraphData graph, RenderTexture target, Vector3 scenePosition, Quaternion sceneRotation, SetupMaterialDelegate setupMaterial = null, Mode mode = Mode.DIFF, bool useSRP = false)
    {
        var camXform = previewScene.camera.transform;

        // setup 2D quad render
        camXform.position = -Vector3.forward * 2 + scenePosition;
        camXform.rotation = sceneRotation;
        previewScene.camera.orthographicSize = 0.5f;
        previewScene.camera.orthographic = true;

        graph.ValidateGraph();

        // build the shader
        var shader = BuildShaderGraph(graph, "Test Shader");
        var mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

        SetKeyword(mat, "_MODE_DIFF", (mode == Mode.DIFF));
        SetKeyword(mat, "_MODE_EXPECTED", (mode == Mode.EXPECTED));
        SetKeyword(mat, "_MODE_ACTUAL", (mode == Mode.ACTUAL));

        if (setupMaterial != null)
            setupMaterial(mat);

        var quadMatrix = Matrix4x4.TRS(camXform.position + camXform.forward * 2, camXform.rotation, Vector3.one);

        // render with it
        RenderMeshWithMaterial(previewScene.camera, previewScene.quad, quadMatrix, mat, target, useSRP);
    }
}
