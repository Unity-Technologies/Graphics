using System;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine.Pool;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Binds the concrete buffers onto each particle system's system task, under the
    /// <see cref="ParticleSystemTask.BufferKeys"/> names the runtime looks them up by. The system task keeps
    /// its logical bindings until the templated tasks are lowered, so this runs while both forms coexist.
    /// </summary>
    class SystemTaskBindingsPass : CompilationPass
    {
        public bool Execute(ref CompilationContext context)
        {
            var particleSystemContainer = context.data.Get<ParticleSystemContainer>();
            var concretization = context.data.Get<DataConcretizationResult>();

            foreach (var system in particleSystemContainer)
            {
                var systemTaskNode = context.graph.TaskNodes[system.SystemTask.Id];

                if (system.SystemFlags.HasFlag(UnityEngine.VFX.VFXSystemFlag.SystemReceivedEventGPU))
                    BindGpuEventList(system, systemTaskNode, concretization, ref context);

                // Snapshotted: binding below adds to the very sublist being walked.
                using var bindingIds = systemTaskNode.DataBindings.Snapshot();

                using (ListPool<DataNodeId>.Get(out var parentNodes))
                {
                    foreach (var bindingId in bindingIds)
                    {
                        var binding = context.graph.DataBindings[bindingId];
                        foreach (var dataView in binding.DataNode.UsedDataViews)
                        {
                            var bindingKey = BufferKeyFor(binding.BindingDataKey, dataView);
                            if (bindingKey == null)
                                continue;
                            if (!concretization.TryGetConcreteDataView(dataView.Id, out var concreteRootViewId))
                                continue;

                            concretization.CollectConcreteParents(binding.DataNode, dataView.Id, parentNodes);

                            var newDataNodeId = context.graph.BindData(
                                systemTaskNode.Id, bindingKey, concreteRootViewId, parentNodes);
                            concretization.AddDataNodeMapping(binding.DataNode.Id, dataView.Id, newDataNodeId);
                        }
                    }
                }
            }

            return true;
        }

        // A GPU event list is read by the init task, not by the system task, so it is picked up from there.
        static void BindGpuEventList(ParticleSystemContainer.ParticleSystem system,
            TaskNode systemTaskNode, DataConcretizationResult concretization, ref CompilationContext context)
        {
            foreach (var task in system.Tasks)
            {
                if (task.TaskType != UnityEngine.VFX.VFXTaskType.Initialize)
                    continue;

                foreach (var binding in context.graph.TaskNodes[task.Id].DataBindings)
                {
                    if (binding.DataView.DataDescription is not EventListData eventListData || eventListData.IsCpu)
                        continue;
                    if (!concretization.TryGetConcreteDataView(binding.DataView.Id, out var concreteRootViewId))
                        continue;

                    // A GPU buffer is written by the system task, so it depends on nothing here.
                    context.graph.BindData(systemTaskNode.Id, ParticleSystemTask.BufferKeys.EventListBufferKey,
                        concreteRootViewId, Array.Empty<DataNodeId>());
                }
                break;
            }
        }

        static IDataKey BufferKeyFor(IDataKey logicalBindingKey, DataView dataView)
        {
            switch (dataView.DataDescription)
            {
                // The same description reaches the system task through two different logical bindings:
                // the particle's own attributes, and the source attributes it spawns from.
                case AttributeData when logicalBindingKey == ParticleSystemTask.CpuEventListBindingKey:
                    return ParticleSystemTask.BufferKeys.SourceAttributeBufferKey;
                case AttributeData when logicalBindingKey == ParticleSystemTask.ParticleDataBindingKey:
                    return ParticleSystemTask.BufferKeys.AttributeBufferKey;
                case StructuredData:
                    return ParticleSystemTask.BufferKeys.GraphValuesBufferKey;
                case DeadListData:
                    return ParticleSystemTask.BufferKeys.DeadListBufferKey;
                case EventListData eventListData when eventListData.IsCpu:
                    return ParticleSystemTask.BufferKeys.SpawnBufferKey;
                case EventData:
                    return ParticleSystemTask.BufferKeys.SpawnerInputBufferKey;
                default:
                    return null;
            }
        }
    }
}
