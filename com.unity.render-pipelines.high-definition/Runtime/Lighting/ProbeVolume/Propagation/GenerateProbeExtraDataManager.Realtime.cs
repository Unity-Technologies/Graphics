using System;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using Unity.Collections;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class ProbeDynamicGIManager
    {

        public class ExtraDataGenRTData
        {
            public RayTracingShader extraDataGenRT;
            public ComputeBufferHandle extraDataBuffer;
            public ComputeBufferHandle probePosBuffer;
            public RayTracingAccelerationStructure accelerationStructure;
            public Vector4 parameters;

            public ComputeBufferHandle debugRay;
            public ComputeBufferHandle debugOut;

        }

        RayTracingAccelerationStructure accelerationStructure = null;
        internal void SetRTAccelerationStructure(RayTracingAccelerationStructure structure)
        {
            accelerationStructure = structure;
        }

        internal void GenerateExtraDataRealtime(RenderGraph renderGraph, HDCamera hdCamera)
        {
            var buffersToProcess = ProbeReferenceVolume.instance.GetExtraDataBuffers();
            for (int i = 0; i < buffersToProcess.Count; ++i)
            {
                var buffer = buffersToProcess[i];

                if (buffer.finalExtraDataBuffer != null && buffer.probeCount > 0)
                    GenerateExtraData_RT(renderGraph, hdCamera, buffer);
            }
        }

        // ProbeExtraDataBuffers buffers
        void GenerateExtraData_RT(RenderGraph renderGraph, HDCamera hdCamera, ProbeExtraDataBuffers buffers)
        {
            var layerForRTDataGen = hdCamera.volumeStack.GetComponent<ProbeDynamicGI>().rtLayerMask.value;
            using (var builder = renderGraph.AddRenderPass<ExtraDataGenRTData>("ExtraDataGen RT", out var passData, ProfilingSampler.Get(HDProfileId.GenerateExtraData)))
            {
                passData.extraDataGenRT = m_GatherExtraDataRT;
                passData.probePosBuffer = renderGraph.ImportComputeBuffer(buffers.probeLocationBuffer);
                passData.extraDataBuffer = renderGraph.ImportComputeBuffer(buffers.finalExtraDataBuffer);
                passData.parameters = new Vector4(buffers.hitProbesAxisCount, ProbeReferenceVolume.instance.MinDistanceBetweenProbes(), layerForRTDataGen, buffers.hitProbesAxisCount + buffers.missProbesAxisCount);
                passData.accelerationStructure = accelerationStructure;

                passData.debugRay = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(buffers.hitProbesAxisCount + buffers.missProbesAxisCount, sizeof(float) * 4) { name = "DBG Ray" });
                passData.debugOut = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(buffers.hitProbesAxisCount + buffers.missProbesAxisCount, sizeof(float) * 4) { name = "DBG Out" });

                builder.SetRenderFunc(
                (ExtraDataGenRTData data, RenderGraphContext ctx) =>
                {
                    ctx.cmd.SetRayTracingShaderPass(data.extraDataGenRT, "ExtraDataGenDXR");

                    ctx.cmd.SetRayTracingAccelerationStructure(data.extraDataGenRT, HDShaderIDs._RaytracingAccelerationStructureName, data.accelerationStructure);
                    ctx.cmd.SetRayTracingBufferParam(data.extraDataGenRT, HDShaderIDs._PackedProbeExtraData, data.extraDataBuffer);
                    ctx.cmd.SetRayTracingBufferParam(data.extraDataGenRT, HDShaderIDs._ProbeWorldLocations, data.probePosBuffer);

                    ctx.cmd.SetRayTracingBufferParam(data.extraDataGenRT, "_DBGRAY", data.debugRay);
                    ctx.cmd.SetRayTracingBufferParam(data.extraDataGenRT, "_DBGOUT", data.debugOut);

                    ctx.cmd.SetRayTracingVectorArrayParam(data.extraDataGenRT, HDShaderIDs._RayAxis, ProbeDynamicGIManager.NeighbourAxis);

                    ctx.cmd.SetRayTracingVectorParam(data.extraDataGenRT, "_RTExtraDataGenParam", data.parameters);

                    uint numberOfRays = (uint)data.parameters.w;
                    ctx.cmd.DispatchRays(data.extraDataGenRT, "RayGenExtraData", numberOfRays, 1, 1);
                    // TODO!!!! This is only to test, after we should do the run only on a subset of probes around the camera.
                });
            }
        }

        // TODO_FCC: Mah, NOT needed for hackweek.
        void ReorderHits(ProbeExtraDataBuffers buffers)
        {
            // Read back the data.
            int totalPairings = (buffers.missProbesAxisCount + buffers.hitProbesAxisCount);
            int bufferEntries = totalPairings * 3;
            uint[] extraDataBuffer = new uint[bufferEntries];
            buffers.finalExtraDataBuffer.GetData(extraDataBuffer);

            List<uint> misses = new List<uint>(buffers.missProbesAxisCount * 3); // Undershoots one of the two, but at least one of the two will be fine.
            List<uint> hits = new List<uint>(buffers.hitProbesAxisCount * 3); // Undershoots one of the two, but at least one of the two will be fine.

            // Sort.
            for (int probeAxisPair = 0; probeAxisPair < totalPairings; ++probeAxisPair)
            {
                int entryInBuffer = probeAxisPair * 3 + 0;
                // To check if hit or miss, we just need to unpack the distance in the first index.
                uint albedoAndDistPacked = extraDataBuffer[entryInBuffer];
                float distance = ((albedoAndDistPacked >> 24) & 255) / 255.0f;
                if (distance > 0.0f)
                {
                    hits.Add(extraDataBuffer[entryInBuffer + 0]);
                    hits.Add(extraDataBuffer[entryInBuffer + 1]);
                    hits.Add(extraDataBuffer[entryInBuffer + 2]);
                }
                else
                {
                    misses.Add(extraDataBuffer[entryInBuffer + 0]);
                    misses.Add(extraDataBuffer[entryInBuffer + 1]);
                    misses.Add(extraDataBuffer[entryInBuffer + 2]);
                }
            }

            // Refit in the data.
            int entryInMainBuffer = 0;
            for (int hitIndex = 0; hitIndex < hits.Count; hitIndex += 3)
            {
                extraDataBuffer[entryInMainBuffer * 3 + 0] = hits[hitIndex + 0];
                extraDataBuffer[entryInMainBuffer * 3 + 1] = hits[hitIndex + 1];
                extraDataBuffer[entryInMainBuffer * 3 + 2] = hits[hitIndex + 2];

                entryInMainBuffer++;
            }
            buffers.hitProbesAxisCount = entryInMainBuffer;
            for (int missIndex = 0; missIndex < misses.Count; missIndex += 3)
            {
                extraDataBuffer[entryInMainBuffer * 3 + 0] = misses[missIndex + 0];
                extraDataBuffer[entryInMainBuffer * 3 + 1] = misses[missIndex + 1];
                extraDataBuffer[entryInMainBuffer * 3 + 2] = misses[missIndex + 2];

                entryInMainBuffer++;
            }
            buffers.missProbesAxisCount = entryInMainBuffer - buffers.hitProbesAxisCount;

            buffers.finalExtraDataBuffer.SetData(extraDataBuffer);
        }

    }
}
