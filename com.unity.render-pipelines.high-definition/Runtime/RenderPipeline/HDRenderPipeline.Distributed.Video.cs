using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RttTest;
using Unity.Rendering.VideoCodec;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Profiling;
using UnityEngine.TCPTransmissionDatagrams;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        private static Codec[] s_videoCodecs = null;

        private static Codec[] VideoCodecs
        {
            get
            {
                if (!GetVideoMode()) return null;
                switch (GetDistributedMode())
                {
                    case DistributedMode.None:
                        return null;
                    case DistributedMode.Renderer:
                        s_videoCodecs ??= new Codec[1];
                        break;
                    case DistributedMode.Merger:
                        s_videoCodecs ??= new Codec[SocketServer.Instance.userCount];
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return s_videoCodecs;
            }
        }

        private static Encoder VideoEncoder
        {
            get
            {
                if (!GetVideoMode()) return null;
                if (GetDistributedMode() != DistributedMode.Renderer) return null;
                Const.UserInfo userInfo = SocketClient.Instance.UserInfo;
                ScreenSubsection subsection =
                    new ScreenSubsection(userInfo.userCount, userInfo.userID, userInfo.mergerWidth, userInfo.mergerHeight);
                Vector2Int viewportSize = subsection.GetPaddedSlicedResolution();
                if (VideoCodecs[0] == null)
                {
                    // lazy initialization
                    VideoCodecs[0] = new Encoder(0);
                }

                if (!VideoCodecs[0].IsCreated)
                {
                    // Create native plugin resource
                    VideoCodecs[0].Create();
                }
                if (!VideoCodecs[0].IsOpened)
                {
                    // Register callback
                    VideoCodecs[0].RegisterOnCodecCompleteCallback(SendEncodedData);

                    // Inherit Unity Graphics Hardware
                    VideoCodecs[0].InitializeGraphicsHardwareContext(viewportSize);
                    // Create codec context
                    // TODO: create an API to set this from game code
                    VideoCodecs[0].InitializeCodecContext(100000000);
                    // Prepare frames, packets
                    VideoCodecs[0].InitializeCodecResources();
                }
                return (Encoder) VideoCodecs[0];
            }
        }

        private static Decoder[] VideoDecoders
        {
            get
            {
                if (!GetVideoMode()) return null;
                if (GetDistributedMode() != DistributedMode.Merger) return null;
                for (int i = 0; i < VideoCodecs.Length; ++i)
                {
                    ScreenSubsection subsection =
                        new ScreenSubsection(SocketServer.Instance.userCount, i, Screen.width, Screen.height);
                    Vector2Int viewportSize = subsection.GetPaddedSlicedResolution();

                    if (VideoCodecs[i] == null)
                    {
                        // lazy initialization
                        VideoCodecs[i] = new Decoder(i);
                    }

                    if (!VideoCodecs[i].IsCreated)
                    {
                        // Create native plugin resource
                        VideoCodecs[i].Create();
                    }

                    if (!VideoCodecs[i].IsOpened)
                    {
                        // Register callback
                        // TODO: when actual decoding is moved out of render pipeline, this will be needed
                        //VideoCodecs[i].RegisterOnCodecCompleteCallback(DecoderCallback);

                        // Inherit Unity Graphics Hardware
                        VideoCodecs[i].InitializeGraphicsHardwareContext(viewportSize);
                        // Create codec context
                        VideoCodecs[i].InitializeCodecContext();
                        // Prepare frames, packets
                        VideoCodecs[i].InitializeCodecResources();
                    }
                }

                return Array.ConvertAll(VideoCodecs, c => (Decoder) c);
            }
        }

        private static void DestroyCodecs()
        {
            if (!GetVideoMode()) return;
            if (s_videoCodecs == null) return;

            for (var i = 0; i < s_videoCodecs.Length; ++i)
            {
                if (s_videoCodecs[i] == null) continue;

                if (s_videoCodecs[i].IsOpened)
                {
                    s_videoCodecs[i].DestroyCodecResources();
                    s_videoCodecs[i].DestroyCodecContext();
                    s_videoCodecs[i].DestroyGraphicsContext();
                }

                if (s_videoCodecs[i].IsCreated)
                {
                    s_videoCodecs[i].Destroy();
                }

                s_videoCodecs[i] = null;
            }

            s_videoCodecs = null;
        }

        private static Dictionary<int, int> s_lastFrameID = new Dictionary<int, int>();

        private static void ProcessReceivedDataVideoFrame(int frameID, int userID, Datagram datagram)
        {
            // Set received data to video packet
            if (datagram == null)
                return;

            Profiler.BeginSample($"Load Data {userID} to Video Packet");

            if (s_lastFrameID.TryGetValue(userID, out var lastFrameID))
            {
                if (lastFrameID == frameID) return;
            }
            else
            {
                s_lastFrameID.Add(userID, -1);
            }

            s_lastFrameID[userID] = frameID;

            RttTestUtilities.ReceiveFrame(RttTestUtilities.Role.Merger, (uint)frameID, userID);
            VideoDecoders[userID].SetPacketData(datagram.data);
            VideoDecoders[userID].FrameID = frameID;
            VideoDecoders[userID].SignalCodecThread();

            Profiler.EndSample();

            // Decoding process starts right after signalling the thread, no need for another pass here
            // Video/Graphics data transfer starts right after decoding process, before calling callback
            // The texture object is locked in native plugin so the process is safe across the main thread and the render thread
        }

        private static void SendEncodedData(IntPtr data, int size, int frameId)
        {
            var rendererId = SocketClient.Instance.UserInfo.userID;
            Profiling.Profiler.BeginSample("Send Encoded Data");

            RttTestUtilities.FinishReadBack(RttTestUtilities.Role.Renderer, (uint)frameId, rendererId);

            byte[] managedData = new byte[size];
            Marshal.Copy(data, managedData, 0, size);

            RttTestUtilities.SendFrame(RttTestUtilities.Role.Renderer, (uint)frameId, rendererId);
            //SocketClient.Instance.ReplaceOrSet(Datagram.DatagramType.VideoFrame, managedData);
            SocketClient.Instance.Set(Datagram.DatagramType.VideoFrame, managedData, frameId);
            LastSentFrameID = frameId;
            Profiling.Profiler.EndSample();
        }

        class ReceiveDataVideo
        {
            public TextureHandle whiteTexture;

            public TextureHandle tempYTexture;
            public TextureHandle tempUVTexture;

            public Material blitYUVToRGBMaterial;

            public TextureHandle colorBufferSection;

            public TextureHandle colorBuffer;

            public int userCount;
            public int userIndex;
            public ScreenSubsection subsection;
        }

        class SendDataVideo
        {
            public TextureHandle colorBuffer;

            public Material blitRGBToYUVMaterial;

            public TextureHandle tempYTexture;
            public TextureHandle tempUVTexture;

            public ScreenSubsection subsection;
        }

        void CreateNV12Texture(RenderGraphBuilder builder, Vector2Int size,
            out TextureHandle yTex,
            out TextureHandle uvTex)
        {
            yTex = builder.CreateTransientTexture(GetDistributedIntermediateTextureDesc(size, GraphicsFormat.R16_SFloat));
            uvTex = builder.CreateTransientTexture(GetDistributedIntermediateTextureDesc(size / 2, GraphicsFormat.R16G16_SFloat));
        }

        bool ReceiveColorBufferVideo(RenderGraph renderGraph, TextureHandle colorBuffer, int userIndex)
        {
            if (!SocketServer.Instance.Connected(userIndex))
                return false;

            using var builder = renderGraph.AddRenderPass<ReceiveDataVideo>($"Receive Color Buffer (Video Decode) {userIndex}", out var passData);

            passData.userCount = SocketServer.Instance.userCount;
            passData.userIndex = userIndex;
            // TODO: We don't have a good place to store this for now so we create a new one each frame
            passData.subsection = new ScreenSubsection(passData.userCount, userIndex, Screen.width, Screen.height);

            passData.whiteTexture = builder.ReadTexture(renderGraph.defaultResources.whiteTexture);

            Vector2Int textureSize = passData.subsection.GetPaddedSlicedResolution();

            CreateNV12Texture(builder, textureSize,
                out passData.tempYTexture, out passData.tempUVTexture);

            passData.blitYUVToRGBMaterial = GetBlitYUVToRGBMaterial(TextureXR.dimension);

            passData.colorBufferSection = builder.CreateTransientTexture(
                GetDistributedIntermediateTextureDesc(textureSize, GraphicsFormat.R16G16B16A16_SFloat));

            passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);

            builder.SetRenderFunc(
                (ReceiveDataVideo data, RenderGraphContext context) =>
                {
                    // Blit YUV textures to a RGB temp texture
                    using (new ProfilingScope(context.cmd,
                               new ProfilingSampler($"Blit NV12 Textures to RGB Section")))
                    {
                        RttTestUtilities.BeginDecodeYuv(RttTestUtilities.Role.Merger, (uint)CurrentFrameID, passData.userIndex);

                        // Blit to Unity Texture First
                        VideoDecoders[passData.userIndex].SetTextures(new Texture[] {passData.tempYTexture, passData.tempUVTexture},
                            new[] {1.0f, 0.5f});
                        VideoDecoders[passData.userIndex].BlitTexture(context.cmd);

                        var mpbYUVToRGB = context.renderGraphPool.GetTempMaterialPropertyBlock();

                        mpbYUVToRGB.SetTexture(HDShaderIDs._BlitTextureY, data.tempYTexture);
                        mpbYUVToRGB.SetTexture(HDShaderIDs._BlitTextureU, data.tempUVTexture);

                        context.cmd.SetRenderTarget(data.colorBufferSection);
                        CoreUtils.DrawFullScreen(context.cmd, data.blitYUVToRGBMaterial, mpbYUVToRGB, 1);
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

        void SendColorBufferVideo(RenderGraph renderGraph, TextureHandle colorBuffer)
        {
            using var builder = renderGraph.AddRenderPass<SendDataVideo>("Send Color Buffer (Video Encode)", out var passData);

            passData.colorBuffer = builder.ReadTexture(colorBuffer);

            passData.blitRGBToYUVMaterial = GetBlitRGBToYUVMaterial(TextureXR.dimension);

            Const.UserInfo userInfo = SocketClient.Instance.UserInfo;
            // TODO: We don't have a good place to store this for now so we create a new one each frame
            passData.subsection =
                new ScreenSubsection(userInfo.userCount, userInfo.userID, userInfo.mergerWidth, userInfo.mergerHeight);

            CreateNV12Texture(builder, passData.subsection.GetPaddedSlicedResolution(),
                out passData.tempYTexture, out passData.tempUVTexture);

            builder.SetRenderFunc(
                (SendDataVideo data, RenderGraphContext context) =>
                {
                    int currentFrameID = CurrentFrameID;
                    int rendererId = SocketClient.Instance.UserInfo.userID;

                    RttTestUtilities.BeginEncodeYuv(RttTestUtilities.Role.Renderer, (uint)currentFrameID, rendererId);

                    // Blit color buffer to NV12 Textures
                    using (new ProfilingScope(context.cmd,
                               new ProfilingSampler("Blit Color Buffer to NV12 Textures")))
                    {
                        var mpbRGBToYUV = context.renderGraphPool.GetTempMaterialPropertyBlock();
                        mpbRGBToYUV.SetTexture(HDShaderIDs._BlitTexture, data.colorBuffer);

                        context.cmd.SetRenderTarget(data.tempYTexture);
                        CoreUtils.DrawFullScreen(context.cmd, data.blitRGBToYUVMaterial, mpbRGBToYUV, 0);

                        context.cmd.SetRenderTarget(data.tempUVTexture);
                        CoreUtils.DrawFullScreen(context.cmd, data.blitRGBToYUVMaterial, mpbRGBToYUV, 3);
                    }

                    // Blit NV12 textures to Encoder
                    using (new ProfilingScope(context.cmd,
                               new ProfilingSampler("Blit NV12 Textures to Encoder")))
                    {
                        // TODO: this executes earilier than it looks!
                        VideoEncoder.SetTextures(new Texture[] {passData.tempYTexture, passData.tempUVTexture}, new []{1.0f, 0.5f});
                        VideoEncoder.FrameID = currentFrameID;
                        VideoEncoder.BlitTexture(context.cmd);
                    }

                    // Transfer Encoder texture to Video frame
                    using (new ProfilingScope(context.cmd,
                               new ProfilingSampler("Transfer Graphics Data To Video")))
                    {
                        VideoEncoder.TransferData(context.cmd);
                    }

                    // Tell the encoding thread that we are ready
                    using (new ProfilingScope(context.cmd,
                               new ProfilingSampler("Signal Encoder Thread")))
                    {
                        VideoEncoder.SignalCodecThread(context.cmd);
                        RttTestUtilities.BeginReadBack(RttTestUtilities.Role.Renderer, (uint)currentFrameID, rendererId);
                    }
                }
            );
        }
    }
}
