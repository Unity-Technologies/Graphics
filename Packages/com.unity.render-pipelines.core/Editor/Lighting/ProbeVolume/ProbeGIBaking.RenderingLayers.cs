using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering.UnifiedRayTracing;
using Unity.Collections;

namespace UnityEngine.Rendering
{
    partial class AdaptiveProbeVolumes
    {
        /// <summary>
        /// Rendering Layer baker
        /// </summary>
        abstract class RenderingLayerBaker : IDisposable
        {
            /// <summary>The current baking step.</summary>
            public abstract ulong currentStep { get; }
            /// <summary>The total amount of step.</summary>
            public abstract ulong stepCount { get; }

            /// <summary>Array storing the rendering layer mask per probe. Only the first 4 bits are used.</summary>
            public abstract NativeArray<uint> renderingLayerMasks { get; }

            /// <summary>
            /// This is called before the start of baking to allow allocating necessary resources.
            /// </summary>
            /// <param name="bakingSet">The baking set that is currently baked.</param>
            /// <param name="probePositions">The probe positions.</param>
            public abstract void Initialize(ProbeVolumeBakingSet bakingSet, NativeArray<Vector3> probePositions);

            /// <summary>
            /// Run a baking step. Baking is considered done when currentStep property equals stepCount.
            /// </summary>
            /// <returns>Return false if bake failed and should be stopped.</returns>
            public abstract bool Step();

            /// <summary>
            /// Performs necessary tasks to free allocated resources.
            /// </summary>
            public abstract void Dispose();
        }

        class DefaultRenderingLayer : RenderingLayerBaker
        {
            const int k_MaxProbeCountPerBatch = 65535 * 64;

            static readonly int _ProbePositions = Shader.PropertyToID("_ProbePositions");
            static readonly int _LayerMasks = Shader.PropertyToID("_LayerMasks");
            static readonly int _RenderingLayerMasks = Shader.PropertyToID("_RenderingLayerMasks");

            int batchIndex, batchCount;
            Vector4 regionMasks;

            // Input data
            NativeArray<Vector3> probePositions;

            // Output buffers
            GraphicsBuffer layerMaskBuffer;
            NativeArray<uint> layerMask;

            public override NativeArray<uint> renderingLayerMasks => layerMask;
            
            CommandBuffer cmd;
            IRayTracingAccelStruct m_AccelerationStructure;
            GraphicsBuffer scratchBuffer;
            GraphicsBuffer probePositionsBuffer;

            public override ulong currentStep => (ulong)batchIndex;
            public override ulong stepCount => (ulong)batchCount;

            public override void Initialize(ProbeVolumeBakingSet bakingSet, NativeArray<Vector3> positions)
            {
                // Divide the job into batches to reduce memory usage.
                batchCount = CoreUtils.DivRoundUp(bakingSet.useRenderingLayers ? positions.Length : 0, k_MaxProbeCountPerBatch);
                batchIndex = 0;

                probePositions = positions;
                if (batchCount == 0)
                    return;

                regionMasks = Vector4.zero;
                for (int i = 0; i < bakingSet.renderingLayerMasks.Length; i++)
                    regionMasks[i] = Unity.Mathematics.math.asfloat(bakingSet.renderingLayerMasks[i].mask);

                // Allocate array storing results
                layerMask = new NativeArray<uint>(probePositions.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                // Create acceleration structure
                m_AccelerationStructure = BuildAccelerationStructure();

                int batchSize = Mathf.Min(k_MaxProbeCountPerBatch, probePositions.Length);
                probePositionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, batchSize, Marshal.SizeOf<Vector3>());
                layerMaskBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, batchSize, Marshal.SizeOf<uint>());
                scratchBuffer = RayTracingHelper.CreateScratchBufferForBuildAndDispatch(m_AccelerationStructure, s_TracingContext.shaderRL, (uint)batchSize, 1, 1);

                cmd = new CommandBuffer();
                m_AccelerationStructure.Build(cmd, scratchBuffer);
                Graphics.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            static IRayTracingAccelStruct BuildAccelerationStructure()
            {
                var accelStruct = s_TracingContext.CreateAccelerationStructure();
                var contributors = m_BakingBatch.contributors;

                foreach (var renderer in contributors.renderers)
                {
                    var mesh = renderer.component.GetComponent<MeshFilter>().sharedMesh;
                    if (mesh == null)
                        continue;

                    int subMeshCount = mesh.subMeshCount;
                    for (int i = 0; i < subMeshCount; ++i)
                    {
                        var instanceDesc = new MeshInstanceDesc(mesh, i);
                        instanceDesc.localToWorldMatrix = renderer.component.transform.localToWorldMatrix;
                        instanceDesc.materialID = renderer.component.renderingLayerMask; // repurpose the material id as we don't need it here

                        instanceDesc.enableTriangleCulling = true;
                        instanceDesc.frontTriangleCounterClockwise = false;

                        accelStruct.AddInstance(instanceDesc);
                    }
                }

                foreach (var terrain in contributors.terrains)
                {
                    uint mask = GetInstanceMask(terrain.component.shadowCastingMode);

                    var terrainDesc = new TerrainDesc(terrain.component);
                    terrainDesc.localToWorldMatrix = terrain.component.transform.localToWorldMatrix;
                    terrainDesc.materialID = terrain.component.renderingLayerMask; // repurpose the material id as we don't need it here

                    accelStruct.AddTerrain(terrainDesc);
                }

                return accelStruct;
            }

            public override bool Step()
            {
                if (currentStep >= stepCount)
                    return true;
                
                var shader = s_TracingContext.shaderRL;

                int batchOffset = batchIndex * k_MaxProbeCountPerBatch;
                int batchSize = Mathf.Min(probePositions.Length - batchOffset, k_MaxProbeCountPerBatch);
                cmd.SetBufferData(probePositionsBuffer, probePositions.GetSubArray(batchOffset, batchSize));

                shader.SetAccelerationStructure(cmd, "_AccelStruct", m_AccelerationStructure);
                shader.SetVectorParam(cmd, _RenderingLayerMasks, regionMasks);
                shader.SetBufferParam(cmd, _ProbePositions, probePositionsBuffer);
                shader.SetBufferParam(cmd, _LayerMasks, layerMaskBuffer);
                
                shader.Dispatch(cmd, scratchBuffer, (uint)batchSize, 1, 1);
                batchIndex++;

                Graphics.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                FetchResults(batchOffset, batchSize);

                return true;
            }

            void FetchResults(int batchOffset, int batchSize)
            {
                var batchLayers = layerMask.GetSubArray(batchOffset, batchSize);
                var req = AsyncGPUReadback.RequestIntoNativeArray(ref batchLayers, layerMaskBuffer, batchSize * sizeof(uint), 0);

                // TODO: use double buffering to hide readback latency
                req.WaitForCompletion();
            }

            public override void Dispose()
            {
                if (m_AccelerationStructure == null)
                    return;

                cmd.Dispose();

                scratchBuffer?.Dispose();
                probePositionsBuffer.Dispose();
                m_AccelerationStructure.Dispose();

                layerMaskBuffer.Dispose();
                layerMask.Dispose();
            }
        }
    }
}
