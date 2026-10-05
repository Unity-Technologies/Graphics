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
        public const int ContextSlotId = -1;

        readonly Dictionary<IDataKey, VFXExpression> m_RequestedResources = new();
        readonly HashSet<VFXExpression> m_Resources = new();

        public VFXExpressionMapper GpuMapper { get; }

        public VFXExpressionMapper CpuMapper { get; }

        public VFXUniformMapper UniformMapper { get; }

        public ReadOnlyDictionary<VFXExpression, BufferType> BufferTypeUsage { get; }

        public ContextExpressions(VFXContext context, VFXExpressionGraph expressionGraph)
            : this(expressionGraph.BuildGPUMapper(context), expressionGraph.BuildCPUMapper(context),
                expressionGraph.GetBufferUsage(context))
        {
        }

        ContextExpressions(VFXExpressionMapper gpuMapper, VFXExpressionMapper cpuMapper,
            ReadOnlyDictionary<VFXExpression, BufferType> bufferTypeUsage)
        {
            GpuMapper = gpuMapper;
            CpuMapper = cpuMapper;
            UniformMapper = new VFXUniformMapper(GpuMapper, false, false);
            BufferTypeUsage = bufferTypeUsage;

            foreach (var texture in UniformMapper.textures)
                m_Resources.Add(texture);
            foreach (var buffer in UniformMapper.buffers)
                m_Resources.Add(buffer);
        }

        public static ContextExpressions ForDerivedTask(VFXExpressionMapper sourceGpuMapper,
            ReadOnlyDictionary<VFXExpression, BufferType> bufferTypeUsage,
            IEnumerable<VFXNamedExpression> ownParameters)
        {
            var gpuMapper = new VFXExpressionMapper();

            foreach (var expression in sourceGpuMapper.expressions)
            {
                foreach (var data in sourceGpuMapper.GetData(expression))
                {
                    if (data.id != ContextSlotId)
                        gpuMapper.AddExpression(expression, data);
                }
            }

            foreach (var parameter in ownParameters)
                gpuMapper.AddExpression(parameter.exp, parameter.name, ContextSlotId);

            return new ContextExpressions(gpuMapper, new VFXExpressionMapper(), bufferTypeUsage);
        }

        public IReadOnlyDictionary<IDataKey, VFXExpression> RequestedResources => m_RequestedResources;

        public string GetBindingName(VFXExpression expression) => UniformMapper.GetName(expression);

        public BufferType GetBufferType(VFXExpression expression)
            => BufferTypeUsage.TryGetValue(expression, out var bufferType) ? bufferType : default;

        public bool IsResource(VFXExpression expression) => expression != null && m_Resources.Contains(expression);

        public VfxResourceBinding CreateResourceBinding(VFXExpression expression, IDataKey key = null)
        {
            Debug.Assert(IsResource(expression));

            key ??= new NameDataKey(GetBindingName(expression));

            m_RequestedResources[key] = expression;
            return new VfxResourceBinding(key, GetBufferType(expression));
        }

        public IEnumerable<VfxResourceBinding> CreateContextResourceBindings()
        {
            foreach (var expression in UniformMapper.textures)
                foreach (var binding in CreateContextResourceBindings(expression))
                    yield return binding;

            foreach (var expression in UniformMapper.buffers)
                foreach (var binding in CreateContextResourceBindings(expression))
                    yield return binding;
        }

        IEnumerable<VfxResourceBinding> CreateContextResourceBindings(VFXExpression expression)
        {
            foreach (var data in GpuMapper.GetData(expression))
            {
                if (data.id != ContextSlotId)
                    continue;

                yield return CreateResourceBinding(expression, new NameDataKey(data.name));
            }
        }

        public IEnumerable<VFXNamedExpression> EnumerateContextParameters()
        {
            foreach (var namedExpression in GpuMapper.CollectExpression(ContextSlotId))
            {
                if (VFXExpression.IsUniform(namedExpression.exp.valueType))
                    yield return namedExpression;
            }
        }

        public IEnumerable<VfxTaskParameterBinding> CreateContextParameterBindings()
        {
            foreach (var expression in CpuMapper.expressions)
            {
                foreach (var data in CpuMapper.GetData(expression))
                {
                    if (data.id != ContextSlotId)
                        continue;

                    yield return new VfxTaskParameterBinding(new NameDataKey(data.name));
                }
            }
        }
    }
}
