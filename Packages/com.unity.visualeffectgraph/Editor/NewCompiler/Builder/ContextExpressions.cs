using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;

namespace UnityEditor.VFX
{
    /// <summary>
    /// A context's expressions, resolved once so that the subtask code, the bindings declared on the task
    /// and the data bound onto it all agree on the name each expression is bound under.
    /// </summary>
    class ContextExpressions
    {
        // Expressions from the context's own slots are registered under this id, the ones from a block under
        // the block's index.
        const int k_ContextSlotId = -1;

        readonly Dictionary<IDataKey, VFXExpression> m_RequestedResources = new();
        readonly HashSet<VFXExpression> m_Resources = new();

        public VFXExpressionMapper GpuMapper { get; }

        public VFXExpressionMapper CpuMapper { get; }

        public VFXUniformMapper UniformMapper { get; }

        public ReadOnlyDictionary<VFXExpression, BufferType> BufferTypeUsage { get; }

        public ContextExpressions(VFXContext context, VFXExpressionGraph expressionGraph)
        {
            GpuMapper = expressionGraph.BuildGPUMapper(context);
            CpuMapper = expressionGraph.BuildCPUMapper(context);
            UniformMapper = new VFXUniformMapper(GpuMapper, false, false);
            BufferTypeUsage = expressionGraph.GetBufferUsage(context);

            // Asking the uniform mapper, which already sorted the expressions by kind, avoids going through
            // VFXExpression.valueType later: it is recomputed and allocates on every access.
            foreach (var texture in UniformMapper.textures)
                m_Resources.Add(texture);
            foreach (var buffer in UniformMapper.buffers)
                m_Resources.Add(buffer);
        }

        /// <summary>
        /// The expression behind each resource binding created so far, so the builder can bind their values
        /// onto the task. Only populated by <see cref="CreateResourceBinding"/>, so read it once the task is
        /// built.
        /// </summary>
        public IReadOnlyDictionary<IDataKey, VFXExpression> RequestedResources => m_RequestedResources;

        public string GetBindingName(VFXExpression expression) => UniformMapper.GetName(expression);

        public BufferType GetBufferType(VFXExpression expression)
            => BufferTypeUsage.TryGetValue(expression, out var bufferType) ? bufferType : default;

        /// <summary>
        /// Whether <paramref name="expression"/> is a GPU resource this context binds, i.e. whether it can be
        /// passed to <see cref="CreateResourceBinding"/>.
        /// </summary>
        public bool IsResource(VFXExpression expression) => expression != null && m_Resources.Contains(expression);

        /// <summary>
        /// Creates the binding declaring a GPU resource on a task, under <paramref name="key"/> or under the
        /// name the uniform mapper gave it. <paramref name="expression"/> must be a resource this context
        /// binds; check <see cref="IsResource"/> first if that isn't already known.
        /// </summary>
        public VfxResourceBinding CreateResourceBinding(VFXExpression expression, IDataKey key = null)
        {
            Debug.Assert(IsResource(expression));

            key ??= new NameDataKey(GetBindingName(expression));

            m_RequestedResources[key] = expression;
            return new VfxResourceBinding(key, GetBufferType(expression));
        }

        /// <summary>
        /// The bindings for the GPU resources fed by the context's own slots rather than by its blocks, keyed
        /// by slot name: that is the name the template refers to them by. A resource a block also uses still
        /// gets its own binding here, under the context's name for it.
        /// </summary>
        public IEnumerable<VfxResourceBinding> CreateContextResourceBindings()
        {
            foreach (var expression in UniformMapper.textures)
                foreach (var binding in CreateContextResourceBindings(expression))
                    yield return binding;

            foreach (var expression in UniformMapper.buffers)
                foreach (var binding in CreateContextResourceBindings(expression))
                    yield return binding;
        }

        /// <summary>
        /// The bindings for the parameters the context asks the runtime to resolve, such as a mesh output's
        /// mesh and submesh mask, keyed by slot name. These come from the CPU expressions, so none of them is
        /// read by the generated code, whatever their type says.
        /// </summary>
        public IEnumerable<VfxTaskParamBinding> CreateContextParamBindings()
        {
            foreach (var expression in CpuMapper.expressions)
            {
                foreach (var data in CpuMapper.GetData(expression))
                {
                    if (data.id != k_ContextSlotId)
                        continue;

                    yield return new VfxTaskParamBinding(new NameDataKey(data.name));
                }
            }
        }

        IEnumerable<VfxResourceBinding> CreateContextResourceBindings(VFXExpression expression)
        {
            foreach (var data in GpuMapper.GetData(expression))
            {
                if (data.id != k_ContextSlotId)
                    continue;

                yield return CreateResourceBinding(expression, new NameDataKey(data.name));
            }
        }

    }
}
