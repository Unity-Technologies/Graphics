using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

#if UNITY_EDITOR
[CustomEditor(typeof(BufferBinder))]
class BufferBinderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Reset"))
        {
            var bufferBinder = (BufferBinder)target;
            bufferBinder.Reset();
        }
    }
}
#endif

[ExecuteInEditMode]
public class BufferBinder : MonoBehaviour
{
    public enum BufferType
    {
        Float,
        Int,
        UInt,
        Float2,
        Float3,
        Float4,
    }

    private static Dictionary<BufferType, int> TypeToSizeMap = new()
    {
        { BufferType.Float, Marshal.SizeOf(typeof(float)) },
        { BufferType.Int, Marshal.SizeOf(typeof(int)) },
        { BufferType.UInt, Marshal.SizeOf(typeof(uint)) },
        { BufferType.Float2, Marshal.SizeOf(typeof(float)) * 2 },
        { BufferType.Float3, Marshal.SizeOf(typeof(float)) * 3 },
        { BufferType.Float4, Marshal.SizeOf(typeof(float)) * 4 },
    };

    [SerializeField] private string bufferName;
    [SerializeField] private int bufferSize;
    [SerializeField] private BufferType bufferType;
    [SerializeField] private VisualEffect vfx;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float radius;

    private GraphicsBuffer buffer;

    private void OnValidate()
    {
        this.InitBuffer();
    }

    private void OnEnable()
    {
        this.InitBuffer();
    }

    private void OnDisable()
    {
        this.buffer.Release();
        this.buffer = null;
    }

    private void InitBuffer()
    {
        this.buffer?.Release();
        var bufferNameID = Shader.PropertyToID(this.bufferName);
        this.buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, this.bufferSize, TypeToSizeMap[this.bufferType]);
        //Shader.SetGlobalBuffer(bufferNameID, this.buffer);
        this.data = Enumerable.Repeat(0f, this.bufferSize).ToArray();
        this.buffer.SetData(data);
        this.vfx.SetGraphicsBuffer(this.bufferName, this.buffer);
    }

    public void Reset()
    {
        this.buffer.SetData(Enumerable.Repeat(Vector3.zero, bufferSize).ToArray());
    }

    private float currentAngle;
    Array data;
    private void Update()
    {
        this.currentAngle += Time.deltaTime * rotationSpeed;
        var x = this.radius * Mathf.Cos(this.currentAngle);
        var y = this.radius * Mathf.Sin(this.currentAngle);

        this.buffer.GetData(this.data);
        this.data.SetValue(x, 0);
        this.data.SetValue(y, 1);
        this.buffer.SetData(this.data);
    }
}
