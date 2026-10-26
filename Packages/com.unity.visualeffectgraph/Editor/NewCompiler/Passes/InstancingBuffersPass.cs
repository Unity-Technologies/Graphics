using System;
using Unity.GraphCommon.LowLevel.Editor;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    class InstancingBuffersPass : CompilationPass
    {
        public bool Execute(ref CompilationContext context)
        {
            var particleSystemContainer = context.data.Get<ParticleSystemContainer>();
            var instancingSplitDescContainer = context.data.GetOrCreate<InstancingSplitDescContainer>();

            foreach (var system in particleSystemContainer)
            {
                var splitValueSets = instancingSplitDescContainer.GetOrCreate(system);
                AddInstanceSplitDesc(splitValueSets, new List<DataNodeId>());

                for (int i = 0; i < system.Tasks.Count; i++)
                {
                    var taskNode = context.graph.TaskNodes[system.Tasks[i].Id];
                    var splitValuesNodes = GenerateInstancingSplitDataNodes(taskNode);
                    particleSystemContainer.SetInstanceSplitIndex(system, i, AddInstanceSplitDesc(splitValueSets, splitValuesNodes));
                }

                var systemTaskNode = context.graph.TaskNodes[system.SystemTask.Id];
                VFXGPUBufferDesc desc = new VFXGPUBufferDesc()
                    {
                        debugName = $"VFXInstancesIndirectionBuffer_{systemTaskNode.Name}",
                        target = GraphicsBuffer.Target.Structured,
                        size = 1u + (uint)splitValueSets.Count,
                        stride = 4,
                        mode = ComputeBufferMode.Dynamic
                    };
                VfxGpuBufferData instancingIndirectAndActiveIndirect = new VfxGpuBufferData(desc);
                var instancingId = context.graph.AddData($" {systemTaskNode.Name}_instancingIndirectAndActiveIndirect", instancingIndirectAndActiveIndirect);

                DataViewId prefixSumId = DataViewId.Invalid;
                if (system.Parent == null)
                {
                    desc = new VFXGPUBufferDesc()
                    {
                        debugName = $"VFXInstancesPrefixSumBuffer_{systemTaskNode.Name}",
                        target = GraphicsBuffer.Target.Structured,
                        size = (uint)splitValueSets.Count + 1u,
                        stride = 4,
                        mode = ComputeBufferMode.Dynamic
                    };
                    VfxGpuBufferData instancingPrefixSum = new VfxGpuBufferData(desc);
                    prefixSumId = context.graph.AddData($" {systemTaskNode.Name}_instancingPrefixSum", instancingPrefixSum);
                }
                else
                {
                    // GPU-event child systems have no CPU-driven spawn prefix sum. Reuse the GPU
                    // event buffer already bound as EventListBufferKey: the native runtime fills its
                    // own per-instance prefix sum section (see VFXGetEventListBufferPrefixSumIndex).
                    foreach (var binding in systemTaskNode.DataBindings)
                    {
                        if (binding.BindingDataKey.Equals(ParticleSystemTask.BufferKeys.EventListBufferKey))
                        {
                            prefixSumId = binding.DataView.Id;
                            break;
                        }
                    }
                }


                DataNodeId srcInstancingDataNode = context.graph.BindData(systemTaskNode.Id, ParticleSystemTask.BufferKeys.InstancingBufferKey, instancingId, Array.Empty<DataNodeId>(), BindingUsage.Write);
                DataNodeId[] prefixSumDataNode = Array.Empty<DataNodeId>();
                if (prefixSumId.IsValid)
                {
                    var dataNode = context.graph.BindData(systemTaskNode.Id, ParticleSystemTask.BufferKeys.InstancingPrefixSumBufferKey, prefixSumId,
                        Array.Empty<DataNodeId>(), BindingUsage.Write);
                    prefixSumDataNode = new [] { dataNode };
                }

                var parentInstancingNode = new[] { srcInstancingDataNode };

                foreach (var task in system.Tasks)
                {
                    // A task with no generated code has nothing to read these from, and the runtime
                    // resolves its buffers and parameters by position: an extra one would shift them.
                    if (context.graph.TaskNodes[task.Id].Task is VfxTask { HasShaderSource: false })
                        continue;

                    context.graph.BindData(task.Id, VfxTask.InstancingBufferKey, instancingId, parentInstancingNode, BindingUsage.Read);
                    if(prefixSumId.IsValid)
                        context.graph.BindData(task.Id, VfxTask.InstancingPrefixSumBufferKey, prefixSumId, prefixSumDataNode, BindingUsage.Read);
                }
            }


            return true;
        }

        static VFXExpression GetProducedExpression(ITask task) => task switch
        {
            LegacyExpressionTask legacyExpressionTask => legacyExpressionTask.Expression,
            ExternalSetTask externalSetTask => externalSetTask.Expression,
            _ => null
        };

        List<DataNodeId> GenerateInstancingSplitDataNodes(TaskNode taskNode)
        {
            List<DataNodeId> instancingSplitDescValues = new List<DataNodeId>();
            foreach (var dataNode in taskNode.DataNodes)
            {
                foreach (var dataNodeParent in dataNode.Parents)
                {
                    var expression = GetProducedExpression(dataNodeParent.TaskNode.Task);
                    if (expression != null
                        && (VFXExpression.IsTexture(expression.valueType) || VFXExpression.IsBufferOnGPU(expression.valueType))
                        && !expression.IsAny(VFXExpression.Flags.Constant | VFXExpression.Flags.Foldable))
                    {
                        instancingSplitDescValues.Add(dataNode.Id);
                    }
                }
            }
            instancingSplitDescValues.Sort((a, b) => a.Index.CompareTo(b.Index));
            return instancingSplitDescValues;
        }

        // Finds the split whose values match splitValues, or appends a new one. Mirrors
        // VFXDataParticle.AddInstanceSplitDesc, deduplicating by the (sorted) set of values.
        static uint AddInstanceSplitDesc(List<List<DataNodeId>> instanceSplitDescs, List<DataNodeId> splitValues)
        {
            for (int i = 0; i < instanceSplitDescs.Count; ++i)
            {
                if (AreSplitValuesEqual(splitValues, instanceSplitDescs[i]))
                    return (uint)i;
            }

            instanceSplitDescs.Add(splitValues);
            return (uint)(instanceSplitDescs.Count - 1);
        }

        static bool AreSplitValuesEqual(List<DataNodeId> a, List<DataNodeId> b)
        {
            if (a.Count != b.Count)
                return false;

            for (int i = 0; i < a.Count; ++i)
            {
                if (!a[i].Equals(b[i]))
                    return false;
            }

            return true;
        }
    }

    // Holds, per particle system, the distinct sets of data node ids (texture/buffer expressions) that
    // require a separate instancing batch. Resolved into VFXInstanceSplitDesc expression indices by
    // OutputPass, once expression indices are known, via m_ValuesExpressionIndices.
    class InstancingSplitDescContainer
    {
        readonly Dictionary<ParticleSystemContainer.ParticleSystem, List<List<DataNodeId>>> m_SplitValuesPerSystem = new();

        public List<List<DataNodeId>> GetOrCreate(ParticleSystemContainer.ParticleSystem system)
        {
            if (!m_SplitValuesPerSystem.TryGetValue(system, out var splitValues))
            {
                splitValues = new List<List<DataNodeId>>();
                m_SplitValuesPerSystem.Add(system, splitValues);
            }
            return splitValues;
        }

        public List<List<DataNodeId>> Find(ParticleSystemContainer.ParticleSystem system)
        {
            m_SplitValuesPerSystem.TryGetValue(system, out var splitValues);
            return splitValues;
        }
    }

}
