using System.Collections.Generic;
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
        private static bool s_videoMode = false;

        public static void SetDistributedMode(DistributedMode renderingMode, bool useVideoEncoding = false)
        {
#if UNITY_EDITOR
            // In editor, we only do non-distributed mode
            s_distributedMode = DistributedMode.None;
            // In editor, we don't do video encoding
            s_videoMode = false;
#else
            s_distributedMode = renderingMode;
            // Video encoding only supports vulkan at this point
            s_videoMode = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan && useVideoEncoding;

            if (s_distributedMode == DistributedMode.Renderer)
            {
                Const.UserInfo info = SocketClient.Instance.UserInfo;
                ScreenSubsection subsection =
                    new ScreenSubsection(info.userCount, info.userID, info.mergerWidth, info.mergerHeight);
                Vector2Int resolution = subsection.GetPaddedSlicedResolution();
                Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreenMode);
            }
#endif
        }

        public static DistributedMode GetDistributedMode()
        {
            return s_distributedMode;
        }

        public static bool GetVideoMode()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan && s_videoMode;
        }

        internal static int CurrentFrameID { get; set; } = -1;

        private static int LastSentFrameID { get; set; }

        internal static void RegisterReceivedData(int frameID, int userID, Datagram datagram)
        {
            if (GetVideoMode())
            {
                ProcessReceivedDataVideoFrame(frameID, userID, datagram);
            }
            else
            {
                ProcessReceivedDataRawTexture(frameID, userID, datagram);
            }
        }

        private static Dictionary<int, Datagram> s_userDatagram = new Dictionary<int, Datagram>();

        private static void ProcessReceivedDataRawTexture(int frameID, int userID, Datagram datagram)
        {
            if (s_userDatagram.ContainsKey(userID))
                s_userDatagram[userID] = datagram;
            else
                s_userDatagram.Add(userID, datagram);
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
            public int userIndex;
            public ScreenSubsection subsection;

            public byte[] receivedData;
        }

        class BlitWhitePassData
        {
            public TextureHandle whiteTexture;
            public TextureHandle colorBuffer;
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

            public ScreenSubsection subsection;
        }

        TextureDesc GetDistributedIntermediateTextureDesc(Vector2Int size, GraphicsFormat format)
        {
            return new TextureDesc(size.x, size.y, false, false)
            {
                autoGenerateMips = false,
                bindTextureMS = false,
                clearBuffer = false,
                anisoLevel = 0,
                colorFormat = format,
                depthBufferBits = DepthBits.None,
                dimension = TextureXR.dimension,
                slices = TextureXR.slices,
                filterMode = FilterMode.Trilinear,
                enableMSAA = false,
                enableRandomWrite = true
            };
        }

        void CreateYUVTexture(RenderGraphBuilder builder, Vector2Int size,
            out TextureHandle yTex,
            out TextureHandle uTex,
            out TextureHandle vTex)
        {
            yTex = builder.CreateTransientTexture(GetDistributedIntermediateTextureDesc(size, GraphicsFormat.R16_SFloat));
            uTex = builder.CreateTransientTexture(GetDistributedIntermediateTextureDesc(size / 2, GraphicsFormat.R16_SFloat));
            vTex = builder.CreateTransientTexture(GetDistributedIntermediateTextureDesc(size / 2, GraphicsFormat.R16_SFloat));
        }

        int GetYUVBufferLength(Vector2Int size)
        {
            int yChannelSize = size.x * size.y;
            return 3 * yChannelSize * TextureXR.slices;
        }

        void CreateYUVComputeBuffer(RenderGraphBuilder builder, Vector2Int size, out ComputeBufferHandle buffer)
        {
            buffer = builder.CreateTransientComputeBuffer(
                new ComputeBufferDesc(GetYUVBufferLength(size) / 2, 4, ComputeBufferType.Default));
        }

        bool ReceiveColorBuffer(RenderGraph renderGraph, TextureHandle colorBuffer, int userIndex)
        {
            if (!SocketServer.Instance.Connected(userIndex))
                return false;

            // Check if this part of the data is ready
            bool hasData = s_userDatagram.TryGetValue(userIndex, out var datagram);
            if (!hasData)
                return false;

            if (datagram == null)
                return false;

            using var builder = renderGraph.AddRenderPass<ReceiveData>($"Receive Color Buffer {userIndex}", out var passData);

            RttTestUtilities.ReceiveFrame(RttTestUtilities.Role.Merger, (uint)CurrentFrameID, userIndex);
            passData.receivedData = datagram.data;
            // Finished using the data, mark it as consumed
            s_userDatagram[userIndex] = null;

            passData.userCount = SocketServer.Instance.userCount;
            passData.userIndex = userIndex;
            // TODO: We don't have a good place to store this for now so we create a new one each frame
            passData.subsection = new ScreenSubsection(passData.userCount, userIndex, Screen.width, Screen.height);
            passData.whiteTexture = builder.ReadTexture(renderGraph.defaultResources.whiteTexture);

            Vector2Int textureSize = passData.subsection.GetPaddedSlicedResolution();

            CreateYUVComputeBuffer(builder, textureSize, out passData.receivedYUVDataBuffer);

            passData.computeBufferToYUVTexturesCS = defaultResources.shaders.convertBufferYUVCS;

            CreateYUVTexture(builder, textureSize,
                out passData.tempYTexture, out passData.tempUTexture, out passData.tempVTexture);

            passData.blitYUVToRGBMaterial = GetBlitYUVToRGBMaterial(TextureXR.dimension);
            passData.colorBufferSection =
                builder.CreateTransientTexture(
                    GetDistributedIntermediateTextureDesc(textureSize, GraphicsFormat.R16G16B16A16_SFloat));

            passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
            builder.SetRenderFunc(
                (ReceiveData data, RenderGraphContext context) =>
                {
                    // Set received data to compute buffer
                    using (new ProfilingScope(context.cmd, new ProfilingSampler($"Load Data to Compute Buffer")))
                    {
                        context.cmd.SetComputeBufferData(data.receivedYUVDataBuffer, passData.receivedData,
                            0, 0, passData.receivedData.Length);
                    }

                    // Use compute shader to move the data to YUV textures
                    using (new ProfilingScope(context.cmd,
                               new ProfilingSampler($"Copy Buffer to YUV Textures")))
                    {
                        RttTestUtilities.BeginDecodeYuv(RttTestUtilities.Role.Merger, (uint) CurrentFrameID,
                            passData.userIndex);
                        ComputeShader cs = data.computeBufferToYUVTexturesCS;
                        int kernelID = cs.FindKernel("BufferToTexture");

                        Vector2Int paddedSize = passData.subsection.GetPaddedSlicedResolution();

                        context.cmd.SetComputeBufferParam(cs, kernelID, HDShaderIDs._YUVBufferID,
                            data.receivedYUVDataBuffer);
                        context.cmd.SetComputeTextureParam(cs, kernelID, HDShaderIDs._YUVYTexID,
                            data.tempYTexture);
                        context.cmd.SetComputeTextureParam(cs, kernelID, HDShaderIDs._YUVUTexID,
                            data.tempUTexture);
                        context.cmd.SetComputeTextureParam(cs, kernelID, HDShaderIDs._YUVVTexID,
                            data.tempVTexture);
                        context.cmd.SetComputeIntParam(cs, HDShaderIDs._YUVFullWidthID, paddedSize.x);
                        context.cmd.SetComputeIntParam(cs, HDShaderIDs._YUVFullHeightID, paddedSize.y);
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

                        int threadGroupX = Mathf.CeilToInt((float)paddedSize.x / threadGroupSizeX);
                        int threadGroupY = Mathf.CeilToInt((float)paddedSize.y / threadGroupSizeY);

                        context.cmd.DispatchCompute(cs, kernelID, threadGroupX, threadGroupY, threadGroupZ);
                    }

                    // Blit YUV textures to a RGB temp texture
                    using (new ProfilingScope(context.cmd,
                               new ProfilingSampler($"Blit YUV Textures to RGB Section")))
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
                               new ProfilingSampler($"Copy Section to Color Buffer")))
                    {
                        RttTestUtilities.CombineFrame(RttTestUtilities.Role.Merger, (uint)CurrentFrameID, passData.userIndex);

                        context.cmd.SetRenderTarget(data.colorBuffer);

                        Rect activeSubSection = data.subsection.activeSubsection;
                        Vector2Int paddedResolution = data.subsection.GetPaddedSlicedResolution();
                        Vector2 scaleBias = activeSubSection.size / data.subsection.fullResolution;
                        Vector2 scaleOffset = activeSubSection.min / data.subsection.fullResolution;

                        Vector2 blitScale = activeSubSection.size / paddedResolution;
                        Vector2 blitOffset = Vector2.one * data.subsection.padding / paddedResolution;

                        HDUtils.BlitQuadWithPadding(
                            context.cmd,
                            data.colorBufferSection,
                            activeSubSection.size,
                            new Vector4(blitScale.x, blitScale.y, blitOffset.x, blitOffset.y),
                            new Vector4(scaleBias.x, scaleBias.y, scaleOffset.x, scaleOffset.y),
                            0,
                            false,
                            0);
                    }
                }
            );

            return true;
        }

        void BlitWhiteBuffer(RenderGraph renderGraph, TextureHandle colorBuffer)
        {
            using var builder = renderGraph.AddRenderPass<BlitWhitePassData>($"Blit White Buffer", out var passData);

            passData.whiteTexture = builder.ReadTexture(renderGraph.defaultResources.whiteTexture);
            passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
            builder.SetRenderFunc(
                (BlitWhitePassData data, RenderGraphContext context) =>
                {
                    HDUtils.BlitCameraTexture(context.cmd, data.whiteTexture, data.colorBuffer);
                }
            );
        }

        void SendColorBuffer(RenderGraph renderGraph, TextureHandle colorBuffer)
        {
            using var builder = renderGraph.AddRenderPass<SendData>("Send Color Buffer", out var passData);

            passData.colorBuffer = builder.ReadTexture(colorBuffer);

            passData.blitRGBToYUVMaterial = GetBlitRGBToYUVMaterial(TextureXR.dimension);

            Const.UserInfo userInfo = SocketClient.Instance.UserInfo;
            // TODO: We don't have a good place to store this for now so we create a new one each frame
            passData.subsection =
                new ScreenSubsection(userInfo.userCount, userInfo.userID, userInfo.mergerWidth, userInfo.mergerHeight);
            var textureSize = passData.subsection.GetPaddedSlicedResolution();

            CreateYUVTexture(builder, textureSize,
                out passData.tempYTexture, out passData.tempUTexture, out passData.tempVTexture);

            passData.yuvToComputeBufferCS = defaultResources.shaders.convertBufferYUVCS;

            CreateYUVComputeBuffer(builder, textureSize, out passData.computeBuffer);

            builder.SetRenderFunc(
                (SendData data, RenderGraphContext context) =>
                {
                    int currentFrameID = CurrentFrameID;
                    int rendererId = SocketClient.Instance.UserInfo.userID;

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

                        Vector2Int viewportSize = data.subsection.GetPaddedSlicedResolution();

                        context.cmd.SetComputeBufferParam(cs, kernelID, HDShaderIDs._YUVBufferID,
                            data.computeBuffer);
                        context.cmd.SetComputeTextureParam(cs, kernelID, HDShaderIDs._YUVYTexID, data.tempYTexture);
                        context.cmd.SetComputeTextureParam(cs, kernelID, HDShaderIDs._YUVUTexID, data.tempUTexture);
                        context.cmd.SetComputeTextureParam(cs, kernelID, HDShaderIDs._YUVVTexID, data.tempVTexture);
                        context.cmd.SetComputeIntParam(cs, HDShaderIDs._YUVFullWidthID, viewportSize.x);
                        context.cmd.SetComputeIntParam(cs, HDShaderIDs._YUVFullHeightID, viewportSize.y);

                        cs.GetKernelThreadGroupSizes(kernelID, out var threadGroupSizeX, out var threadGroupSizeY,
                            out _);

                        int threadGroupX = Mathf.CeilToInt((float)viewportSize.x / threadGroupSizeX);
                        int threadGroupY = Mathf.CeilToInt((float)viewportSize.y / threadGroupSizeY);

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
                            LastSentFrameID = currentFrameID;
                            Profiling.Profiler.EndSample();
                        });
                        context.cmd.WaitAllAsyncReadbackRequests();
                    }
                }
            );
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

        private struct ScreenSubsection
        {
            private const int DEFAULT_PADDING = 20;

            public Vector2Int fullResolution;
            public Rect activeSubsection;
            public int padding;

            public ScreenSubsection(int userCount, int userIndex, int screenWidth, int screenHeight)
            {
                fullResolution = new Vector2Int(screenWidth, screenHeight);

                Vector2Int viewportLayout = GetViewportLayout(userCount);

                int xIndex = userIndex % viewportLayout.x;
                int yIndex = (userCount - userIndex - 1) / viewportLayout.x;

                float leftBound = Mathf.Round((float) screenWidth / viewportLayout.x * xIndex);
                float rightBound = Mathf.Round((float) screenWidth / viewportLayout.x * (xIndex + 1));
                float bottomBound = Mathf.Round((float) screenHeight / viewportLayout.y * yIndex);
                float topBound = Mathf.Round((float) screenHeight / viewportLayout.y * (yIndex + 1));

                activeSubsection = new Rect(leftBound, bottomBound, rightBound - leftBound, topBound - bottomBound);

                padding = DEFAULT_PADDING;
            }

            public override string ToString()
            {
                return $"xMin = {activeSubsection.xMin} - {padding}; " +
                       $"xMax = {activeSubsection.xMax} + {padding}; " +
                       $"yMin = {activeSubsection.yMin} - {padding}; " +
                       $"yMax = {activeSubsection.yMax} + {padding}";
            }

            public Vector2Int GetPaddedSlicedResolution()
            {
                float leftBound = activeSubsection.xMin - padding;
                float bottomBound = activeSubsection.yMin - padding;
                float rightBound = activeSubsection.xMax + padding;
                float topBound = activeSubsection.yMax + padding;

                return new Vector2Int((int) (rightBound - leftBound), (int) (topBound - bottomBound));
            }

            public Vector2Int GetActiveSlicedResolution()
            {
                return new Vector2Int((int) activeSubsection.width, (int) activeSubsection.height);
            }

            public Matrix4x4 GetSlicedAsymmetricProjection(Matrix4x4 originalProjection)
            {
                var baseFrustumPlanes = originalProjection.decomposeProjection;
                var frustumPlanes = new FrustumPlanes
                {
                    zNear = baseFrustumPlanes.zNear,
                    zFar = baseFrustumPlanes.zFar,
                    left = Mathf.LerpUnclamped(baseFrustumPlanes.left, baseFrustumPlanes.right,
                        (activeSubsection.xMin - padding) / fullResolution.x),
                    right = Mathf.LerpUnclamped(baseFrustumPlanes.left, baseFrustumPlanes.right,
                        (activeSubsection.xMax + padding) / fullResolution.x),
                    bottom = Mathf.LerpUnclamped(baseFrustumPlanes.bottom, baseFrustumPlanes.top,
                        (activeSubsection.yMin - padding) / fullResolution.y),
                    top = Mathf.LerpUnclamped(baseFrustumPlanes.bottom, baseFrustumPlanes.top,
                        (activeSubsection.yMax + padding) / fullResolution.y)
                };
                return Matrix4x4.Frustum(frustumPlanes);
            }

            private static Vector2Int GetViewportLayout(int count)
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
        }

        #endregion
    }
}
