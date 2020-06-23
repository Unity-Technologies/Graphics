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
        private const int MaxBufferElements = 1024 * 1024; // 1M

        private List<GraphicsBuffer> m_outputBuffers = new List<GraphicsBuffer>();

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
            }
        }

        public static ShaderDebugPrintManager Instance
        {
            get { return instance; }
        }

        public void SetShaderDebugPrintBindings(CommandBuffer cmd)
        {
            cmd.SetRandomWriteTarget(DebugUAVSlot, m_outputBuffers[m_FrameCounter % FramesInFlight]);

            // Only clear the buffer the first time this is called in each frame
            if (!m_FrameCleared)
            {
                cmd.ClearRandomWriteTargets();
                m_FrameCleared = true;
            }
        }

        private void BufferReadComplete(Rendering.AsyncGPUReadbackRequest request)
        {
            Assert.IsTrue(request.done);

            if (!request.hasError)
            {
                NativeArray<uint> data = request.GetData<uint>(0);
                Debug.Log("Frame number #" + m_FrameCounter + " debug value: "  + data[0]);
            }
            else
            {
                Debug.Log("Error at read back!");
            }
        }

        public void EndFrame()
        {
            // TODO: Read back
            Rendering.AsyncGPUReadback.Request(m_outputBuffers[m_FrameCounter % FramesInFlight], BufferReadComplete);

            m_FrameCounter++;
            m_FrameCleared = false;
        }
    }
}
