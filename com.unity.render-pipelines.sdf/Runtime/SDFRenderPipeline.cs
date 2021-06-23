using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.SDFRP
{
    public static class Utilities
    {
        static Mesh s_FullscreenMesh = null;
        /// <summary>
        /// Returns a mesh that you can use with <see cref="CommandBuffer.DrawMesh(Mesh, Matrix4x4, Material)"/> to render full-screen effects.
        /// </summary>
        public static Mesh fullscreenMesh
        {
            get
            {
                if (s_FullscreenMesh != null)
                    return s_FullscreenMesh;

                float topV = 1.0f;
                float bottomV = 0.0f;

                s_FullscreenMesh = new Mesh { name = "Fullscreen Quad" };
                s_FullscreenMesh.SetVertices(new List<Vector3>
                {
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(-1.0f,  1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f,  1.0f, 0.0f)
                });

                s_FullscreenMesh.SetUVs(0, new List<Vector2>
                {
                    new Vector2(0.0f, bottomV),
                    new Vector2(0.0f, topV),
                    new Vector2(1.0f, bottomV),
                    new Vector2(1.0f, topV)
                });

                s_FullscreenMesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, false);
                s_FullscreenMesh.UploadMeshData(true);
                return s_FullscreenMesh;
            }
        }
    }

    [ExecuteInEditMode]
    public class SDFRenderPipeline : RenderPipeline
    {
        internal static SDFRenderPipelineAsset currentAsset
                => GraphicsSettings.currentRenderPipeline is SDFRenderPipelineAsset sdfAsset ? sdfAsset : null;

    //    internal static HDRenderPipeline currentPipeline
    //            => RenderPipelineManager.currentPipeline is HDRenderPipeline hdrp ? hdrp : null;

        Material m_DepthOfFieldMaterial = null;

        static int Frame = 0;

        struct ObjectHeader
        {
            Matrix4x4 worldToObjMatrix;
            int      objID;
            int      numEntries;
            int      startOffset;
            float    voxelSize;
            Vector3  minExtent;
            float    pad0;
            Vector3  maxExtent;
            float    pad1;
            Vector4  color;
        };

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            // SDFRenderer[] SDFObjects = GameObject.FindObjectsOfType<SDFRenderer>();
            // ComputeBuffer SDFHeaderData = new ComputeBuffer(SDFObjects.Length, UnsafeUtility.SizeOf<ObjectHeader>(), ComputeBufferType.Default);
            // ComputeBuffer SDFData = GetDataFromSceneGraph(SDFObjects, SDFHeaderData);

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
            CreateObjectList(context, cameras, sampleExtents, sampleTransforms);

            // ScriptableCullingParameters scp;
            // cameras[0].TryGetCullingParameters(out scp);
            // CullingResults cullResults = context.Cull(ref scp);
            // DrawRendererSettings drawRenderSettings = new DrawRendererSettings();
            // context.DrawRenderers(cullResults.visibleRenderers);
            // context.DrawSkybox(cameras[0]);

            // SDF Rendering
            {
                CommandBuffer cmdRayMarch = new CommandBuffer();
                cmdRayMarch.name = "RayMarch";
                SDFRayMarch.RayMarch(cmdRayMarch, currentAsset.rayMarchingCS);
                context.ExecuteCommandBuffer(cmdRayMarch);
                cmdRayMarch.Release();
            }

            if (currentAsset.EnableDepthOfField)
            {
                if (m_DepthOfFieldMaterial == null)
                {
                    m_DepthOfFieldMaterial = new Material(Shader.Find("Hidden/SDFRP/DepthOfField"));
                }
                 foreach (Camera camera in cameras)
                 {
                    if (camera.cameraType == CameraType.Game && camera.enabled)
                    {
                        // // Double check cmdbuffer setup
                        // CommandBuffer cmd = new CommandBuffer();
                        // cmd.name = "SDF";
                        // cmd.SetViewport(camera.pixelRect);
                        // cmd.SetViewMatrix(camera.worldToCameraMatrix);
                        // cmd.SetProjectionMatrix(camera.projectionMatrix);

                        // ComputeShader cs = (ComputeShader)Resources.Load("NameOfShader");
                        // int kernelHandle = cs.FindKernel("CSMain");
                        // cs.SetBuffer(kernelHandle, "_ObjectSDFData", SDFData);
                        // cmd.SetRandomWriteTarget(1, SDFData);
                        // cs.SetBuffer(kernelHandle, "_ObjectHeaderData", SDFHeaderData);
                        // cmd.SetRandomWriteTarget(2, SDFHeaderData);

                        // RenderTexture tex = new RenderTexture(cameras[0].pixelWidth, cameras[0].pixelHeight, 0);  // Check depth buffer size
                        // tex.enableRandomWrite = true;
                        // tex.Create();
                        // cs.SetTexture(kernelHandle, "Result", tex);
                        // // cmd.SetRandomWriteTarget(3, renderTexture);

                        // cs.Dispatch(kernelHandle, (int)Math.Ceiling(tex.width / 8.0f), (int)Math.Ceiling(tex.height / 8.0f), 1);     // 8 x 8 tiles
                        // // cmd.Blit(tex, null);
                        // cmd.Blit(tex, BuiltinRenderTextureType.CameraTarget);

                        // cmd.ClearRandomWriteTargets();

                        CommandBuffer cmd1 = new CommandBuffer();
                        cmd1.name = "DepthOfField";
                        cmd1.SetViewport(camera.pixelRect);
                        cmd1.SetRenderTarget(camera.targetTexture);
                        cmd1.SetViewMatrix(camera.worldToCameraMatrix);
                        cmd1.SetProjectionMatrix(camera.projectionMatrix);
                        cmd1.SetGlobalColor("BackgroundColor", currentAsset.clearColor);
                        cmd1.SetGlobalVector("iResolution", new Vector4(camera.scaledPixelWidth, camera.scaledPixelHeight, camera.pixelRect.x, camera.pixelRect.y));
                        cmd1.SetGlobalInt("lensRes", currentAsset.lensRes);
                        cmd1.SetGlobalFloat("lensDis", camera.nearClipPlane);
                        cmd1.SetGlobalFloat("lensSiz", currentAsset.lensSiz);
                        cmd1.SetGlobalFloat("focalDis", currentAsset.focalDis);
                        cmd1.DrawMesh(Utilities.fullscreenMesh, Matrix4x4.identity, m_DepthOfFieldMaterial);
                        context.ExecuteCommandBuffer(cmd1);
                        cmd1.Release();
                    }
                }
            }
            context.Submit();
            Frame++;
        }


        // private ComputeBuffer GetDataFromSceneGraph(SDFRenderer[] SDFObjects, ComputeBuffer headers)
        // {
        //     // First, get size
        //     int dataSize = 0;
        //     foreach (SDFRenderer renderer in SDFObjects)
        //     {
        //         SDFFilter filter = renderer.gameObject.GetComponent<SDFFilter>();
        //         dataSize += filter.size;
        //     }

        //     // Next, fill out array of data and array of data-headers
        //     float[] nativeData = new float[dataSize];
        //     ObjectHeader[] nativeHeaders = new ObjectHeader[SDFObjects.Length];

        //     int offset = 0;
        //     for(int i = 0; i < SDFObjects.Length; i++)
        //     {
        //         SDFFilter filter = SDFObjects[i].gameObject.GetComponent<SDFFilter>();
        //         ObjectHeader header = new ObjectHeader();
        //         header.worldToObjMatrix = SDFObjects[i].worldToLocalMatrix;
        //         header.objID = i; // index into data. Change later?
        //         header.numEntries = dataSize;
        //         header.startOffset = offset;
        //         header.voxelSize = filter.voxelSize;
        //         header.minExtent = filter.minExtent;
        //         header.maxExtent = filter.maxExtent;
        //         header.color = SDFObjects[i].material.color;
        //         nativeHeaders[i] = header;

        //         nativeData[offset] = filter.data;
        //         offset += filter.size;
        //     }

        //     headers.SetData(nativeHeaders);
        //     ComputeBuffer SDFData = new ComputeBuffer(dataSize, sizeof(float), ComputeBufferType.Default);
        //     SDFData.SetData(nativeData);
        //     return SDFData;
        // }

        private void CreateObjectList(ScriptableRenderContext context, Camera[] cameras, Vector3[] extents, Matrix4x4[] transforms)
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
                CommandBuffer cmd1 = new CommandBuffer();
                if (camera.cameraType == CameraType.Preview)
                {
                    Debug.LogError(camera.pixelRect);
                }
                cmd1.SetViewport(camera.pixelRect);

                cmd1.SetRenderTarget(camera.targetTexture);
                cmd1.ClearRenderTarget(false, true, currentAsset.clearColor);
                cmd1.SetViewMatrix(camera.worldToCameraMatrix);
                cmd1.SetProjectionMatrix(camera.projectionMatrix);

                for (int i = 0; i < extents.Length; i++)
                {
                    Matrix4x4 scale = Matrix4x4.Scale(extents[i]);
                    Matrix4x4 finalTRS = transforms[i] * scale;       // Check multiply scale correctness later

                    cmd1.DrawMesh(mesh, finalTRS, material, 0, 0);
                }
                context.ExecuteCommandBuffer(cmd1);
                cmd1.Release();
            }
        }
    }
}
