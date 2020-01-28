using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using System.Text;

namespace UnityEditor.VFX
{
    class VFXDataOutputEvent : VFXData
    {
        public override VFXDataType type => VFXDataType.OutputEvent;

        public override void CopySettings<T>(T dst)
        {
            //There is nothing serialized here
        }

        public override bool CanBeCompiled()
        {
            return m_Owners.Any(o => o.inputContexts.Any());
        }

        public override void FillDescs(
            List<VFXGPUBufferDesc> outBufferDescs,
            List<VFXTemporaryGPUBufferDesc> outTemporaryBufferDescs,
            List<VFXEditorSystemDesc> outSystemDescs,
            VFXExpressionGraph expressionGraph,
            Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData,
            Dictionary<VFXContext, int> contextSpawnToBufferIndex,
            VFXDependentBuffersData dependentBuffers,
            Dictionary<VFXContext, List<VFXContextLink>[]> effectiveFlowInputLinks,
            VFXSystemNames systemNames)
        {
            if (m_Contexts.Count == 0)
                throw new InvalidOperationException("VFXDataOutputEvent unexpected context count : " + m_Contexts.Count);

            if (m_Contexts.Any(o => o.contextType != VFXContextType.OutputEvent))
                throw new InvalidOperationException("VFXDataOutputEvent unexpected context type");

            var flowInputLinks = m_Contexts.SelectMany(context =>
            {
                if (effectiveFlowInputLinks.ContainsKey(context))
                {
                    var r = effectiveFlowInputLinks[context];
                    return r.SelectMany(o => o);
                }
                //A context could have been filtered out due because there isn't any flow input link
                return Enumerable.Empty<VFXContextLink>();
            });
            var inputSpawnerContext = flowInputLinks.Select(l => l.context);

            var systemBufferMappings = new List<VFXMapping>();
            foreach (var spawner in inputSpawnerContext)
            {
                if (spawner.contextType != VFXContextType.Spawner)
                    throw new InvalidOperationException("VFXDataOutputEvent unexpected link on Output event");

                systemBufferMappings.Add(new VFXMapping()
                {
                    name = "spawner_input",
                    index = contextSpawnToBufferIndex[spawner]
                });
            }

            string nativeName = string.Empty;
            if (systemNames != null)
                nativeName = systemNames.GetUniqueSystemName(this);
            else
                throw new InvalidOperationException("system names manager cannot be null");

            outSystemDescs.Add(new VFXEditorSystemDesc()
            {
                flags = VFXSystemFlag.SystemDefault,
                name = nativeName,
                buffers = systemBufferMappings.ToArray(),
                type = VFXSystemType.OutputEvent,
                layer = m_Layer
            });
        }

        public override void GenerateAttributeLayout(Dictionary<VFXContext, List<VFXContextLink>[]> effectiveFlowInputLinks)
        {
        }

        public override string GetAttributeDataDeclaration(VFXAttributeMode mode)
        {
            throw new NotImplementedException();
        }

        public override VFXDeviceTarget GetCompilationTarget(VFXContext context)
        {
            return VFXDeviceTarget.CPU;
        }

        public override string GetLoadAttributeCode(VFXAttribute attrib, VFXAttributeLocation location)
        {
            throw new NotImplementedException();
        }

        public override string GetStoreAttributeCode(VFXAttribute attrib, string value)
        {
            throw new NotImplementedException();
        }
    }
}
