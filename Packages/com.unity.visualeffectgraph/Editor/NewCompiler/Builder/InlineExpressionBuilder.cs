using System.Collections.Generic;
using System.Text;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Inlines an expression into generated shader code: writes its evaluation, declares on the task
    /// everything it names, and records the attributes it reads. This is the road for anything no one
    /// can work out before the shader runs - a block parameter driven by a particle attribute, a
    /// context parameter that is - whatever the code it lands in.
    /// </summary>
    class InlineExpressionBuilder
    {
        readonly ExpressionWriter m_ExpressionWriter = new();
        readonly Dictionary<VFXExpression, VfxResourceBinding> m_ScopeResources = new();

        // These two hashset are just for speed-up
        readonly HashSet<VFXExpression> m_WalkedResourceExpressions = new();
        readonly HashSet<VFXExpression> m_WalkedAttributeExpressions = new();

        ContextExpressions m_ContextExpressions;

        ContextExpressions ContextExpressions => m_ContextExpressions
            ?? throw new System.InvalidOperationException($"{nameof(Initialize)} was not called.");

        public void Initialize(ContextExpressions contextExpressions)
        {
            m_ContextExpressions = contextExpressions;
            m_ExpressionWriter.Initialize(contextExpressions.UniformMapper);
        }

        public void BeginScope()
        {
            m_ExpressionWriter.BeginScope();
            m_ScopeResources.Clear();
            m_WalkedResourceExpressions.Clear();
            m_WalkedAttributeExpressions.Clear();
        }

        public string Evaluate(StringBuilder code, VFXExpression expression)
        {
            m_ExpressionWriter.WriteExpressionEvaluation(code, expression);
            return m_ExpressionWriter.GetVariableValueString(expression);
        }

        public void RequestExpressionInputs(List<TemplatedTaskBinding> bindings, VFXExpression expression)
        {
            foreach (var parent in expression.parents)
            {
                if (!m_WalkedResourceExpressions.Add(parent))
                    continue;

                if (parent.IsAny(VFXExpression.Flags.NotCompilableOnCPU))
                {
                    RequestExpressionInputs(bindings, parent);
                    continue;
                }

                if (RequestResource(bindings, parent) == null)
                    RequestUniform(bindings, parent);
            }
        }

        public VfxResourceBinding RequestResource(List<TemplatedTaskBinding> bindings, VFXExpression expression)
        {
            if (m_ScopeResources.TryGetValue(expression, out var binding))
                return binding;

            if (!ContextExpressions.IsResource(expression))
                return null;

            binding = ContextExpressions.CreateResourceBinding(expression);
            m_ScopeResources.Add(expression, binding);
            AddBindingIfNew(bindings, binding);
            return binding;
        }

        public void RegisterReadAttributes(AttributeSet attributeSet, AttributeSet sourceAttributeSet,
            VFXExpression expression)
        {
            // An attribute expression is per element, and that flag propagates to everything built on top of
            // it, so a subexpression without it cannot hold one and the whole subtree can be skipped.
            if (!expression.IsAny(VFXExpression.Flags.NotCompilableOnCPU))
                return;

            if (!m_WalkedAttributeExpressions.Add(expression))
                return;

            if (expression is VFXAttributeExpression attributeExpression)
            {
                var target = attributeExpression.attributeLocation == VFXAttributeLocation.Source
                    ? sourceAttributeSet
                    : attributeSet;
                target.AddAttribute(VFXAttributesManager.ConvertToNewCompiler(attributeExpression.attribute),
                    AttributeUsage.Read);
            }

            foreach (var expressionParent in expression.parents)
            {
                RegisterReadAttributes(attributeSet, sourceAttributeSet, expressionParent);
            }
        }

        public void RequestUniform(List<TemplatedTaskBinding> bindings, VFXExpression expression)
        {
            // Patched straight into the generated code, so it names nothing.
            if (expression.Is(VFXExpression.Flags.Constant))
                return;

            var valueType = expression.valueType;
            if (!VFXExpression.IsUniform(valueType) || !ContextExpressions.UniformMapper.Contains(expression))
                return;

            var dataDescriptionType = ValueData.Create(VFXExpression.TypeToType(valueType)).GetType();
            var binding = TemplatedTaskBinding<IDataDescription>.Create(
                dataDescriptionType, new NameDataKey(ContextExpressions.GetBindingName(expression)));
            binding.ReadPathSet.Add(DataPath.Root);
            AddBindingIfNew(bindings, binding);
        }

        static void AddBindingIfNew(List<TemplatedTaskBinding> bindings, TemplatedTaskBinding binding)
        {
            foreach (var existing in bindings)
            {
                if (existing.Key.Equals(binding.Key))
                    return;
            }
            bindings.Add(binding);
        }
    }

    class ExpressionWriter
    {
        Dictionary<VFXExpression, string> m_InputVariableValueStrings = new();
        Dictionary<VFXExpression, string> m_GeneratedVariableValueStrings = new();
        uint m_TmpCounter = 0;

        public void Initialize(VFXUniformMapper uniformMapper)
        {
            m_InputVariableValueStrings.Clear();
            m_GeneratedVariableValueStrings.Clear();
            foreach (var uniform in uniformMapper.uniforms)
            {
                AddInputVariableValueString(uniform, uniform.Is(VFXExpression.Flags.Constant) ? uniform.GetCodeString(null) : uniformMapper.GetName(uniform));
            }
            foreach (var texture in uniformMapper.textures)
            {
                AddInputVariableValueString(texture, uniformMapper.GetName(texture));
            }
            foreach (var buffer in uniformMapper.buffers)
            {
                AddInputVariableValueString(buffer, uniformMapper.GetName(buffer));
            }
        }

        public void BeginScope()
        {
            m_GeneratedVariableValueStrings.Clear();
            m_TmpCounter = 0;
        }

        public string GetVariableValueString(VFXExpression expression)
        {
            if(m_InputVariableValueStrings.TryGetValue(expression, out string value))
                return value;
            if(m_GeneratedVariableValueStrings.TryGetValue(expression, out value))
                return value;
            Debug.Assert(false, $"Expression {expression} has not been written yet.");
            return null;
        }

        public void WriteExpressionEvaluation(StringBuilder codeBuilder, VFXExpression exp)
        {
            if (HasVariableValueString(exp)) // Expression value is already assigned within the scope.
                return;

            if (exp is VFXAttributeExpression attributeExpression)
            {
                // Current attributes live in a local the blocks read and write, so the expression has to go
                // through that local rather than through the struct, which is only up to date on entry.
                AddGeneratedVariableValueString(exp, attributeExpression.attributeLocation == VFXAttributeLocation.Current
                    ? attributeExpression.attributeName
                    : attributeExpression.GetCodeString(null));
                return;
            }

            string entry;
            if (exp.Is(VFXExpression.Flags.Constant))
                entry = exp.GetCodeString(null); // Patch constant directly
            else
            {
                foreach (var parent in exp.parents)
                    WriteExpressionEvaluation(codeBuilder, parent);
                string[] parents = new string[exp.parents.Length];
                for(int i = 0; i < exp.parents.Length; ++i)
                {
                    parents[i] = GetVariableValueString(exp.parents[i]);
                }

                string value = exp.GetCodeString(parents);
                entry = "tmp_" + VFXCodeGeneratorHelper.GeneratePrefix(m_TmpCounter++);
                WriteVariable(codeBuilder, exp.valueType, entry, value);

                codeBuilder.Append('\n');
            }
            AddGeneratedVariableValueString(exp, entry);
        }

        void WriteVariable(StringBuilder codeBuilder, VFXValueType type, string variableName, string value)
        {
            if (!VFXExpression.IsTypeValidOnGPU(type))
                return;

            string typeStr = VFXExpression.TypeToCode(type);
            codeBuilder.Append($"{typeStr} {variableName} = {value};\n");
        }

        void AddInputVariableValueString(VFXExpression uniform, string entry)
        {
            m_InputVariableValueStrings[uniform] = entry;
        }

        void AddGeneratedVariableValueString(VFXExpression expression, string entry)
        {
            m_GeneratedVariableValueStrings[expression] = entry;
        }

        bool HasVariableValueString(VFXExpression expression)
        {
            return m_GeneratedVariableValueStrings.ContainsKey(expression) ||
                   m_InputVariableValueStrings.ContainsKey(expression);
        }
    }
}
