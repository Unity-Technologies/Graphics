using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteInEditMode]
public class CreateAndBindBufferTest : MonoBehaviour
{

    [StructLayout(LayoutKind.Sequential)]
    struct CustomData
    {
        public Vector4 position;
        public Vector4 color;
    }

    public void OnDisable()
    {
        if (m_buffer != null)
        {
            m_buffer.Release();
            m_buffer = null;
        }
    }

    private GraphicsBuffer m_buffer;
    void Update()
    {
        if (m_buffer == null)
        {
            m_buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 3, Marshal.SizeOf(typeof(CustomData)));
            var data = new List<CustomData>()
            {
                new CustomData() { position = new Vector4(0, 0, 0, 0), color = new Vector4(1, 0, 0, 0) },
                new CustomData() { position = new Vector4(1, 0, 0, 0), color = new Vector4(0, 1, 0, 0) },
                new CustomData() { position = new Vector4(2, 0, 0, 0), color = new Vector4(0, 0, 1, 0) },
            };
            m_buffer.SetData(data);
        }

        var vfx = GetComponent<VisualEffect>();
        if (vfx.GetGraphicsBuffer("buffer") == null)
        {
            vfx.SetGraphicsBuffer("buffer", m_buffer);
        }
    }
}
