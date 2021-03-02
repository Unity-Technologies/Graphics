using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements.UIR;
using Vertex = UnityEngine.UIElements.Vertex;
using Unity.Profiling;
using UnityEngine.UIElements;

internal class UpdateBufferRangesRepaintUpdater : BaseVisualTreeUpdater
{
    private List<VisualElement> m_Elements = new List<VisualElement>();
    private UpdateBufferRangesPainter m_Painter;

    public UpdateBufferRangesRepaintUpdater(Material mat)
    {
        m_Painter = new UpdateBufferRangesPainter(mat);
    }

    private static readonly string s_Description = "UpdateBufferRangesRepaintUpdater";
    private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(s_Description);
    public override ProfilerMarker profilerMarker => s_ProfilerMarker;

    protected override void Dispose(bool disposing)
    {
        m_Painter.Dispose(disposing);
    }

    public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
    {
    }

    public void RegisterRoot(VisualElement ve)
    {
        m_Elements.Add(ve);
        for (int i = 0; i < ve.hierarchy.childCount; ++i)
            RegisterRoot(ve.hierarchy[i]);
    }

    public override void Update()
    {
        if (m_Elements.Count > 0)
        {
            foreach (var ve in m_Elements)
            {
                m_Painter.currentElement = ve;
                m_Painter.DrawVisualElementBackground();
                //ve.InvokeGenerateVisualContent(new MeshGenerationContext(m_Painter));
            }
            m_Elements.Clear();
        }

        GL.Clear(true, false, Color.clear, 1.0f);

        var viewport = panel.visualTree.layout;
        var projection = ProjectionUtils.Ortho(viewport.xMin, viewport.xMax, viewport.yMax, viewport.yMin, -0.001f, 1.001f);
        GL.LoadProjectionMatrix(projection);
        GL.modelview = Matrix4x4.identity;

        m_Painter.Draw();
    }
}

internal unsafe class UpdateBufferRangesPainter : IStylePainter
{
    const int kMaxVertices = 1024;
    const int kMaxIndices = 4096;
    const int kMaxRanges = 1024;

