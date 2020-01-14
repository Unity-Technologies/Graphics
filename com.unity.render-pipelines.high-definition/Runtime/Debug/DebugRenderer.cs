using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    public class DebugRenderer
    {
        // Line data.
        [GenerateHLSL]
        struct LineData
        {
            public Vector4  p0;
            public Vector4  p1;
            public Color    color;
        }

        class LineBuffers
        {
            public int              maxLineCount;
            public int              currentLineCount;
            public int              missedLineAllocation;
            public ComputeBuffer    lineDataBuffer;
            public List<LineData>   lineData;

            public LineBuffers(int initialLineCount)
            {
                maxLineCount = initialLineCount;
                lineDataBuffer = new ComputeBuffer(maxLineCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LineData)));
                lineData = new List<LineData>();
            }

            public void Clear()
            {
                lineData.Clear();
                currentLineCount = 0;
                missedLineAllocation = 0;
            }

            public bool CheckAllocation(int newAllocCount)
            {
                int newSize = currentLineCount + newAllocCount;
                // If we require more space, try to allocate a bigger buffer.
                if (newSize > maxLineCount)
                {
                    // In this case we can't allocate more memory so return false to calling code.
                    if (newSize > kMaxLineAllocationCount)
                    {
                        missedLineAllocation += newAllocCount;
                        return false;
                    }

                    lineDataBuffer.Release();

                    maxLineCount = Math.Min(kMaxLineAllocationCount, maxLineCount * 2);
                    lineDataBuffer = new ComputeBuffer(maxLineCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LineData)));
                }

                return true;
            }

            public void Cleanup()
            {
                lineDataBuffer.Release();
            }
        }

        const int               kMaxLineAllocationCount = short.MaxValue;
        LineBuffers             m_LineNoDepthTest;
        LineBuffers             m_LineDepthTest;

        // Rendering Shaders
        Material                m_DebugRendererMaterial;
        MaterialPropertyBlock   m_DebugRendererMPB = new MaterialPropertyBlock();
        readonly int            _LineData = Shader.PropertyToID("_LineData");
        readonly int            _CameraRelativeOffset = Shader.PropertyToID("_CameraRelativeOffset");
        int                     m_LineNoDepthTestPass;
        int                     m_LineDepthTestPass;

        Vector3[]               m_OBBPointsCache = new Vector3[8];

        public DebugRenderer(int initialLineCount, Shader debugRendererShader)
        {
            m_DebugRendererMaterial = CoreUtils.CreateEngineMaterial(debugRendererShader);

            m_LineNoDepthTestPass = m_DebugRendererMaterial.FindPass("LineNoDepthTest");
            m_LineDepthTestPass = m_DebugRendererMaterial.FindPass("LineDepthTest");

            m_LineNoDepthTest = new LineBuffers(initialLineCount);
            m_LineDepthTest = new LineBuffers(initialLineCount);
        }

        public void Cleanup()
        {
            CoreUtils.Destroy(m_DebugRendererMaterial);
            m_LineNoDepthTest.Cleanup();
            m_LineDepthTest.Cleanup();
        }

        public void ClearData()
        {
            m_LineNoDepthTest.Clear();
            m_LineDepthTest.Clear();
        }

        void RenderLines(CommandBuffer cmd, HDCamera hdCamera, LineBuffers lineBuffers, int passIndex)
        {
            lineBuffers.lineDataBuffer.SetData(lineBuffers.lineData);
            m_DebugRendererMPB.SetBuffer(_LineData, lineBuffers.lineDataBuffer);

            Vector3 cameraRelativeOffset = ShaderConfig.s_CameraRelativeRendering != 0 ? hdCamera.camera.transform.position : Vector3.zero;
            m_DebugRendererMPB.SetVector(_CameraRelativeOffset, cameraRelativeOffset);
            cmd.DrawProcedural(Matrix4x4.identity, m_DebugRendererMaterial, passIndex, MeshTopology.Lines, 2, lineBuffers.lineData.Count, m_DebugRendererMPB);
        }

        internal void Render(CommandBuffer cmd, HDCamera hdCamera)
        {
            RenderLines(cmd, hdCamera, m_LineDepthTest, m_LineDepthTestPass);
            RenderLines(cmd, hdCamera, m_LineNoDepthTest, m_LineNoDepthTestPass);
        }

        public void PushLine(Vector4 p0, Vector4 p1, Color color, bool depthTest = true)
        {
            var lineBuffers = depthTest ? m_LineDepthTest : m_LineNoDepthTest;

            if (!lineBuffers.CheckAllocation(1))
                return;

            lineBuffers.lineData.Add(new LineData { p0 = p0, p1 = p1, color = color });
        }

        public void PushOBB(OrientedBBox obb, Color color, bool depthTest = true)
        {
            var lineBuffers = depthTest ? m_LineDepthTest : m_LineNoDepthTest;

            if (!lineBuffers.CheckAllocation(12))
                return;

            obb.GetPoints(m_OBBPointsCache);
            // Base
            lineBuffers.lineData.Add(new LineData { p0 = m_OBBPointsCache[0], p1 = m_OBBPointsCache[1], color = color });
            lineBuffers.lineData.Add(new LineData { p0 = m_OBBPointsCache[1], p1 = m_OBBPointsCache[2], color = color });
            lineBuffers.lineData.Add(new LineData { p0 = m_OBBPointsCache[2], p1 = m_OBBPointsCache[3], color = color });
            lineBuffers.lineData.Add(new LineData { p0 = m_OBBPointsCache[3], p1 = m_OBBPointsCache[0], color = color });

            // Top
            lineBuffers.lineData.Add(new LineData { p0 = m_OBBPointsCache[4], p1 = m_OBBPointsCache[5], color = color });
            lineBuffers.lineData.Add(new LineData { p0 = m_OBBPointsCache[5], p1 = m_OBBPointsCache[6], color = color });
            lineBuffers.lineData.Add(new LineData { p0 = m_OBBPointsCache[6], p1 = m_OBBPointsCache[7], color = color });
            lineBuffers.lineData.Add(new LineData { p0 = m_OBBPointsCache[7], p1 = m_OBBPointsCache[4], color = color });

            // Body
            lineBuffers.lineData.Add(new LineData { p0 = m_OBBPointsCache[0], p1 = m_OBBPointsCache[4], color = color });
            lineBuffers.lineData.Add(new LineData { p0 = m_OBBPointsCache[1], p1 = m_OBBPointsCache[5], color = color });
            lineBuffers.lineData.Add(new LineData { p0 = m_OBBPointsCache[2], p1 = m_OBBPointsCache[6], color = color });
            lineBuffers.lineData.Add(new LineData { p0 = m_OBBPointsCache[3], p1 = m_OBBPointsCache[7], color = color });
        }
    }
}
