using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.CoreModule;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
[ExecuteInEditMode]
public class SDFRenderPipeline : RenderPipeline
{
    internal static SDFRenderPipelineAsset currentAsset
            => GraphicsSettings.currentRenderPipeline is SDFRenderPipelineAsset sdfAsset ? sdfAsset : null;

//    internal static HDRenderPipeline currentPipeline
//            => RenderPipelineManager.currentPipeline is HDRenderPipeline hdrp ? hdrp : null;

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "My SETUP";
        cmd.ClearRenderTarget(false, true, currentAsset.clearColor);
        cmd.SetViewport(cameras[0].pixelRect);

        Vector3[] sampleExtents = {
            new Vector3(1.0f, 1.0f, 1.0f),
            new Vector3(3.0f, 5.0f, 1.0f),
            new Vector3(4.0f, 2.0f, 2.0f),
        };
        Matrix4x4[] sampleTransforms = {
            Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, Vector3.one),
            Matrix4x4.TRS(new Vector3(3, 3, 3), Quaternion.identity, Vector3.one),
            Matrix4x4.TRS(new Vector3(-4, 0, 0), Quaternion.identity, Vector3.one)
        };
        CreateObjectList(cmd, cameras, sampleExtents, sampleTransforms);

        context.ExecuteCommandBuffer(cmd);
        cmd.Release();

        // ScriptableCullingParameters scp;
        // cameras[0].TryGetCullingParameters(out scp);
        // CullingResults cullResults = context.Cull(ref scp);
        // DrawRendererSettings drawRenderSettings = new DrawRendererSettings();
        // context.DrawRenderers(cullResults.visibleRenderers);
        // context.DrawSkybox(cameras[0]);
        context.Submit();
    }

    private void CreateObjectList(CommandBuffer cmd, Camera[] cameras, Vector3[] extents, Matrix4x4[] transforms)
    {
        Material material;

        Vector3[] cubeVertices =
        {
            new Vector3(-0.5f,-0.5f,-0.5f),
            new Vector3(-0.5f,-0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f,-0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f,-0.5f,-0.5f),
            new Vector3(0.5f,-0.5f, 0.5f),
            new Vector3(0.5f, 0.5f,-0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
        };
        int[] cubeIndices =
        {
            0, 1, 3,
            6, 0, 2,
            5, 0, 4,
            6, 4, 0,
            0, 3, 2,
            5, 1, 0,
            3, 1, 5,
            7, 4, 6,
            4, 7, 5,
            7, 6, 2,
            7, 2, 3,
            7, 3, 5
        };

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        material = new Material(shader);
        material.SetColor("_Color", Color.red);
        material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

        // var buffer = new ...
        // material.SetBuffer(...)
        // cmd.SetRandomWriteTarget(...)

        var mesh = new Mesh();
        mesh.SetVertices(cubeVertices, 0, 8);
        mesh.SetIndices(cubeIndices, MeshTopology.Triangles, 0);

        foreach (Camera camera in cameras)
        {
            cmd.SetViewMatrix(camera.worldToCameraMatrix);
            cmd.SetProjectionMatrix(camera.projectionMatrix);

            for (int i = 0; i < extents.Length; i++)
            {
                Matrix4x4 scale = Matrix4x4.Scale(extents[i]);
                Matrix4x4 finalTRS = transforms[i] * scale;       // Check multiply scale correctness later

                cmd.DrawMesh(mesh, finalTRS, material, 0, 0);
            }
        }
    }
}
