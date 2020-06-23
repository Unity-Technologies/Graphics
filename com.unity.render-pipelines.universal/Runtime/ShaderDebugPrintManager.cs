using System.Collections.Generic;
using Microsoft.SqlServer.Server;
using Unity.Collections;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.Universal
{
    public sealed class ShaderDebugPrintManager
    {
        private static readonly ShaderDebugPrintManager instance = new ShaderDebugPrintManager();

        private const int DebugUAVSlot = 7;
        private const int FramesInFlight = 4;
        private const int MaxBufferElements = 1024 * 1024; // 1M - must match the shader size definition

        private List<GraphicsBuffer> m_outputBuffers = new List<GraphicsBuffer>();

        private List<Rendering.AsyncGPUReadbackRequest> m_readbackRequests =
            new List<Rendering.AsyncGPUReadbackRequest>();

        private int m_FrameCounter = 0;
        private bool m_FrameCleared = false;

        static ShaderDebugPrintManager()
        {
        }

        private ShaderDebugPrintManager()
        {
            for (int i = 0; i < FramesInFlight; i++)
            {
                m_outputBuffers.Add(new GraphicsBuffer(GraphicsBuffer.Target.Structured, MaxBufferElements, 4));
                m_readbackRequests.Add(new Rendering.AsyncGPUReadbackRequest());
            }
        }

        public static ShaderDebugPrintManager Instance
        {
            get { return instance; }
        }

        public void SetShaderDebugPrintBindings(CommandBuffer cmd)
        {
            int index = m_FrameCounter % FramesInFlight;
            if (!m_readbackRequests[index].done)
            {
                // We shouldn't end up here too often
                m_readbackRequests[index].WaitForCompletion();
            }

            cmd.SetRandomWriteTarget(DebugUAVSlot, m_outputBuffers[index]);

            ClearShaderDebugPrintBuffer();
        }

        private void ClearShaderDebugPrintBuffer()
        {
            // Only clear the buffer the first time this is called in each frame
            if (!m_FrameCleared)
            {
                int index = m_FrameCounter % FramesInFlight;
                NativeArray<uint> data = new NativeArray<uint>(1, Allocator.Temp);
                data[0] = 0;
                m_outputBuffers[index].SetData(data, 0, 0, 1);
                m_FrameCleared = true;
            }
        }

        private void BufferReadComplete(Rendering.AsyncGPUReadbackRequest request)
        {
            Assert.IsTrue(request.done);

            if (!request.hasError)
            {
                NativeArray<uint> data = request.GetData<uint>(0);
                string msg = "Frame #" + m_FrameCounter + " (" + m_FrameCounter % FramesInFlight + "): ";
                // TODO: This is just for testing...
                for (int i = 0; i < 10; i++)
                {
                    msg += data[i] + ", ";
                }
                Debug.Log(msg);
            }
            else
            {
                Debug.Log("Error at read back!");
            }
        }

        public void EndFrame()
        {
            int index = m_FrameCounter % FramesInFlight;
            m_readbackRequests[index] = Rendering.AsyncGPUReadback.Request(m_outputBuffers[index], BufferReadComplete);

            m_FrameCounter++;
            m_FrameCleared = false;
        }
    }
}
