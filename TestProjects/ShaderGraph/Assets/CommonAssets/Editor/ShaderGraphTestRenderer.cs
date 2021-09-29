
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;

public class ShaderGraphTestRenderer
{
    PreviewSceneResources previewScene = new PreviewSceneResources();

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
        //var systemDirectory = Application.dataPath + "/../" + directoryPath;
        Directory.CreateDirectory(systemDirectoryPath);
        /*
        var dirs = directoryPath.Split('/');
        string curpath = string.Empty;
        foreach (var dir in dirs)
        {
            var parentPath = curpath;
            curpath = curpath + dir;
            if (!UnityEditor.AssetDatabase.IsValidFolder(curpath))
            {
                AssetDatabase.CreateFolder(parentPath, dir);
            }
            curpath = curpath + '/';
        }
        */
    }

    internal static void SaveToPNG(RenderTexture target, string path, bool createDirectory = true)
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
    }

    internal static int CountPixelsNotEqual(RenderTexture target, Color32 value, bool compareAlpha)
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
            if ((pixel.r != value.r) ||
                (pixel.g != value.g) ||
                (pixel.b != value.b) ||
                (compareAlpha && (pixel.a != value.a)))
            {
                mismatchCount++;
            }
        }

        UnityEngine.Object.DestroyImmediate(temp);
        return mismatchCount;
    }

    // scenePosition/Rotation is applied to both the camera and the quad.  Useful for testing position-sensitive behaviors
    internal void RenderQuadPreview(GraphData graph, RenderTexture target, bool useSRP = false)
    {
        RenderQuadPreview(graph, target, Vector3.zero, Quaternion.identity, useSRP);
    }

    internal enum Mode
    {
        COMPARE,
        EXPECTED,
        ACTUAL
    }

    void SetKeyword(Material mat, string keyword, bool enabled)
    {
        if (enabled)
            mat.EnableKeyword(keyword);
        else
            mat.DisableKeyword(keyword);
    }

    internal void RenderQuadPreview(GraphData graph, RenderTexture target, Vector3 scenePosition, Quaternion sceneRotation, bool useSRP = false, Mode mode = Mode.COMPARE)
    {
        var camXform = previewScene.camera.transform;

        // setup 2D quad render
        camXform.position = -Vector3.forward * 2 + scenePosition;
        camXform.rotation = sceneRotation;
        previewScene.camera.orthographicSize = 0.5f;
        previewScene.camera.orthographic = true;

        // EditorUtility.SetCameraAnimateMaterialsTime(sceneResources.camera, 7.3542f);

        graph.ValidateGraph();

        // build the shader
        var shader = BuildShaderGraph(graph, "Test Shader");
        var mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

        SetKeyword(mat, "_MODE_COMPARE", (mode == Mode.COMPARE));
        SetKeyword(mat, "_MODE_EXPECTED", (mode == Mode.EXPECTED));
        SetKeyword(mat, "_MODE_ACTUAL", (mode == Mode.ACTUAL));

        var quadMatrix = Matrix4x4.TRS(camXform.position + camXform.forward * 2, camXform.rotation, Vector3.one);

        // render with it
        RenderMeshWithMaterial(previewScene.camera, previewScene.quad, quadMatrix, mat, target, useSRP);
    }
}
