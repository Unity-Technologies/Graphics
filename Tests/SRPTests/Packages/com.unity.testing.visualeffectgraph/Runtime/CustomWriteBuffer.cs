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
        public VisualEffect m_ReadBackVFX;
        public VisualEffect m_FakeGPUVFX;
        public VisualEffect m_CellularVFX;
        public GameObject m_ReadBackPrefab;

        private uint m_CurrentReadBackIndex = 0;
        private GraphicsBuffer[] m_ReadBackBuffers = new GraphicsBuffer[2];
        private GraphicsBuffer m_FakeGPUEvent_Buffer;
        private GraphicsBuffer m_FakeGPUEvent_Counter;
        private GraphicsBuffer m_CellularBuffer;

        private const int kReadbackGraphicsBufferSize = (64 + 1) * 6;

        void OnEnable()
        {
            m_ReadBackBuffers[0] = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, kReadbackGraphicsBufferSize, Marshal.SizeOf(typeof(uint)));
            m_ReadBackBuffers[1] = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, kReadbackGraphicsBufferSize, Marshal.SizeOf(typeof(uint)));
            m_ReadBackBuffers[0].SetData(new uint[] { 0 });
            m_ReadBackBuffers[1].SetData(new uint[] { 0 });
            m_ReadBackVFX.SetGraphicsBuffer("RWBuffer", m_ReadBackBuffers[m_CurrentReadBackIndex]);
            m_ReadBackVFX.enabled = true;

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
            if (m_ReadBackVFX)
                m_ReadBackVFX.enabled = false;
            m_ReadBackBuffers[0].Release();
            m_ReadBackBuffers[1].Release();

            if (m_FakeGPUVFX)
                m_FakeGPUVFX.enabled = false;

            m_FakeGPUEvent_Buffer.Release();
            m_FakeGPUEvent_Counter.Release();

            if (m_CellularVFX)
                m_CellularVFX.enabled = false;
            m_CellularBuffer.Release();
        }

        private const float kPeriod = 0.2f;
        private float m_WaitTime = kPeriod;

        private AsyncGPUReadbackRequest? m_Async = null;

        void Update()
        {
            m_WaitTime -= Time.deltaTime;
            if (m_WaitTime > 0.0f)
                return;

            m_WaitTime = kPeriod;
            if (m_Async == null)
            {
                m_Async = AsyncGPUReadback.Request(m_ReadBackBuffers[m_CurrentReadBackIndex], OnReceivedData);
            }
        }

        void OnReceivedData(AsyncGPUReadbackRequest async)
        {
            if (async.done)
            {
                var spawnCount = async.GetData<uint>();
                var allData = async.GetData<float>();

                if (spawnCount[0] > 0)
                {
                    int cursor = 1;
                    for (int i = 0; i < spawnCount[0]; ++i)
                    {
                        var position = new Vector3(allData[cursor++], allData[cursor++], allData[cursor++]);
                        var color = new Vector3(allData[cursor++], allData[cursor++], allData[cursor++]);
                        var newPrefab = Instantiate(m_ReadBackPrefab);
                        newPrefab.transform.position = position;
                        newPrefab.GetComponentInChildren<Renderer>().material
                            .SetVector("_Color", new Vector4(color.x, color.y, color.z, 0.0f));
                    }

                    m_ReadBackBuffers[m_CurrentReadBackIndex].SetData(new uint[] { 0 });
                    m_CurrentReadBackIndex = (++m_CurrentReadBackIndex) % 2;
                    m_ReadBackVFX.SetGraphicsBuffer("RWBuffer", m_ReadBackBuffers[m_CurrentReadBackIndex]);
                }
            }
            m_Async = null;
        }
    }
}
