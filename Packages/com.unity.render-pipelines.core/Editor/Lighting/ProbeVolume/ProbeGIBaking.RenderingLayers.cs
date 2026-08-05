using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.Rendering.Sampling;
using UnityEngine.Rendering.UnifiedRayTracing;

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
            static readonly int _SobolBuffer = Shader.PropertyToID("_SobolMatricesBuffer");

            int m_BatchIndex, m_BatchCount;
            Vector4 m_RegionMasks;

            // Input data
            NativeArray<Vector3> m_ProbePositions;

            // Output buffers
            GraphicsBuffer m_LayerMaskBuffer;
            NativeArray<uint> m_LayerMask;

            public override NativeArray<uint> renderingLayerMasks => m_LayerMask;

            CommandBuffer m_Cmd;
            AccelStructAdapter m_AccelerationStructure;
            GraphicsBuffer m_ScratchBuffer;
            GraphicsBuffer m_ProbePositionsBuffer;
            GraphicsBuffer m_SobolBuffer;
            bool m_HasTerrains;

            public override ulong currentStep => (ulong)m_BatchIndex;
            public override ulong stepCount => (ulong)m_BatchCount;

            public override void Initialize(ProbeVolumeBakingSet bakingSet, NativeArray<Vector3> positions)
            {
                // Divide the job into batches to reduce memory usage.
                m_BatchCount = CoreUtils.DivRoundUp(bakingSet.useRenderingLayers ? positions.Length : 0, k_MaxProbeCountPerBatch);
                m_BatchIndex = 0;

                m_ProbePositions = positions;
                if (m_BatchCount == 0)
                    return;

                m_RegionMasks = Vector4.zero;
                for (int i = 0; i < bakingSet.renderingLayerMasks.Length; i++)
                {
                    m_RegionMasks[i] = Unity.Mathematics.math.asfloat(bakingSet.renderingLayerMasks[i].mask);
                }

                // Allocate array storing results
                m_LayerMask = new NativeArray<uint>(m_ProbePositions.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                // Create acceleration structure
                m_AccelerationStructure = BuildAccelerationStructure(out m_HasTerrains);

                int batchSize = Mathf.Min(k_MaxProbeCountPerBatch, m_ProbePositions.Length);
                m_ProbePositionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, batchSize, Marshal.SizeOf<Vector3>());
                m_LayerMaskBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, batchSize, Marshal.SizeOf<uint>());
                m_ScratchBuffer = RayTracingHelper.CreateScratchBufferForBuildAndDispatch(m_AccelerationStructure.GetAccelerationStructure(), s_TracingContext.shaderRL, (uint)batchSize, 1, 1);

                int sobolBufferSize = (int)(SobolData.SobolDims * SobolData.SobolSize);
                m_SobolBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, sobolBufferSize, Marshal.SizeOf<uint>());
                m_SobolBuffer.SetData(SobolData.SobolMatrices);

                m_Cmd = new CommandBuffer();
                m_AccelerationStructure.Build(m_Cmd, ref m_ScratchBuffer);
                Graphics.ExecuteCommandBuffer(m_Cmd);
                m_Cmd.Clear();
            }

            static AccelStructAdapter BuildAccelerationStructure(out bool hasTerrains)
            {
                var accelStruct = s_TracingContext.CreateAccelerationStructure();
                var contributors = s_BakingBatch.contributors;

                foreach (var renderer in contributors.renderers)
                {
                    var mesh = renderer.component.GetComponent<MeshFilter>().sharedMesh;
                    if (mesh == null)
                        continue;

                    if (renderer.component is SkinnedMeshRenderer)
                        continue;

                    int subMeshCount = mesh.subMeshCount;
                    var matIndices = new uint[subMeshCount];
                    Array.Fill(matIndices, renderer.component.renderingLayerMask); // repurpose the material id as we don't need it here
                    var perSubMeshMask = new uint[subMeshCount];
                    Array.Fill(perSubMeshMask, GetInstanceMask(renderer.component.shadowCastingMode));
                    Span<bool> perSubMeshOpaqueness = stackalloc bool[subMeshCount];
                    perSubMeshOpaqueness.Fill(true);

                    accelStruct.AddInstance(EntityId.ToULong(renderer.component.GetEntityId()), renderer.component, perSubMeshMask, matIndices, perSubMeshOpaqueness, 1);
                }

                hasTerrains = false;
#if ENABLE_TERRAIN_MODULE
                foreach (var terrain in contributors.terrains)
                {
                    uint mask = GetInstanceMask(terrain.component.shadowCastingMode);
                    uint materialID = terrain.component.renderingLayerMask; // Using rendering layer as material ID (intentional)

                    hasTerrains = true;

                    ExtractTerrainData(terrain.component, out var heightData, out var heightmapResolution,
                        out var heightmapScale, out var holeData, out var holeResolution);

                    accelStruct.AddTerrainInstance(
                        EntityId.ToULong(terrain.component.GetEntityId()),
                        heightData,
                        heightmapResolution,
                        heightmapScale,
                        holeData,
                        holeResolution,
                        terrain.component.transform.localToWorldMatrix,
                        materialID,  // Using renderingLayerMask as materialID (intentional)
                        1,
                        mask);
                }
#endif

                return accelStruct;
            }

            public override bool Step()
            {
                if (currentStep >= stepCount)
                    return true;

                var shader = s_TracingContext.shaderRL;
                shader.SetKeyword(m_Cmd, shader.CreateLocalKeyword("TERRAIN_RAY_MARCHING_ENABLED"), m_HasTerrains);

                int batchOffset = m_BatchIndex * k_MaxProbeCountPerBatch;
                int batchSize = Mathf.Min(m_ProbePositions.Length - batchOffset, k_MaxProbeCountPerBatch);
                m_Cmd.SetBufferData(m_ProbePositionsBuffer, m_ProbePositions.GetSubArray(batchOffset, batchSize));

                m_AccelerationStructure.Bind(m_Cmd, "_AccelStruct", shader);
                if (m_HasTerrains)
                    m_AccelerationStructure.BindTerrainResources(m_Cmd, shader);
                shader.SetVectorParam(m_Cmd, _RenderingLayerMasks, m_RegionMasks);
                shader.SetBufferParam(m_Cmd, _ProbePositions, m_ProbePositionsBuffer);
                shader.SetBufferParam(m_Cmd, _LayerMasks, m_LayerMaskBuffer);
                shader.SetBufferParam(m_Cmd, _SobolBuffer, m_SobolBuffer);

                shader.Dispatch(m_Cmd, m_ScratchBuffer, (uint)batchSize, 1, 1);
                m_BatchIndex++;

                Graphics.ExecuteCommandBuffer(m_Cmd);
                m_Cmd.Clear();

                FetchResults(batchOffset, batchSize);

                return true;
            }

            void FetchResults(int batchOffset, int batchSize)
            {
                var batchLayers = m_LayerMask.GetSubArray(batchOffset, batchSize);
                var req = AsyncGPUReadback.RequestIntoNativeArray(ref batchLayers, m_LayerMaskBuffer, batchSize * sizeof(uint), 0);

                // TODO: use double buffering to hide readback latency
                req.WaitForCompletion();
            }

            public override void Dispose()
            {
                if (m_AccelerationStructure == null)
                    return;

                m_Cmd.Dispose();

                m_ScratchBuffer?.Dispose();
                m_ProbePositionsBuffer.Dispose();
                m_AccelerationStructure.Dispose();
                m_SobolBuffer?.Dispose();

                m_LayerMaskBuffer.Dispose();
                m_LayerMask.Dispose();
            }
        }
    }
}
