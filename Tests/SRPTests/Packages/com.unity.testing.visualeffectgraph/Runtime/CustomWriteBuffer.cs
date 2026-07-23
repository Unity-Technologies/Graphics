using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngine.VFX;

namespace Unity.Testing.VisualEffectGraph
{
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
    struct ManualGPUEvent
    {
        public Vector3 position;
        public Vector3 color;
    }

    public class CustomWriteBuffer : MonoBehaviour
    {
        public VisualEffect m_FakeGPUVFX;
        public VisualEffect m_CellularVFX;

        private GraphicsBuffer m_FakeGPUEvent_Buffer;
        private GraphicsBuffer m_FakeGPUEvent_Counter;
        private GraphicsBuffer m_CellularBuffer;


        void OnEnable()
        {
            m_FakeGPUEvent_Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None, 64, Marshal.SizeOf(typeof(ManualGPUEvent)));
            m_FakeGPUEvent_Counter = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None, 1, Marshal.SizeOf(typeof(uint)));
            m_FakeGPUEvent_Counter.SetData(new uint[] { 0 });

            m_FakeGPUVFX.SetGraphicsBuffer("RWBuffer", m_FakeGPUEvent_Buffer);
            m_FakeGPUVFX.SetGraphicsBuffer("Counter", m_FakeGPUEvent_Counter);
            m_FakeGPUVFX.enabled = true;

            m_CellularBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None, 64 * 64, Marshal.SizeOf(typeof(float)));
            m_CellularBuffer.SetData(new float[64*64]);
            m_CellularVFX.SetGraphicsBuffer("RWBuffer", m_CellularBuffer);
            m_CellularVFX.enabled = true;
        }

        void OnDisable()
        {
            if (m_FakeGPUVFX)
                m_FakeGPUVFX.enabled = false;

            m_FakeGPUEvent_Buffer.Release();
            m_FakeGPUEvent_Counter.Release();

            if (m_CellularVFX)
                m_CellularVFX.enabled = false;
            m_CellularBuffer.Release();
        }
    }
}
