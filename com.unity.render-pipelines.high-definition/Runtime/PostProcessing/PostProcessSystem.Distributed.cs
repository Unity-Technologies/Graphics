using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class PostProcessSystem
    {
        class SendExposureData
        {
            public TextureHandle prevExposure;
            public TextureHandle nextExposure;
            public ComputeBufferHandle exposureDataBuffer;
            public ComputeShader getExposureCS;
            public int kernelID;
            public int inputPrevTextureID;
            public int inputNextTextureID;
            public int outputBufferID;
            public HDAdditionalCameraData additionalData;
        }

        class ReceiveExposureData
        {
            public TextureHandle prevExposure;
            public TextureHandle nextExposure;
            public ComputeBufferHandle exposureDataBuffer;
            public ComputeShader setExposureCS;
            public int kernelID;
            public int inputBufferID;
            public int outputPrevTextureID;
            public int outputNextTextureID;
            public HDAdditionalCameraData additionalData;
        }

        void SendExposurePass(
            RenderGraph renderGraph,
            TextureHandle prevExposureHandle,
            TextureHandle nextExposureHandle,
            HDCamera hdCamera)
        {
            if (Application.isPlaying)
            {
                using (var builder = renderGraph.AddRenderPass<SendExposureData>("Readback Exposure",
                           out var passData))
                {
                    passData.prevExposure = builder.ReadTexture(prevExposureHandle);
                    passData.nextExposure = builder.ReadTexture(nextExposureHandle);
                    passData.exposureDataBuffer = builder.CreateTransientComputeBuffer(
                        new ComputeBufferDesc(2, sizeof(float) * 2, ComputeBufferType.Default));
                    passData.getExposureCS = m_Resources.shaders.setExposureDataCS;
                    passData.kernelID = passData.getExposureCS.FindKernel("GetExposureData");
                    passData.inputPrevTextureID = Shader.PropertyToID("_PrevExposureTexture");
                    passData.inputNextTextureID = Shader.PropertyToID("_NextExposureTexture");
                    passData.outputBufferID = Shader.PropertyToID("_ExposureData");

                    passData.additionalData = hdCamera.camera.GetComponent<HDAdditionalCameraData>();

                    builder.SetRenderFunc((SendExposureData data, RenderGraphContext ctx) =>
                    {
                        // fill the the buffer with textures
                        ctx.cmd.SetComputeBufferParam(
                            data.getExposureCS,
                            data.kernelID,
                            data.outputBufferID,
                            data.exposureDataBuffer);
                        ctx.cmd.SetComputeTextureParam(
                            data.getExposureCS,
                            data.kernelID,
                            data.inputPrevTextureID,
                            data.prevExposure // <- prev texture
                        );
                        ctx.cmd.SetComputeTextureParam(
                            data.getExposureCS,
                            data.kernelID,
                            data.inputNextTextureID,
                            data.nextExposure // <- next texture
                        );
                        ctx.cmd.DispatchCompute(data.getExposureCS, data.kernelID,
                            1, 1, 1);

                        ctx.cmd.RequestAsyncReadback(data.exposureDataBuffer,
                            request =>
                            {
                                while (!request.done)
                                {
                                }

                                NativeArray<byte> nativeData = request.GetData<byte>();

                                byte[] bytes = nativeData.ToArray();

                                //TCPTransmissionDatagrams.SocketServer.Instance.SendAll(TCPTransmissionDatagrams.Datagram.DatagramType.Exposure, bytes);
                                data.additionalData.ExposureFromBytes(bytes);
                                //Debug.Log($"Exposure {BitConverter.ToSingle(bytes, 0)}");
                            }
                        );
                    });
                }
            }
        }

        void ReceiveExposurePass(
            RenderGraph renderGraph,
            TextureHandle prevExposureHandle,
            TextureHandle nextExposureHandle,
            HDCamera hdCamera)
        {
            if (!Application.isPlaying) return;

            using var builder = renderGraph.AddRenderPass<ReceiveExposureData>("Receive Exposure", out var passData);

            passData.prevExposure = builder.WriteTexture(prevExposureHandle);
            passData.nextExposure = builder.WriteTexture(nextExposureHandle);
            passData.exposureDataBuffer = builder.CreateTransientComputeBuffer(
                new ComputeBufferDesc(2, sizeof(float) * 2, ComputeBufferType.Default));
            passData.setExposureCS = m_Resources.shaders.setExposureDataCS;
            passData.kernelID = passData.setExposureCS.FindKernel("SetExposureData");
            passData.outputPrevTextureID = Shader.PropertyToID("_PrevExposureTexture");
            passData.outputNextTextureID = Shader.PropertyToID("_NextExposureTexture");
            passData.inputBufferID = Shader.PropertyToID("_ExposureData");

            passData.additionalData = hdCamera.camera.GetComponent<HDAdditionalCameraData>();

            builder.SetRenderFunc((ReceiveExposureData data, RenderGraphContext ctx) =>
            {
                using (new ProfilingScope(ctx.cmd, new ProfilingSampler($"Blit Exposure")))
                {
                    // if (!TCPTransmissionDatagrams.SocketClient.Instance.IsConnected())
                    //     return ;
                    // TCPTransmissionDatagrams.SocketClient.Instance.GetReceivedLastOne(TCPTransmissionDatagrams.Datagram.DatagramType.Exposure, out TCPTransmissionDatagrams.Datagram datagram);
                    //
                    // if (datagram == null)
                    // {
                    //     Debug.Log("data null");
                    //     return;
                    // }
                    // else
                    // {
                    //     Debug.Log($"Exposure {BitConverter.ToSingle(datagram.data, 0)}");
                    // }
                    //Debug.Log($"Exposure {data.additionalData.m_ExposureData}");

                    byte[] exposureBytes = data.additionalData.ExposureAsBytes();

                    // Set data to compute buffer
                    ctx.cmd.SetComputeBufferData(data.exposureDataBuffer, exposureBytes,
                        0, 0, 16);
                    //TCPTransmissionDatagrams.SocketClient.Instance.AddReceiveRingBuffer(TCPTransmissionDatagrams.Datagram.DatagramType.Exposure, datagram);

                    // fill the textures with buffer
                    ctx.cmd.SetComputeBufferParam(
                        data.setExposureCS,
                        data.kernelID,
                        data.inputBufferID,
                        data.exposureDataBuffer);
                    ctx.cmd.SetComputeTextureParam(
                        data.setExposureCS,
                        data.kernelID,
                        data.outputPrevTextureID,
                        data.prevExposure // <- prev texture
                    );
                    ctx.cmd.SetComputeTextureParam(
                        data.setExposureCS,
                        data.kernelID,
                        data.outputNextTextureID,
                        data.nextExposure // <- prev texture
                    );
                    ctx.cmd.DispatchCompute(data.setExposureCS, data.kernelID,
                        1, 1, 1);
                }
            });
        }
    }
}
