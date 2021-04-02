using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteInEditMode]
public class CreateAndBindBufferTest : MonoBehaviour
{
    public void OnDisable()
    {
        if (m_buffer != null)
        {
            m_buffer.Release();
            m_buffer = null;
        }
    }

    [VFXType, StructLayout(LayoutKind.Sequential)]
    public struct Rectangle
    {
        public Vector2 size;
        public Vector3 color;
    }

    [VFXType(VFXTypeAttribute.Flags.GraphicsBuffer), StructLayout(LayoutKind.Sequential)]
    struct CustomData
    {
        public Rectangle rectangle;
        public Vector3 position;
    }

    private static readonly int s_BufferID = Shader.PropertyToID("buffer");
    private GraphicsBuffer m_buffer;
    void Update()
    {
        if (m_buffer == null)
        {
            m_buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 3, Marshal.SizeOf(typeof(CustomData)));
            var data = new List<CustomData>()
            {
                new CustomData() { position = new Vector3(0, 0, 1), rectangle = new Rectangle() { color = new Vector3(1, 0, 0), size = new Vector2(1, 1) }},
                new CustomData() { position = new Vector3(1, 0, 1), rectangle = new Rectangle() { color = new Vector3(0, 1, 0), size = new Vector2(1, 2) }},
                new CustomData() { position = new Vector3(2, 0, 1), rectangle = new Rectangle() { color = new Vector3(0, 0, 1), size = new Vector2(1, 3) }},
            };
            m_buffer.SetData(data);
        }

        var vfx = GetComponent<VisualEffect>();
        if (vfx.GetGraphicsBuffer(s_BufferID) == null)
            vfx.SetGraphicsBuffer(s_BufferID, m_buffer);
    }
}
