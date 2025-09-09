using System;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering;
using UnityEngine.Rendering.UnifiedRayTracing;
using BuildFlags = UnityEngine.Rendering.UnifiedRayTracing.BuildFlags;

namespace UnityEngine.PathTracing.Integration
{
    internal class UVAccelerationStructure : IDisposable
    {
        internal IRayTracingAccelStruct _uvAS;
        private GraphicsBuffer _buildScratchBuffer;
        public void Dispose()
        {
            _uvAS?.Dispose();
            _uvAS = null;
            _buildScratchBuffer?.Dispose();
            _buildScratchBuffer = null;
        }

        public void Build(CommandBuffer commandBuffer, RayTracingContext rayTracingContext, UVMesh uvMesh, BuildFlags buildFlags)
        {
            var options = new AccelerationStructureOptions() { buildFlags = buildFlags };
            _uvAS = rayTracingContext.CreateAccelerationStructure(options);
            Debug.Assert(_uvAS is not null);

            for (int i = 0; i < uvMesh.Mesh.subMeshCount; ++i)
            {
                var instanceDesc = new MeshInstanceDesc(uvMesh.Mesh, i)
                {
                    mask = 0xFFFFFFFF,
                    instanceID = (uint)i
                };
                _uvAS.AddInstance(instanceDesc);
            }

            RayTracingHelper.ResizeScratchBufferForBuild(_uvAS, ref _buildScratchBuffer);
            _uvAS.Build(commandBuffer, _buildScratchBuffer);
        }
    }
}
