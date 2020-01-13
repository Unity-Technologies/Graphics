using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    public class DebugRenderer
    {
        [GenerateHLSL]
        struct LineData
        {
            public Vector4  p0;
            public Vector4  p1;
            public Color    color;
        }

        const int       kMaxLineAllocationCount = short.MaxValue;
        int             m_MaxLineCount;
        int             m_CurrentLineCount;
        int             m_MissedLineAllocation;
        ComputeBuffer   m_LineDataBuffer;
        List<LineData>  m_LineData;

        Material        m_DebugRendererMaterial;

        readonly int    _CameraRelativeOffset = Shader.PropertyToID("_CameraRelativeOffset");
        readonly int    _LineData = Shader.PropertyToID("_LineData");

        public DebugRenderer(int initialLineCount, Shader debugRendererShader)
        {
            m_DebugRendererMaterial = CoreUtils.CreateEngineMaterial(debugRendererShader);

            m_MaxLineCount = initialLineCount;
            m_LineDataBuffer = new ComputeBuffer(m_MaxLineCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LineData)));
        }

        public void Cleanup()
        {
            CoreUtils.Destroy(m_DebugRendererMaterial);
            m_LineDataBuffer.Release();
        }

        public void ClearData()
        {
            m_LineData.Clear();
            m_CurrentLineCount = 0;
            m_MissedLineAllocation = 0;
        }

        bool CheckLineAllocation(int newAllocCount)
        {
            int newSize = m_CurrentLineCount + newAllocCount;
            // If we require more space, try to allocate a bigger buffer.
            if (newSize > m_MaxLineCount)
            {
                // In this case we can't allocate more memory so return false to calling code.
                if (newSize > kMaxLineAllocationCount)
                {
                    m_MissedLineAllocation += newAllocCount;
                    return false;
                }

                m_LineDataBuffer.Release();

                m_MaxLineCount = Math.Min(kMaxLineAllocationCount, m_MaxLineCount * 2);
                m_LineDataBuffer = new ComputeBuffer(m_MaxLineCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LineData)));
            }

            return true;
        }

        internal void Render(CommandBuffer cmd, HDCamera hdCamera)
        {
            m_LineDataBuffer.SetData(m_LineData);
            m_DebugRendererMaterial.SetBuffer(_LineData, m_LineDataBuffer);

            Vector3 cameraRelativeOffset = ShaderConfig.s_CameraRelativeRendering != 0 ? hdCamera.camera.transform.position : Vector3.zero;
            m_DebugRendererMaterial.SetVector(_CameraRelativeOffset, cameraRelativeOffset);
            cmd.DrawProcedural(Matrix4x4.identity, m_DebugRendererMaterial, 0, MeshTopology.Lines, m_LineData.Count * 2);
        }

        public void PushLine(Vector4 p0, Vector4 p1, Color color)
        {
            if (!CheckLineAllocation(1))
                return;

            m_LineData.Add(new LineData { p0 = p0, p1 = p1, color = color });
        }

        public void PushOBB(OrientedBBox obb, Color color)
        {

        }
    }
}
