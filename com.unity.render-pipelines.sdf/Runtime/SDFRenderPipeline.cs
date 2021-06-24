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

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            ClearBackground(context, cameras);

            SDFRenderer[] SDFObjects = GameObject.FindObjectsOfType<SDFRenderer>();
            if (SDFObjects.Length > 0)
            {
                GetDataFromSceneGraph(SDFObjects, out ComputeBuffer SDFData, out ComputeBuffer SDFHeaderData);

                // Vector3[] sampleExtents = {
                //     new Vector3(1.0f, 1.0f, 1.0f),
                //     new Vector3(3.0f, 5.0f, 1.0f),
                //     new Vector3(4.0f, 2.0f, 2.0f),
                // };
                // Matrix4x4[] sampleTransforms = {
                //     Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, Vector3.one),
                //     Matrix4x4.TRS(new Vector3(3, 3, 3), Quaternion.identity, Vector3.one),
                //     Matrix4x4.TRS(new Vector3(-4, 0, 0), Quaternion.identity, Vector3.one)
                // };
                CreateObjectList(context, cameras, SDFHeaderData, SDFObjects.Length);

                // ScriptableCullingParameters scp;
                // cameras[0].TryGetCullingParameters(out scp);
                // CullingResults cullResults = context.Cull(ref scp);
                // DrawRendererSettings drawRenderSettings = new DrawRendererSettings();
                // context.DrawRenderers(cullResults.visibleRenderers);
                // context.DrawSkybox(cameras[0]);

                // SDF Rendering
                {
                    foreach (Camera camera in cameras)
                    {
                        if (camera.cameraType == CameraType.Game && camera.enabled)
                        {
                            CommandBuffer cmdRayMarch = new CommandBuffer();
                            cmdRayMarch.name = "RayMarch";
                            cmdRayMarch.SetViewport(camera.pixelRect);
                            cmdRayMarch.SetViewMatrix(camera.worldToCameraMatrix);
                            cmdRayMarch.SetProjectionMatrix(camera.projectionMatrix);
                            SDFRayMarch.RayMarchForRealsies(cmdRayMarch, currentAsset.rayMarchingCS, camera.pixelRect, SDFData, SDFHeaderData, camera);
                            context.ExecuteCommandBuffer(cmdRayMarch);
                            cmdRayMarch.Release();
                        }
                    }

                    SDFHeaderData.Release();
                    SDFData.Release();
                }
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

        private void ClearBackground(ScriptableRenderContext context, Camera[] cameras)
        {
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
                context.ExecuteCommandBuffer(cmd1);
                cmd1.Release();
            }
        }

        private void GetDataFromSceneGraph(SDFRenderer[] SDFObjects, out ComputeBuffer SDFData, out ComputeBuffer SDFHeaderData)
        {
            // First, get size
            int dataSize = 0;
            foreach (SDFRenderer renderer in SDFObjects)
            {
                dataSize += renderer.SDFFilter.VoxelField.m_Field.Length;
            }

            // Next, fill out array of data and array of data-headers
            float[] nativeData = new float[dataSize];
            SDFRayMarch.ObjectHeader[] nativeHeaders = new SDFRayMarch.ObjectHeader[SDFObjects.Length];

            int offset = 0;
            for(int i = 0; i < SDFObjects.Length; i++)
            {
                VoxelField field = SDFObjects[i].SDFFilter.VoxelField;
                SDFRayMarch.ObjectHeader header = new SDFRayMarch.ObjectHeader();
                header.worldToObjMatrix = SDFObjects[i].gameObject.transform.worldToLocalMatrix; // may not work with shader according to docs?
                header.objID = i; // index into data. Change later?
                header.numEntries = field.m_Field.Length;
                header.startOffset = offset;
                header.voxelSize = field.m_VoxelSize;
                Vector3 minExtent = field.MeshBounds.center - 0.5f * field.MeshBounds.size; // is this correct? Can we just pass the counts instead?
                header.minExtentX = minExtent.x;
                header.minExtentY = minExtent.y;
                header.minExtentZ = minExtent.z;
                Vector3 maxExtent = field.MeshBounds.center + 0.5f * field.MeshBounds.size;
                header.maxExtentX = maxExtent.x;
                header.maxExtentY = maxExtent.y;
                header.maxExtentZ = maxExtent.z;
                //header.color = SDFObjects[i].SDFMaterial.color;
                nativeHeaders[i] = header;

                Array.Copy(field.m_Field, 0, nativeData, offset, field.m_Field.Length);
                offset += field.m_Field.Length;
            }

            SDFHeaderData = new ComputeBuffer(SDFObjects.Length, /*UnsafeUtility.SizeOf<ObjectHeader>()*/SDFRayMarch.ObjectHeaderDataSize, ComputeBufferType.Default);
            SDFHeaderData.SetData(nativeHeaders);
            SDFData = new ComputeBuffer(dataSize, sizeof(float), ComputeBufferType.Default);
            SDFData.SetData(nativeData);
        }

        private void CreateObjectList(ScriptableRenderContext context, Camera[] cameras, ComputeBuffer SDFHeaders, int totalSDFs)
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
                // cmd1.ClearRenderTarget(false, true, currentAsset.clearColor);
                cmd1.SetViewMatrix(camera.worldToCameraMatrix);
                cmd1.SetProjectionMatrix(camera.projectionMatrix);

                SDFRayMarch.ObjectHeader[] data = new SDFRayMarch.ObjectHeader[totalSDFs];  // get correct size of array
                SDFHeaders.GetData(data);     // optimize this, maybe don't create a compute buffer yet
                for (int i = 0; i < data.Length; i++)
                {
                    Vector3 minExtent = new Vector3(data[i].minExtentX, data[i].minExtentY, data[i].minExtentZ);
                    Vector3 maxExtent = new Vector3(data[i].maxExtentX, data[i].maxExtentY, data[i].maxExtentZ);
                    var extents = maxExtent - minExtent;
                    Matrix4x4 scale = Matrix4x4.Scale(extents);
                    Matrix4x4 finalTRS = data[i].worldToObjMatrix.inverse * scale;       // Check multiply scale correctness later

                    cmd1.DrawMesh(mesh, finalTRS, material, 0, 0);
                }
                context.ExecuteCommandBuffer(cmd1);
                cmd1.Release();
            }
        }
    }
}
