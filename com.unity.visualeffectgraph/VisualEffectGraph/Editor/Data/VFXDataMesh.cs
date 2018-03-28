using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXDataMesh : VFXData
    {
        public Shader shader;

        public override VFXDataType type { get { return VFXDataType.kMesh; } }

        public override void CopySettings<T>(T dst)
        {
            VFXDataMesh other = dst as VFXDataMesh;
            if (other != null)
                other.shader = shader;
        }

        public override VFXDeviceTarget GetCompilationTarget(VFXContext context)
        {
            return VFXDeviceTarget.GPU;
        }

        public override bool CanBeCompiled()
        {
            return shader != null && m_Owners.Count == 1;
        }

        public override void FillDescs(
            List<VFXGPUBufferDesc> outBufferDescs,
            List<VFXSystemDesc> outSystemDescs,
            VFXExpressionGraph expressionGraph,
            Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData,
            Dictionary<VFXContext, int> contextSpawnToBufferIndex)
        {
            var context = m_Owners[0];
            var contextData = contextToCompiledData[context];

            var mappings = new List<VFXMapping>();
            foreach (var uniform in contextData.uniformMapper.uniforms.Concat(contextData.uniformMapper.textures))
                mappings.Add(new VFXMapping(contextData.uniformMapper.GetName(uniform), expressionGraph.GetFlattenedIndex(uniform)));

            var task = new VFXTaskDesc()
            {
                processor = shader,
                values = mappings.ToArray(),
                type = VFXTaskType.Output
            };

            mappings.Clear();
            var mapper = contextData.cpuMapper;

            // TODO Factorize that
            var meshExp = mapper.FromNameAndId("mesh", -1);
            var transformExp = mapper.FromNameAndId("transform", -1);
            var subMaskExp = mapper.FromNameAndId("subMeshMask", -1);

            int meshIndex = meshExp != null ? expressionGraph.GetFlattenedIndex(meshExp) : -1;
            int transformIndex = transformExp != null ? expressionGraph.GetFlattenedIndex(transformExp) : -1;
            int subMaskIndex = subMaskExp != null ? expressionGraph.GetFlattenedIndex(subMaskExp) : -1;

            if (meshIndex != -1)
                mappings.Add(new VFXMapping("mesh", meshIndex));
            if (transformIndex != -1)
                mappings.Add(new VFXMapping("transform", transformIndex));
            if (subMaskIndex != -1)
                mappings.Add(new VFXMapping("subMeshMask", subMaskIndex));

            outSystemDescs.Add(new VFXSystemDesc()
            {
                tasks = new VFXTaskDesc[1] { task },
                values = mappings.ToArray(),
                type = VFXSystemType.Mesh,
                layer = uint.MaxValue,
            });
        }
    }
}
