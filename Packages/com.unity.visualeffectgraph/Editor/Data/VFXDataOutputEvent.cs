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

        //Shortcut to per context value
        public string eventName
        {
            get
            {
                if (m_Contexts.Any())
                {
                    var setting = m_Contexts.First().GetSetting("eventName");
                    return (string)setting.value;
                }
                return null;
            }
        }

        public override void FillDescs(
            VFXCompileErrorReporter reporter,
            List<VFXGPUBufferDesc> outBufferDescs,
            List<VFXTemporaryGPUBufferDesc> outTemporaryBufferDescs,
            List<VFXEditorSystemDesc> outSystemDescs,
            VFXExpressionGraph expressionGraph,
            Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData,
            Dictionary<VFXContext, int> contextSpawnToBufferIndex,
            VFXDependentBuffersData dependentBuffers,
            Dictionary<VFXContext, List<VFXContextLink>[]> effectiveFlowInputLinks,
            Dictionary<VFXData, uint> dataToSystemIndex,
            VFXSystemNames systemNames)
        {
            if (m_Contexts.Count != 1)
                throw new InvalidOperationException("VFXDataOutputEvent unexpected context count : " + m_Contexts.Count);

            if (m_Contexts.Any(o => o.contextType != VFXContextType.OutputEvent))
                throw new InvalidOperationException("VFXDataOutputEvent unexpected context type");

            var nativeName = eventName;
            if (outSystemDescs.Any(o => o.name == nativeName && o.type == VFXSystemType.OutputEvent))
            {
                //Check if already processed, it already present, this VFXDataOutputEvent has been gather with previous entry.
                return;
            }

            var allMatchingVFXOutputEvent = contextToCompiledData.Keys.Where(context =>
            {
                if (context.contextType == VFXContextType.OutputEvent)
                {
                    if (((VFXDataOutputEvent)context.GetData()).eventName == nativeName)
                    {
                        return true;
                    }
                }
                return false;
            }).ToArray();

            var allMatchingVFXDataOutputEvent = allMatchingVFXOutputEvent.Select(o => o.GetData()).Cast<VFXDataOutputEvent>().ToArray();
            var flowInputLinks = allMatchingVFXDataOutputEvent.SelectMany(data => data.m_Contexts.SelectMany(context =>
            {
                if (effectiveFlowInputLinks.ContainsKey(context))
                {
                    var r = effectiveFlowInputLinks[context];
                    return r.SelectMany(o => o);
                }
                //A context could have been filtered out due because there isn't any flow input link
                return Enumerable.Empty<VFXContextLink>();
            }));
            var inputSpawnerContext = flowInputLinks.Select(l => l.context).Distinct();

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
