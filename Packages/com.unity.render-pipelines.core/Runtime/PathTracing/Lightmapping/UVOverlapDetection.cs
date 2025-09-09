using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.PathTracing.Lightmapping
{
    // Detects pixels where multiple UV charts have overlapping bilinear neighborhoods.
    internal class UVOverlapDetection : IDisposable
    {
        private static class ShaderProperties
        {
            public static int TextureSize = Shader.PropertyToID("_TextureSize");
            public static int PerPixelChart = Shader.PropertyToID("_PerPixelChart");
            public static int InstanceIndex = Shader.PropertyToID("_InstanceIndex");
            public static int EdgeCount = Shader.PropertyToID("_EdgeCount");
            public static int TriangleEdges = Shader.PropertyToID("_TriangleEdges");
            public static int ChartIndices = Shader.PropertyToID("_ChartIndices");
            public static int OverlapPixels = Shader.PropertyToID("_OverlapPixels");
            public static int OverlapInstances = Shader.PropertyToID("_OverlapInstances");
            public static int TileX = Shader.PropertyToID("_TileX");
            public static int TileY = Shader.PropertyToID("_TileY");
            public static int TileSize = Shader.PropertyToID("_TileSize");
        }

        private int _lightmapResolution;

        private ComputeShader _shader;
        private NativeArray<float4> _triangleEdges;
        private NativeArray<uint> _chartIndices;
        private GraphicsBuffer _triangleEdgesBuffer;
        private GraphicsBuffer _chartIndicesBuffer;
        private GraphicsBuffer _perPixelChart;
        private GraphicsBuffer _overlapPixelsBuffer;
        private GraphicsBuffer _overlapInstancesBuffer;

        private int _overlapKernel;
        private uint _overlapKernelSize;

        public void Initialize(ComputeShader shader, uint lightmapResolution, uint maxEdgeCount, uint instanceCount)
        {
            _lightmapResolution = (int)lightmapResolution;

            _shader = shader;
            _triangleEdges = new NativeArray<float4>((int)maxEdgeCount,Allocator.Persistent);
            _chartIndices = new NativeArray<uint>((int)maxEdgeCount, Allocator.Persistent);
            _triangleEdgesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _triangleEdges.Length, UnsafeUtility.SizeOf<float4>());
            _chartIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _chartIndices.Length, sizeof(uint));
            _perPixelChart = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _lightmapResolution*_lightmapResolution, sizeof(uint));
            _overlapPixelsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _lightmapResolution*_lightmapResolution, sizeof(uint));
            _overlapInstancesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)instanceCount, sizeof(uint));

            _overlapKernel = shader.FindKernel("MarkBilinearOverlaps");
            shader.GetKernelThreadGroupSizes(_overlapKernel, out _overlapKernelSize, out _, out _);

            // Initialize buffers
            _overlapPixelsBuffer.SetData(new uint[_lightmapResolution*_lightmapResolution]);
            _overlapInstancesBuffer.SetData(new uint[instanceCount]);
            var initPerPixelChart = new uint[_lightmapResolution * _lightmapResolution];
            Array.Fill(initPerPixelChart, uint.MaxValue);
            _perPixelChart.SetData(initPerPixelChart);
        }

        public void MarkOverlapsInInstance(
            CommandBuffer cmd,
            Mesh uvMesh,
            NativeArray<UInt32> vertexToChartIndex,
            float4 occupiedST,
            uint instanceIndex,
            uint chartIndexOffset)
        {
            // Get the start and end pos of every edge, and the chart index for each edge.
            var indices = uvMesh.triangles;
            var vertices = uvMesh.vertices;
            for (uint triangleIdx = 0; triangleIdx < indices.Length / 3; triangleIdx++)
            {
                for (uint edgeOffset = 0; edgeOffset < 3; edgeOffset++)
                {
                    uint baseTriangleIdx = triangleIdx * 3;
                    int startVertexIdx = indices[baseTriangleIdx + edgeOffset];
                    int endVertexIdx = indices[baseTriangleIdx + ((edgeOffset + 1) % 3)];

                    float3 start = vertices[startVertexIdx];
                    float3 end = vertices[endVertexIdx];
                    start.xy = (start.xy * occupiedST.xy + occupiedST.zw) * _lightmapResolution;
                    end.xy = (end.xy * occupiedST.xy + occupiedST.zw) * _lightmapResolution;
                    _triangleEdges[(int)(baseTriangleIdx + edgeOffset)] = new float4(start.x, start.y, end.x, end.y);

                    uint chartIdx = chartIndexOffset + vertexToChartIndex[startVertexIdx];
                    _chartIndices[(int)(baseTriangleIdx + edgeOffset)] = chartIdx;
                }
            }
            cmd.SetBufferData(_triangleEdgesBuffer, _triangleEdges);
            cmd.SetBufferData(_chartIndicesBuffer, _chartIndices);

            // If the lightmap resolution is over this constant, we split the dispatch into multiple
            // smaller dispatches over tiles, to prevent dispatches that are too large. Otherwise in
            // the worst case, every lightmap texel can be checked in one dispatch.
            const uint tileSize = 1024;

            // Mark overlaps
            int edgeCount = indices.Length;
            cmd.SetComputeIntParam(_shader, ShaderProperties.InstanceIndex, (int)instanceIndex);
            cmd.SetComputeIntParam(_shader, ShaderProperties.EdgeCount, edgeCount);
            cmd.SetComputeIntParam(_shader, ShaderProperties.TextureSize, _lightmapResolution);
            cmd.SetComputeIntParam(_shader, ShaderProperties.TileSize, (int)tileSize);
            cmd.SetComputeBufferParam(_shader, _overlapKernel, ShaderProperties.TriangleEdges, _triangleEdgesBuffer);
            cmd.SetComputeBufferParam(_shader, _overlapKernel, ShaderProperties.ChartIndices, _chartIndicesBuffer);
            cmd.SetComputeBufferParam(_shader, _overlapKernel, ShaderProperties.PerPixelChart, _perPixelChart);
            cmd.SetComputeBufferParam(_shader, _overlapKernel, ShaderProperties.OverlapPixels, _overlapPixelsBuffer);
            cmd.SetComputeBufferParam(_shader, _overlapKernel, ShaderProperties.OverlapInstances, _overlapInstancesBuffer);
            int dispatchSize = GraphicsHelpers.DivUp(edgeCount, _overlapKernelSize);

            uint tileCountOnEachDim = GraphicsHelpers.DivUp((uint)_lightmapResolution, tileSize);
            for (uint tileY = 0; tileY < tileCountOnEachDim; tileY++)
            {
                for (uint tileX = 0; tileX < tileCountOnEachDim; tileX++)
                {
                    cmd.SetComputeIntParam(_shader, ShaderProperties.TileX, (int)tileX);
                    cmd.SetComputeIntParam(_shader, ShaderProperties.TileY, (int)tileY);
                    cmd.DispatchCompute(_shader, _overlapKernel, dispatchSize, 1, 1);
                }
            }
        }

        public void CompactAndReadbackOverlaps(
            CommandBuffer cmd,
            out uint[] uniqueOverlapPixelIndices,
            out ulong[] uniqueOverlapInstanceIndices)
        {
            // Make sure all kernels are finished
            GraphicsHelpers.Flush(cmd);

            // Readback overlap buffers
            uint[] overlapPixels = new uint[_overlapPixelsBuffer.count];
            _overlapPixelsBuffer.GetData(overlapPixels);
            uint[] overlapInstances = new uint[_overlapInstancesBuffer.count];
            _overlapInstancesBuffer.GetData(overlapInstances);

            // Deduplicate overlaps
            List<uint> uniqueOverlapPixelIndicesSet = new List<uint>();
            List<ulong> uniqueOverlapInstanceIndicesSet = new List<ulong>();
            for (uint pixelIndex = 0; pixelIndex < _overlapPixelsBuffer.count; pixelIndex++)
            {
                if (overlapPixels[pixelIndex] != 0)
                    uniqueOverlapPixelIndicesSet.Add(pixelIndex);
            }
            for (uint instanceIndex = 0; instanceIndex < _overlapInstancesBuffer.count; instanceIndex++)
            {
                if (overlapInstances[instanceIndex] != 0)
                    uniqueOverlapInstanceIndicesSet.Add(instanceIndex);
            }
            uniqueOverlapPixelIndices = new uint[uniqueOverlapPixelIndicesSet.Count];
            uniqueOverlapPixelIndicesSet.CopyTo(uniqueOverlapPixelIndices);
            uniqueOverlapInstanceIndices = new ulong[uniqueOverlapInstanceIndicesSet.Count];
            uniqueOverlapInstanceIndicesSet.CopyTo(uniqueOverlapInstanceIndices);

            // Sort to keep the order deterministic
            Array.Sort(uniqueOverlapPixelIndices);
            Array.Sort(uniqueOverlapInstanceIndices);
        }

        public void Dispose()
        {
            if (_triangleEdges.IsCreated)
                _triangleEdges.Dispose();
            if (_chartIndices.IsCreated)
                _chartIndices.Dispose();
            if (_triangleEdgesBuffer != null && _triangleEdgesBuffer.IsValid())
                _triangleEdgesBuffer.Dispose();
            if (_chartIndicesBuffer != null && _chartIndicesBuffer.IsValid())
                _chartIndicesBuffer.Dispose();
            if (_perPixelChart != null && _perPixelChart.IsValid())
                _perPixelChart.Dispose();
            if (_overlapPixelsBuffer != null && _overlapPixelsBuffer.IsValid())
                _overlapPixelsBuffer.Dispose();
            if (_overlapInstancesBuffer != null && _overlapInstancesBuffer.IsValid())
                _overlapInstancesBuffer.Dispose();
        }

#if UNITY_EDITOR
        public static ComputeShader LoadShader()
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(
                "Packages/com.unity.render-pipelines.core/Runtime/PathTracing/Shaders/Lightmapping/BilinearOverlaps.compute");
        }
#endif
    }
}