    Utility.GPUBuffer<Vertex> m_VertexGPUBuffer = new Utility.GPUBuffer<Vertex>(kMaxVertices, Utility.GPUBufferType.Vertex);
    Utility.GPUBuffer<UInt16> m_IndexGPUBuffer = new Utility.GPUBuffer<UInt16>(kMaxIndices, Utility.GPUBufferType.Index);
    NativeArray<Vertex> m_VertexData = new NativeArray<Vertex>(kMaxVertices, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    NativeArray<UInt16> m_IndexData = new NativeArray<UInt16>(kMaxIndices, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    int m_VertexOffset = 0;
    int m_IndexOffset = 0;

    NativeArray<GfxUpdateBufferRange> m_VertexUpdateRanges = new NativeArray<GfxUpdateBufferRange>(kMaxRanges, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    NativeArray<GfxUpdateBufferRange> m_IndexUpdateRanges = new NativeArray<GfxUpdateBufferRange>(kMaxRanges, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    int m_VertexUpdateOffset = 0;
    int m_IndexUpdateOffset = 0;

    NativeArray<DrawBufferRange> m_DrawRanges = new NativeArray<DrawBufferRange>(kMaxRanges, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    int m_DrawRangeCount = 0;

    Material m_Mat;

    internal VisualElement currentElement;
    private IntPtr m_VertexDecl;

    void InitVertexDeclaration()
    {
        var vertexDecl = new VertexAttributeDescriptor[]
        {
            // Vertex position first
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),

            // Then UINT32 color
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),

            // Then UV
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),

            // TransformID page coordinate (XY), ClipRectID page coordinate (ZW), packed into a Color32
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.UNorm8, 4),

            // In-page index for (TransformID, ClipRectID, OpacityID), Flags (vertex type), all packed into a Color32
            new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.UNorm8, 4),

            // OpacityID page coordinate (XY), SVG SettingIndex (16-bit encoded in ZW), packed into a Color32
            new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.UNorm8, 4)
        };
        m_VertexDecl = Utility.GetVertexDeclaration(vertexDecl);
    }

    public UpdateBufferRangesPainter(Material mat)
    {
        m_Mat = mat;
        InitVertexDeclaration();
    }

    public void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_VertexGPUBuffer.Dispose();
            m_IndexGPUBuffer.Dispose();
            m_VertexData.Dispose();
            m_IndexData.Dispose();
            m_VertexUpdateRanges.Dispose();
            m_IndexUpdateRanges.Dispose();
            m_DrawRanges.Dispose();
        }
    }

    public void Draw()
    {
        m_Mat.SetPass(0);
        IntPtr *vStream = stackalloc IntPtr[1];
        vStream[0] = m_VertexGPUBuffer.BufferPointer;
        var ranges = m_DrawRanges.Slice(0, m_DrawRangeCount);
        Utility.DrawRanges(m_IndexGPUBuffer.BufferPointer, vStream, 1, new IntPtr(ranges.GetUnsafePtr()), ranges.Length, m_VertexDecl);
    }

    public MeshWriteData DrawMesh(int vertexCount, int indexCount, Texture texture, Material material, MeshGenerationContext.MeshFlags flags)
    {
        return new MeshWriteData();
    }

    public void DrawText(MeshGenerationContextUtils.TextParams textParams, ITextHandle handle, float pixelsPerPoint) {}
    public void DrawRectangle(MeshGenerationContextUtils.RectangleParams rectParams)
    {
        var rect = rectParams.rect;
        var color = rectParams.color;

        var m = currentElement.worldTransform;
        m_VertexData[m_VertexOffset + 0] = new Vertex() { position = m.MultiplyPoint(new Vector2(rect.x, rect.y)), tint = color };
        m_VertexData[m_VertexOffset + 1] = new Vertex() { position = m.MultiplyPoint(new Vector2(rect.x + rect.width, rect.y)), tint = color };
        m_VertexData[m_VertexOffset + 2] = new Vertex() { position = m.MultiplyPoint(new Vector2(rect.x, rect.y + rect.height)), tint = color };
        m_VertexData[m_VertexOffset + 3] = new Vertex() { position = m.MultiplyPoint(new Vector2(rect.x + rect.width, rect.y + rect.height)), tint = color };

        int vertexStride = m_VertexGPUBuffer.ElementStride;
        m_VertexUpdateRanges[m_VertexUpdateOffset] = new GfxUpdateBufferRange()
        {
            source = new UIntPtr(m_VertexData.Slice(m_VertexOffset, 4).GetUnsafeReadOnlyPtr()),
            offsetFromWriteStart = 0,
            size = 4 * (UInt32)vertexStride
        };
        m_VertexGPUBuffer.UpdateRanges(m_VertexUpdateRanges.Slice(m_VertexUpdateOffset, 1), m_VertexOffset * vertexStride, (m_VertexOffset + 4) * vertexStride);
        ++m_VertexUpdateOffset;

        m_IndexData[m_IndexOffset + 0] = (UInt16)(m_VertexOffset + 0);
        m_IndexData[m_IndexOffset + 1] = (UInt16)(m_VertexOffset + 1);
        m_IndexData[m_IndexOffset + 2] = (UInt16)(m_VertexOffset + 2);
        m_IndexData[m_IndexOffset + 3] = (UInt16)(m_VertexOffset + 2);
        m_IndexData[m_IndexOffset + 4] = (UInt16)(m_VertexOffset + 1);
        m_IndexData[m_IndexOffset + 5] = (UInt16)(m_VertexOffset + 3);

        int indexStride = m_IndexGPUBuffer.ElementStride;
        m_IndexUpdateRanges[m_IndexUpdateOffset] = new GfxUpdateBufferRange()
        {
            source = new UIntPtr(m_IndexData.Slice(m_IndexOffset, 6).GetUnsafeReadOnlyPtr()),
            offsetFromWriteStart = 0,
            size = 6 * (UInt32)indexStride
        };
        m_IndexGPUBuffer.UpdateRanges(m_IndexUpdateRanges.Slice(m_IndexUpdateOffset, 1), m_IndexOffset * indexStride, (m_IndexOffset + 6) * indexStride);
        ++m_IndexUpdateOffset;


        var drawRange = m_DrawRanges[m_DrawRangeCount];
        drawRange.firstIndex = m_IndexOffset;
        drawRange.indexCount = 6;
        drawRange.minIndexVal = m_VertexOffset;
        drawRange.vertsReferenced = 4;
        m_DrawRanges[m_DrawRangeCount++] = drawRange;

        m_VertexOffset += 4;
        m_IndexOffset += 6;
    }

    public void DrawBorder(MeshGenerationContextUtils.BorderParams borderParams) {}
    public void DrawImmediate(Action callback, bool cullingEnabled) {}

    public VisualElement visualElement { get { return currentElement; } }

    public void DrawVisualElementBackground()
    {
        if (currentElement.layout.width <= Mathf.Epsilon || currentElement.layout.height <= Mathf.Epsilon)
            return;

        var style = currentElement.computedStyle;
        if (style.backgroundColor != Color.clear)
        {
            var parent = currentElement.hierarchy.parent;
            var rectParams = new MeshGenerationContextUtils.RectangleParams
            {
                rect = GUIUtility.AlignRectToDevice(currentElement.rect),
                color = style.backgroundColor
            };
            MeshGenerationContextUtils.GetVisualElementRadii(currentElement,
                out rectParams.topLeftRadius,
                out rectParams.bottomLeftRadius,
                out rectParams.topRightRadius,
                out rectParams.bottomRightRadius);
            DrawRectangle(rectParams);
        }
    }
}
