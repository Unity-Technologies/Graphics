using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    partial class LineRendering
    {
        static int ComputeBinningRecordCapacity(MemoryBudget budget) => (int)Mathf.Ceil(((int) budget * 1024 * 1024) / Marshal.SizeOf<ClusterRecord>()) ;
        static int ComputeWorkQueueCapacity(MemoryBudget budget)     => (int)Mathf.Ceil(((int) budget * 1024 * 1024) / sizeof(uint)) ;

        static int DivRoundUp(int x, int y) => (x + y - 1) / y;

        static int NextPowerOfTwo(int v)
        {
            v -= 1;
            v |= v >> 16;
            v |= v >> 8;
            v |= v >> 4;
            v |= v >> 2;
            v |= v >> 1;
            return v + 1;
        }

        struct BindRendererToComputeKernel : IDisposable
        {
            private List<GraphicsBuffer> boundBuffers;

            static void GetAttributeBufferNames(VertexAttribute attribute, out string bufferName, out string strideName, out string offsetName)
            {
                var attributeName = Enum.GetName(typeof(VertexAttribute), attribute);

                bufferName = $"_VertexBuffer{attributeName}";
                strideName = $"_VertexBuffer{attributeName}Stride";
                offsetName = $"_VertexBuffer{attributeName}Offset";
            }

            internal BindRendererToComputeKernel(CommandBuffer cmd, RendererData renderer)
            {
                boundBuffers = new List<GraphicsBuffer>();

                renderer.mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

                cmd.SetComputeIntParam(renderer.vertexSetupCompute, "_VertexCount", renderer.mesh.vertexCount);

                // Bind the existing attribute buffers, bind a dummy one if it doesn't exist (prevents compiler warning).
                {
                    void TryBindVertexBuffer(VertexAttribute attribute, ref List<GraphicsBuffer> buffers)
                    {
                        var streamIndex = renderer.mesh.GetVertexAttributeStream(attribute);

                        GraphicsBuffer streamBuffer;
                        int streamOffset, streamStride;

                        if (streamIndex < 0)
                        {
                            streamBuffer = CoreUtils.emptyBuffer;
                            streamOffset = 0;
                            streamStride = 4;
                        }
                        else
                        {
                            streamBuffer = renderer.mesh.GetVertexBuffer(streamIndex);
                            streamOffset = renderer.mesh.GetVertexAttributeOffset(attribute);
                            streamStride = renderer.mesh.GetVertexBufferStride(streamIndex);
                            buffers.Add(streamBuffer);
                        }

                        GetAttributeBufferNames(attribute, out var bufferName, out var strideName, out var offsetName);

                        cmd.SetComputeBufferParam(renderer.vertexSetupCompute, 0, bufferName, streamBuffer);
                        cmd.SetComputeIntParam(renderer.vertexSetupCompute, offsetName, streamOffset);
                        cmd.SetComputeIntParam(renderer.vertexSetupCompute, strideName, streamStride);
                    }

                    // Try to bind all existing mesh vertex buffer streams to the kernel.
                    foreach (var attribute in Enum.GetValues(typeof(VertexAttribute)).Cast<VertexAttribute>())
                    {
                        TryBindVertexBuffer(attribute, ref boundBuffers);
                    }
                }

                // Also need to bind these matrices manually. (TODO: Is it cross-SRP safe?)
                cmd.SetComputeMatrixParam(renderer.vertexSetupCompute, "unity_ObjectToWorld",       renderer.matrixW);
                cmd.SetComputeMatrixParam(renderer.vertexSetupCompute, "unity_WorldToObject",       renderer.matrixW.inverse);
                cmd.SetComputeMatrixParam(renderer.vertexSetupCompute, "unity_MatrixPreviousM",     renderer.matrixWP);
                cmd.SetComputeMatrixParam(renderer.vertexSetupCompute, "unity_MatrixPreviousMI",    renderer.matrixWP.inverse);
                cmd.SetComputeVectorParam(renderer.vertexSetupCompute, "unity_MotionVectorsParams", renderer.motionVectorParams);

                cmd.SetComputeParamsFromMaterial(renderer.vertexSetupCompute, 0, renderer.material);
            }

            public void Dispose()
            {
                foreach (var buffer in boundBuffers)
                    buffer.Dispose();
            }
        }

        // TODO: Need to optimize this whole routine.
        private IEnumerable<RendererData[]> SortRenderDatasByCameraDistance(RendererData[] renderData, Camera camera)
        {
            var renderDatasNoGroup = renderData.Where(o => o.@group == RendererGroup.None).Select(o => new[] { o });
            var renderDatasInGroup = renderData.GroupBy(o => o.@group).Where(g => g.Key != RendererGroup.None).Select(o => o.ToArray());
            var renderDatasToSort  = renderDatasNoGroup.Concat(renderDatasInGroup).ToArray();

            if (renderDatasToSort.Length > 1)
            {
                // Sort
                Array.Sort(renderDatasToSort, (RendererData[] a, RendererData[] b) =>
                {
                    float cameraDistanceA = a.Average(i => i.distanceToCamera);
                    float cameraDistanceB = b.Average(i => i.distanceToCamera);

                    return cameraDistanceA.CompareTo(cameraDistanceB);
                });
            }

            foreach (var data in renderDatasToSort)
            {
                yield return data;
            }
        }

        // Exclusive prefix sum.
        int[] PrefixSum(int[] input)
        {
            int[] output = new int[input.Length];

            int sum = 0;

            for (int i = 0; i < input.Length; ++i)
            {
                output[i] = sum;
                sum += input[i];
            }

            return output;
        }
    }
}
