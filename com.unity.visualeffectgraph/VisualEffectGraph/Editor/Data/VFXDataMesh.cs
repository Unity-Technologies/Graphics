using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXDataMesh : VFXData
    {
        public Shader shader;

        public override VFXDataType type { get { return VFXDataType.kMesh; } }

        public override void CopySettings<T>(T dst) {}

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
                mappings.Add(new VFXMapping(expressionGraph.GetFlattenedIndex(uniform), contextData.uniformMapper.GetName(uniform)));

            var task = new VFXTaskDesc()
            {
                processor = shader,
                values = mappings.ToArray(),
                type = VFXTaskType.kOutput
            };

            mappings.Clear();
            var mapper = contextData.cpuMapper;

            // TODO Factorize that
            var meshExp = mapper.FromNameAndId("mesh", -1);
            var transformExp = mapper.FromNameAndId("transform", -1);

            int meshIndex = meshExp != null ? expressionGraph.GetFlattenedIndex(meshExp) : -1;
            int transformIndex = transformExp != null ? expressionGraph.GetFlattenedIndex(transformExp) : -1;

            if (meshIndex != -1)
                mappings.Add(new VFXMapping(meshIndex, "mesh"));
            if (transformIndex != -1)
                mappings.Add(new VFXMapping(transformIndex, "transform"));

            outSystemDescs.Add(new VFXSystemDesc()
            {
                tasks = new VFXTaskDesc[1] { task },
                values = mappings.ToArray(),
                type = VFXSystemType.kVFXMesh,
            });
        }
    }
}
