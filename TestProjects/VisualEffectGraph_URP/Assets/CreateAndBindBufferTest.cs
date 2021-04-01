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

    //TODO : should not be in editor
    [UnityEditor.VFX.VFXType(UnityEditor.VFX.VFXTypeAttribute.Flags.GraphicsBuffer), StructLayout(LayoutKind.Sequential)]
    struct CustomData
    {
        public Vector3 position;
        public Vector3 color;
    }

    [UnityEditor.VFX.VFXType(UnityEditor.VFX.VFXTypeAttribute.Flags.GraphicsBuffer), StructLayout(LayoutKind.Sequential)]
    public struct Rectangle
    {
        public Vector2 size;
        public Vector3 color;
    }

    [UnityEditor.VFX.VFXType(UnityEditor.VFX.VFXTypeAttribute.Flags.GraphicsBuffer), StructLayout(LayoutKind.Sequential)]
    struct CustomDataBis
    {
        public Rectangle rectangle;
        public Vector3 position;
    }

    private GraphicsBuffer m_buffer;
    private GraphicsBuffer m_buffer2;
    void Update()
    {
        if (m_buffer == null)
        {
            m_buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 3, Marshal.SizeOf(typeof(CustomData)));
            var data = new List<CustomData>()
            {
                new CustomData() { position = new Vector3(0, 0, 0), color = new Vector3(1, 0, 0) },
                new CustomData() { position = new Vector3(1, 0, 0), color = new Vector3(0, 1, 0) },
                new CustomData() { position = new Vector3(2, 0, 0), color = new Vector3(0, 0, 1) },
            };
            m_buffer.SetData(data);
        }

        if (m_buffer2 == null)
        {
            m_buffer2 = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 3, Marshal.SizeOf(typeof(CustomDataBis)));
            var data = new List<CustomDataBis>()
            {
                new CustomDataBis() { position = new Vector3(0, 0, 1), rectangle = new Rectangle() { color = new Vector3(1, 0, 0), size = new Vector2(1, 1) }},
                new CustomDataBis() { position = new Vector3(1, 0, 1), rectangle = new Rectangle() { color = new Vector3(0, 1, 0), size = new Vector2(1, 2) }},
                new CustomDataBis() { position = new Vector3(2, 0, 1), rectangle = new Rectangle() { color = new Vector3(0, 0, 1), size = new Vector2(1, 3) } },
            };
            m_buffer2.SetData(data);
        }

        var vfx = GetComponent<VisualEffect>();
        if (vfx.GetGraphicsBuffer("buffer") == null)
            vfx.SetGraphicsBuffer("buffer", m_buffer);

        if (vfx.GetGraphicsBuffer("buffer2") == null)
            vfx.SetGraphicsBuffer("buffer2", m_buffer2);
    }
}
