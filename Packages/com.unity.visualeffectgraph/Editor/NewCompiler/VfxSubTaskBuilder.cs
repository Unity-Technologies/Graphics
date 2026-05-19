using System.Collections.Generic;
using System.Text;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VfxSubTaskBuilder
    {
        private ExpressionWriter m_ExpressionWriter = new ExpressionWriter();
        private IDataKey m_DefaultAttributeKey;
        private IDataKey m_SourceAttributeKey;

        public VfxSubTaskBuilder(IDataKey defaultAttributeKey, IDataKey sourceAttributeKey)
        {
            m_DefaultAttributeKey = defaultAttributeKey;
            m_SourceAttributeKey = sourceAttributeKey;
        }

        public List<SubtaskDescription> GenerateSubtasks(VFXContext context, VFXExpressionGraph expressionGraph)
        {
            List<SubtaskDescription> subtaskDescriptions = new List<SubtaskDescription>();
            var gpuMapper = expressionGraph.BuildGPUMapper(context);
            var uniformMapper = new VFXUniformMapper(gpuMapper, false, false);
            m_ExpressionWriter.Initialize(uniformMapper);

            int blockIndex = 0;
            foreach (var block in context.activeFlattenedChildrenWithImplicit)
            {
                m_ExpressionWriter.StartBlock();
                var subTaskDesc = new SubtaskDescription();
                subTaskDesc.Name = block.name;
                subTaskDesc.ExpressionBindingKeys = new List<IDataKey>();

                StringBuilder blockCodeBuilder = new StringBuilder(block.source);
                StringBuilder expressionCodeBuilder = new StringBuilder();

                var defaultAttributeSet = new AttributeSet();
                var sourceAttributeSet = new AttributeSet();

                foreach (var attributeInfo in block.attributes)
                {
                    var attribute = VFXAttributesManager.ConvertToNewCompiler(attributeInfo.attrib);
                    AttributeUsage usage = GetAttributeUsage(attributeInfo.mode);
                    defaultAttributeSet.AddAttribute(attribute, usage);
                    blockCodeBuilder.Replace(attributeInfo.attrib.name, "attributes." + attributeInfo.attrib.name);
                }

                // Generate parameters assignments and register attributes used by expressions
                foreach (var parameter in block.parameters)
                {
                    var reduced = gpuMapper.FromNameAndId(parameter.name, blockIndex);
                    if (!VFXExpression.IsUniform(reduced.valueType)) // TODO: We'll need to handle buffers and textures as block inputs later.
                        continue;

                    string leftHandSide = $"{VFXExpression.TypeToCode(reduced.valueType)} {parameter.name}";
                    string rightHandSide;
                    if (reduced.IsAny(VFXExpression.Flags.NotCompilableOnCPU))
                    {
                        m_ExpressionWriter.WriteExpressionEvaluation(expressionCodeBuilder, reduced);
                        rightHandSide = m_ExpressionWriter.GetVariableValueString(reduced);
                    }
                    else
                    {
                        string bindingName = uniformMapper.GetName(reduced);
                        subTaskDesc.ExpressionBindingKeys.Add(new NameDataKey(bindingName));
                        rightHandSide = reduced.Is(VFXExpression.Flags.Constant) ? reduced.GetCodeString(null) : bindingName;
                    }
                    expressionCodeBuilder.Append($"{leftHandSide} = {rightHandSide};\n");

                    RegisterBlockAttributesFromExpression(defaultAttributeSet, sourceAttributeSet, reduced);
                }

                // Workaround to handle RAND which implicitly uses the seed attribute
                if(block.source.Contains("RAND"))
                    blockCodeBuilder.Insert(0, "uint seed = attributes.seed;\n");

                blockCodeBuilder.Insert(0, expressionCodeBuilder.ToString());

                Dictionary<IDataKey, AttributeSet> attributeSets = new Dictionary<IDataKey, AttributeSet>();
                attributeSets.Add(m_DefaultAttributeKey, defaultAttributeSet);
                if (!sourceAttributeSet.IsEmpty())
                {
                    attributeSets.Add(m_SourceAttributeKey, sourceAttributeSet);
                }

                subTaskDesc.Task = new TemplateSubtask(block.name, blockCodeBuilder.ToString(), attributeSets);

                subtaskDescriptions.Add(subTaskDesc);
                blockIndex++;
            }
            return subtaskDescriptions;
        }

        void RegisterBlockAttributesFromExpression(AttributeSet defaultAttributeSet, AttributeSet sourceAttributeSet, VFXExpression expression)
        {
            if (expression is VFXAttributeExpression attributeExpression)
            {
                var attributeSet = attributeExpression.attributeLocation == VFXAttributeLocation.Source ? sourceAttributeSet : defaultAttributeSet;
                attributeSet.AddAttribute(VFXAttributesManager.ConvertToNewCompiler(attributeExpression.attribute), GetAttributeUsage(VFXAttributeMode.Read));
            }

            foreach (var expressionParent in expression.parents)
            {
                RegisterBlockAttributesFromExpression(defaultAttributeSet, sourceAttributeSet, expressionParent);
            }
        }

        AttributeUsage GetAttributeUsage(VFXAttributeMode mode)
        {
            AttributeUsage usage = 0;
            if (mode.HasFlag(VFXAttributeMode.Read))
                usage |= AttributeUsage.Read;
            if (mode.HasFlag(VFXAttributeMode.Write))
                usage |= AttributeUsage.Write;
            return usage;
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

        public void StartBlock()
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
