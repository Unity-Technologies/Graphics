using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using RttTest;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.TCPTransmissionDatagrams;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        public enum DistributedMode
        {
            None,
            Renderer,
            Merger
        }

        private static DistributedMode s_distributedMode = DistributedMode.None;

        public static void SetDistributedMode(DistributedMode mode)
        {
            s_distributedMode = Application.isEditor ? DistributedMode.None : mode;
        }

        public static DistributedMode GetDistributedMode()
        {
            //Camera camera = hdCamera.camera;
            if (Application.isEditor)
            {
                // In editor, we only do non-distributed mode
                return DistributedMode.None;
            }

            return s_distributedMode;
        }

        private static int FrameID = -1;

        public static int CurrentFrameID
        {
            get => FrameID;
            set => FrameID = value;
        }

        class ReceiveData
        {
            public TextureHandle whiteTexture;

            public ComputeBufferHandle receivedYUVDataBuffer;

            public ComputeShader computeBufferToYUVTexturesCS;

            public TextureHandle tempYTexture;
            public TextureHandle tempUTexture;
            public TextureHandle tempVTexture;

            public Material blitYUVToRGBMaterial;

            public TextureHandle colorBufferSection;

            public TextureHandle colorBuffer;

            public int userCount;
            public Vector2Int layout;
        }

        class SendData
        {
            public TextureHandle colorBuffer;

            public Material blitRGBToYUVMaterial;

            public TextureHandle tempYTexture;
            public TextureHandle tempUTexture;
            public TextureHandle tempVTexture;

            public ComputeShader yuvToComputeBufferCS;

            public ComputeBufferHandle computeBuffer;
        }

        void CreateYUVTexture(RenderGraphBuilder builder, Vector2Int layout,
            out TextureHandle yTex,
            out TextureHandle uTex,
            out TextureHandle vTex)
        {
            if (GetDistributedMode() == DistributedMode.Renderer)
                layout = Vector2Int.one;

            TextureDesc GetTextureDesc(Vector2 scale)
            {
                return new TextureDesc(scale, false, false)
                {
                    autoGenerateMips = false,
                    bindTextureMS = false,
                    clearBuffer = false,
                    anisoLevel = 0,
                    colorFormat = GraphicsFormat.R16_SFloat,
                    depthBufferBits = DepthBits.None,
                    dimension = TextureXR.dimension,
                    slices = TextureXR.slices,
                    filterMode = FilterMode.Trilinear,
                    enableMSAA = false,
                    enableRandomWrite = true
                };
            }

            Vector2 fullSize = Vector2.one / layout;
            Vector2 halfSize = fullSize * 0.5f;

            yTex = builder.CreateTransientTexture(GetTextureDesc(fullSize));
            uTex = builder.CreateTransientTexture(GetTextureDesc(halfSize));
            vTex = builder.CreateTransientTexture(GetTextureDesc(halfSize));
        }

        int GetYUVBufferLength(Vector2Int layout)
        {
            Vector2Int sectionSize = new Vector2Int(Screen.width / layout.x, Screen.height / layout.y);
            int yChannelSize = sectionSize.x * sectionSize.y;
            return 3 * yChannelSize * TextureXR.slices;
        }

        void CreateYUVComputeBuffer(RenderGraphBuilder builder, Vector2Int layout, out ComputeBufferHandle buffer)
        {
            buffer = builder.CreateTransientComputeBuffer(
                new ComputeBufferDesc(GetYUVBufferLength(layout) / 2, 4, ComputeBufferType.Default));
        }

        void ReceiveColorBuffer(RenderGraph renderGraph, TextureHandle colorBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<ReceiveData>("Receive Color Buffer", out var passData))
            {
                passData.userCount = Const.userCount;

                passData.layout = GetViewportLayout(passData.userCount);

                passData.whiteTexture = builder.ReadTexture(renderGraph.defaultResources.whiteTexture);

                CreateYUVComputeBuffer(builder, passData.layout, out passData.receivedYUVDataBuffer);

                passData.computeBufferToYUVTexturesCS = defaultResources.shaders.convertBufferYUVCS;

                CreateYUVTexture(builder, passData.layout,
                    out passData.tempYTexture, out passData.tempUTexture, out passData.tempVTexture);

                passData.blitYUVToRGBMaterial = GetBlitYUVToRGBMaterial(TextureXR.dimension);

                passData.colorBufferSection = builder.CreateTransientTexture(
                    new TextureDesc(Vector2.one / passData.layout, false, false)
                    {
                        anisoLevel = 0,
                        autoGenerateMips = false,
                        bindTextureMS = false,
                        clearBuffer = false,
                        colorFormat = GraphicsFormat.R16G16B16A16_SFloat,
                        depthBufferBits = DepthBits.None,
                        dimension = TextureXR.dimension,
                        slices = TextureXR.slices,
                        enableMSAA = false
                    });

                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);

                builder.SetRenderFunc(
                    (ReceiveData data, RenderGraphContext context) =>
                    {
                        bool hasConnection = false;
                        for (int i = 0; i < data.userCount; i++)
                        {
                            if (!SocketServer.Instance.Connected(i))
                                continue;
                            hasConnection = true;

                            Rect subsection = GetViewportSubsection(data.layout, i, data.userCount);

                            // Set received data to compute buffer
                            using (new ProfilingScope(context.cmd, new ProfilingSampler($"Load Data {i}")))
                            {
                                Datagram datagram = null;
                                datagram = SocketServer.Instance.ReceiveReadyFrame(i);
                                if (datagram == null)
                                    continue;
                                
                                RttTestUtilities.ReceiveFrame(RttTestUtilities.Role.Merger, (uint)CurrentFrameID, i);
                                context.cmd.SetComputeBufferData(data.receivedYUVDataBuffer, datagram.data,
                                    0, 0, datagram.length);
                                SocketServer.Instance.AddReceiveRingBuffer(i, Datagram.DatagramType.VideoFrame,
                                    in datagram);
                            }

                            // Use compute shader to move the data to YUV textures
                            using (new ProfilingScope(context.cmd,
                                       new ProfilingSampler($"Copy Buffer {i} to YUV Textures")))
                            {
                                RttTestUtilities.BeginDecodeYuv(RttTestUtilities.Role.Merger, (uint)CurrentFrameID, i);
                                ComputeShader cs = data.computeBufferToYUVTexturesCS;
                                int kernelID = cs.FindKernel("BufferToTexture");

                                int width = Screen.width / data.layout.x;
                                int height = Screen.height / data.layout.y;

                                context.cmd.SetComputeBufferParam(cs, kernelID, HDShaderIDs._YUVBufferID,
                                    data.receivedYUVDataBuffer);
                                context.cmd.SetComputeTextureParam(cs, kernelID, HDShaderIDs._YUVYTexID,
                                    data.tempYTexture);
                                context.cmd.SetComputeTextureParam(cs, kernelID, HDShaderIDs._YUVUTexID,
                                    data.tempUTexture);
                                context.cmd.SetComputeTextureParam(cs, kernelID, HDShaderIDs._YUVVTexID,
                                    data.tempVTexture);
                                context.cmd.SetComputeIntParam(cs, HDShaderIDs._YUVFullWidthID, width);
                                context.cmd.SetComputeIntParam(cs, HDShaderIDs._YUVFullHeightID, height);
                                int threadGroupZ;
                                if (TextureXR.dimension == TextureDimension.Tex2DArray)
                                {
                                    cs.EnableKeyword("USE_TEXTURE_ARRAY");
                                    threadGroupZ = TextureXR.slices;
                                }
                                else
                                {
                                    cs.DisableKeyword("USE_TEXTURE_ARRAY");
                                    threadGroupZ = 1;
                                }

                                cs.GetKernelThreadGroupSizes(kernelID, out var threadGroupSizeX,
                                    out var threadGroupSizeY, out _);

                                int threadGroupX = Mathf.CeilToInt((float)width / threadGroupSizeX);
                                int threadGroupY = Mathf.CeilToInt((float)height / threadGroupSizeY);

                                context.cmd.DispatchCompute(cs, kernelID, threadGroupX, threadGroupY, threadGroupZ);
                            }

                            // Blit YUV textures to a RGB temp texture
                            using (new ProfilingScope(context.cmd,
                                       new ProfilingSampler($"Blit YUV Textures {i} to RGB Section")))
                            {
                                var mpbYUVToRGB = context.renderGraphPool.GetTempMaterialPropertyBlock();
                                mpbYUVToRGB.SetTexture(HDShaderIDs._BlitTextureY, data.tempYTexture);
                                mpbYUVToRGB.SetTexture(HDShaderIDs._BlitTextureU, data.tempUTexture);
                                mpbYUVToRGB.SetTexture(HDShaderIDs._BlitTextureV, data.tempVTexture);

                                context.cmd.SetRenderTarget(data.colorBufferSection);
                                CoreUtils.DrawFullScreen(context.cmd, data.blitYUVToRGBMaterial, mpbYUVToRGB);
                            }

                            // Blit the temp texture to part of the color buffer
                            using (new ProfilingScope(context.cmd,
                                       new ProfilingSampler($"Copy Section {i} to Color Buffer")))
                            {
                                RttTestUtilities.CombineFrame(RttTestUtilities.Role.Merger, (uint)CurrentFrameID, i);
                                
                                context.cmd.SetRenderTarget(data.colorBuffer);
                                HDUtils.BlitQuadWithPadding(
                                    context.cmd,
                                    data.colorBufferSection,
                                    new Vector2(960, 540),
                                    new Vector4(1, 1, 0, 0),
                                    new Vector4(1.0f / data.layout.x, 1.0f / data.layout.y, subsection.xMin,
                                        subsection.yMin),
                                    0,
                                    false,
                                    0);
                            }
                        }

                        if (!hasConnection)
                        {
                            HDUtils.BlitCameraTexture(context.cmd, data.whiteTexture, data.colorBuffer);
                        }
                    }
                );
            }
        }

        void SendColorBuffer(RenderGraph renderGraph, TextureHandle colorBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<SendData>("Send Color Buffer", out var passData))
            {
                passData.colorBuffer = builder.ReadTexture(colorBuffer);

                passData.blitRGBToYUVMaterial = GetBlitRGBToYUVMaterial(TextureXR.dimension);

                CreateYUVTexture(builder, Vector2Int.one,
                    out passData.tempYTexture, out passData.tempUTexture, out passData.tempVTexture);

                passData.yuvToComputeBufferCS = defaultResources.shaders.convertBufferYUVCS;

                CreateYUVComputeBuffer(builder, Vector2Int.one, out passData.computeBuffer);

                builder.SetRenderFunc(
                    (SendData data, RenderGraphContext context) =>
                    {
                        int currentFrameID = CurrentFrameID;
                        int rendererId = Const.userID;
                        
                        RttTestUtilities.BeginEncodeYuv(RttTestUtilities.Role.Renderer, (uint)currentFrameID, rendererId);
                        
                        // Blit color buffer to YUV Textures
                        using (new ProfilingScope(context.cmd,
                                   new ProfilingSampler("Blit Color Buffer to YUV Textures")))
                        {
                            var mpbRGBToYUV = context.renderGraphPool.GetTempMaterialPropertyBlock();
                            mpbRGBToYUV.SetTexture(HDShaderIDs._BlitTexture, data.colorBuffer);

                            context.cmd.SetRenderTarget(data.tempYTexture);
                            CoreUtils.DrawFullScreen(context.cmd, data.blitRGBToYUVMaterial, mpbRGBToYUV, 0);

                            context.cmd.SetRenderTarget(data.tempUTexture);
                            CoreUtils.DrawFullScreen(context.cmd, data.blitRGBToYUVMaterial, mpbRGBToYUV, 1);

                            context.cmd.SetRenderTarget(data.tempVTexture);
                            CoreUtils.DrawFullScreen(context.cmd, data.blitRGBToYUVMaterial, mpbRGBToYUV, 2);
                        }

                        // Convert YUV Textures to compute buffer
                        using (new ProfilingScope(context.cmd,
                                   new ProfilingSampler("Convert YUV Textures to Compute Buffer")))
                        {
                            ComputeShader cs = data.yuvToComputeBufferCS;
                            int kernelID = cs.FindKernel("TextureToBuffer");
                            int threadGroupZ;
                            if (TextureXR.dimension == TextureDimension.Tex2DArray)
                            {
                                cs.EnableKeyword("USE_TEXTURE_ARRAY");
                                threadGroupZ = TextureXR.slices;
                            }
                            else
                            {
                                cs.DisableKeyword("USE_TEXTURE_ARRAY");
                                threadGroupZ = 1;
                            }

                            int width = Screen.width;
                            int height = Screen.height;

                            context.cmd.SetComputeBufferParam(cs, kernelID, HDShaderIDs._YUVBufferID,
                                data.computeBuffer);
                            context.cmd.SetComputeTextureParam(cs, kernelID, HDShaderIDs._YUVYTexID, data.tempYTexture);
                            context.cmd.SetComputeTextureParam(cs, kernelID, HDShaderIDs._YUVUTexID, data.tempUTexture);
                            context.cmd.SetComputeTextureParam(cs, kernelID, HDShaderIDs._YUVVTexID, data.tempVTexture);
                            context.cmd.SetComputeIntParam(cs, HDShaderIDs._YUVFullWidthID, width);
                            context.cmd.SetComputeIntParam(cs, HDShaderIDs._YUVFullHeightID, height);

                            cs.GetKernelThreadGroupSizes(kernelID, out var threadGroupSizeX, out var threadGroupSizeY,
                                out _);

                            int threadGroupX = Mathf.CeilToInt((float)width / threadGroupSizeX);
                            int threadGroupY = Mathf.CeilToInt((float)height / threadGroupSizeY);

                            context.cmd.DispatchCompute(cs, kernelID, threadGroupX, threadGroupY, threadGroupZ);
                        }

                        // Readback compute buffer and send the data
                        using (new ProfilingScope(context.cmd,
                                   new ProfilingSampler("Readback and Send Buffer")))
                        {
                            RttTestUtilities.BeginReadBack(RttTestUtilities.Role.Renderer, (uint)currentFrameID, rendererId);
                            
                            //int currentFrameID = CurrentFrameID;
                            context.cmd.RequestAsyncReadback(data.computeBuffer, request =>
                            {
                                while (!request.done)
                                {
                                }

                                Profiling.Profiler.BeginSample("Readback and Send Buffer Internal");
                                
                                RttTestUtilities.FinishReadBack(RttTestUtilities.Role.Renderer, (uint)currentFrameID, rendererId);

                                NativeArray<byte> nativeData = request.GetData<byte>();
                                byte[] managedData = nativeData.ToArray();
                                
                                RttTestUtilities.SendFrame(RttTestUtilities.Role.Renderer, (uint)currentFrameID, rendererId);
                                //SocketClient.Instance.ReplaceOrSet(Datagram.DatagramType.VideoFrame, managedData);
                                SocketClient.Instance.Set(Datagram.DatagramType.VideoFrame, managedData, currentFrameID);

                                Profiling.Profiler.EndSample();
                            });
                        }
                    }
                );
            }
        }

        Material GetBlitYUVToRGBMaterial(TextureDimension dimension, bool singleSlice = false)
        {
            return dimension == TextureDimension.Tex2DArray
                ? (singleSlice ? m_BlitYUVToRGBTexArraySingleSlice : m_BlitYUVToRGBTexArray)
                : m_BlitYUVToRGB;
        }

        Material GetBlitRGBToYUVMaterial(TextureDimension dimension, bool singleSlice = false)
        {
            return dimension == TextureDimension.Tex2DArray
                ? (singleSlice ? m_BlitRGBToYUVTexArraySingleSlice : m_BlitRGBToYUVTexArray)
                : m_BlitRGBToYUV;
        }

        #region Camera

        public static Vector2Int GetViewportLayout(int count)
        {
            float sqrtCount = Mathf.Sqrt(count);
            int floorSqrtCount = Mathf.FloorToInt(sqrtCount);
            int rows = floorSqrtCount;
            int columns = floorSqrtCount;
            while (count % rows != 0)
            {
                --rows;
            }

            columns = count / rows;

            return new Vector2Int(columns, rows);
        }

        private static Rect GetViewportSubsection(Vector2Int layout, int index, int count)
        {
            int xIndex = index % layout.x;
            int yIndex = (count - index - 1) / layout.x;
            float width = 1.0f / layout.x;
            float height = 1.0f / layout.y;
            Rect result = new Rect(xIndex * width, yIndex * height, width, height);
            return result;
        }

        private static Matrix4x4 GetFrustumSlicingAsymmetricProjection(Matrix4x4 originalProjection,
            Rect viewportSubsection)
        {
            var baseFrustumPlanes = originalProjection.decomposeProjection;
            var frustumPlanes = new FrustumPlanes();
            frustumPlanes.zNear = baseFrustumPlanes.zNear;
            frustumPlanes.zFar = baseFrustumPlanes.zFar;
            frustumPlanes.left =
                Mathf.LerpUnclamped(baseFrustumPlanes.left, baseFrustumPlanes.right, viewportSubsection.xMin);
            frustumPlanes.right =
                Mathf.LerpUnclamped(baseFrustumPlanes.left, baseFrustumPlanes.right, viewportSubsection.xMax);
            frustumPlanes.bottom =
                Mathf.LerpUnclamped(baseFrustumPlanes.bottom, baseFrustumPlanes.top, viewportSubsection.yMin);
            frustumPlanes.top =
                Mathf.LerpUnclamped(baseFrustumPlanes.bottom, baseFrustumPlanes.top, viewportSubsection.yMax);
            return Matrix4x4.Frustum(frustumPlanes);
        }

        #endregion
    }
}
