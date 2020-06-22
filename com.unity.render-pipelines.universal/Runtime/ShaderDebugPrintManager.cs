using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    public class ShaderDebugPrintManager
    {
        private const int DebugUAVSlot = 7;
        private const int FramesInFlight = 4;

        private List<GraphicsBuffer> m_outputBuffers = new List<GraphicsBuffer>(FramesInFlight);

        private int m_FrameCounter = 0;

        public ShaderDebugPrintManager()
        {

        }

        void SetShaderDebugPrintBindings(CommandBuffer cmd)
        {
            cmd.SetRandomWriteTarget(DebugUAVSlot, m_outputBuffers[m_FrameCounter]);
        }

        void EndFrame()
        {
            // TODO: Read back

            m_FrameCounter++;
        }
    }
}
