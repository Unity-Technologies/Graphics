using System.Collections.Generic;
using System.Text;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class SubtaskBuilder
    {
        readonly InlineExpressionBuilder m_InlineExpressions = new();

        struct SubtaskState
        {
            public SubtaskDescription Description;
            public StringBuilder Code;
            public AttributeSet Attributes;
            public AttributeSet SourceAttributes;
        }

        public void GenerateSubtasks(VFXContext context, ContextExpressions contextExpressions, VfxTemplatedTask.Args args)
        {
            m_InlineExpressions.Initialize(contextExpressions);
            GenerateBlockSubtasks(context, contextExpressions, args.BaseArgs.Subtasks);
            GenerateContextParameterSubtasks(contextExpressions, args);
        }

        void GenerateBlockSubtasks(VFXContext context, ContextExpressions contextExpressions, List<SubtaskDescription> subtaskDescriptions)
        {
            var gpuMapper = contextExpressions.GpuMapper;

            int blockIndex = 0;
            foreach (var block in context.activeFlattenedChildrenWithImplicit)
            {
                var state = BeginSubtask(block.name);

                foreach (var attributeInfo in block.attributes)
                {
                    var attribute = VFXAttributesManager.ConvertToNewCompiler(attributeInfo.attrib);
                    var usage = VFXAttributesManager.ConvertToNewCompiler(attributeInfo.mode);
                    state.Attributes.AddAttribute(attribute, usage);
                }

                // Workaround to handle RAND which implicitly uses the seed attribute
                if (block.source != null && block.source.Contains("RAND"))
                {
                    state.Attributes.AddAttribute(VFXAttributesManager.ConvertToNewCompiler(VFXAttribute.Seed), AttributeUsage.Read | AttributeUsage.Write);
                }

                foreach (var parameter in block.parameters)
                {
                    var reduced = gpuMapper.FromNameAndId(parameter.name, blockIndex);
                    Debug.Assert(reduced != null);
                    var reducedValueType = reduced.valueType;

                    if (VFXExpression.IsUniform(reducedValueType))
                    {
                        WriteValueParameter(ref state, $"{VFXExpression.TypeToCode(reducedValueType)} {parameter.name}", reduced, contextExpressions);
                    }
                    else
                    {
                        WriteResourceParameter(ref state, parameter.name, reduced, reducedValueType);
                        m_InlineExpressions.RegisterReadAttributes(state.Attributes, state.SourceAttributes, reduced);
                    }
                }

                state.Code.Append(block.source);
                subtaskDescriptions.Add(EndSubtask(state));
                blockIndex++;
            }
        }

        void GenerateContextParameterSubtasks(ContextExpressions contextExpressions, VfxTemplatedTask.Args args)
        {
            foreach (var parameter in contextExpressions.EnumerateContextParameters())
            {
                args.ContextParameters.Add(new VfxTemplatedTask.ContextParameter(
                    VFXExpression.TypeToCode(parameter.exp.valueType), parameter.name));

                var state = BeginSubtask($"Context parameter | {parameter.name}");
                WriteValueParameter(ref state, VfxContextParametersHlsl.Member(parameter.name), parameter.exp, contextExpressions);
                args.BaseArgs.Subtasks.Add(EndSubtask(state));
            }
        }

        SubtaskState BeginSubtask(string name)
        {
            m_InlineExpressions.BeginScope();
            return new SubtaskState
            {
                Description = new SubtaskDescription
                {
                    Name = name,
                    Bindings = new List<TemplatedTaskBinding>(),
                },
                Code = new StringBuilder(),
                Attributes = new AttributeSet(),
                SourceAttributes = new AttributeSet(),
            };
        }

        static SubtaskDescription EndSubtask(in SubtaskState state)
        {
            var attributeSets = new Dictionary<IDataKey, AttributeSet>
            {
                [IntermediateGraphBuilder.DefaultAttributeKey] = state.Attributes
            };
            if (!state.SourceAttributes.IsEmpty())
            {
                attributeSets.Add(IntermediateGraphBuilder.SourceAttributeKey, state.SourceAttributes);
            }

            var description = state.Description;
            description.Task = new TemplateSubtask(description.Name, state.Code.ToString(), attributeSets);
            return description;
        }

        void WriteValueParameter(ref SubtaskState state, string leftHandSide, VFXExpression expression,
            ContextExpressions contextExpressions)
        {
            string value;
            if (expression.Is(VFXExpression.Flags.Constant))
            {
                value = expression.GetCodeString(null);
            }
            else if (expression.IsAny(VFXExpression.Flags.NotCompilableOnCPU))
            {
                m_InlineExpressions.RequestExpressionInputs(state.Description.Bindings, expression);
                value = m_InlineExpressions.Evaluate(state.Code, expression);
            }
            else
            {
                m_InlineExpressions.RequestUniform(state.Description.Bindings, expression);
                value = contextExpressions.GetBindingName(expression);
            }

            state.Code.Append($"{leftHandSide} = {value};\n");
            m_InlineExpressions.RegisterReadAttributes(state.Attributes, state.SourceAttributes, expression);
        }

        void WriteResourceParameter(ref SubtaskState state, string parameterName, VFXExpression reduced, VFXValueType reducedValueType)
        {
            var binding = m_InlineExpressions.RequestResource(state.Description.Bindings, reduced);
            if (binding == null)
                return;

            string parameterType = GetResourceParameterType(reducedValueType, binding.BufferType);
            string parameterValue = GetResourceParameterValue(reducedValueType, binding.BufferType, binding.Key.ToString());
            state.Code.Append($"{parameterType} {parameterName} = {parameterValue};\n");
        }

        static string GetResourceParameterType(VFXValueType valueType, BufferType bufferType)
        {
            var bufferDeclaration = VfxBufferTypeHlsl.GetDeclaration(bufferType);
            if (bufferDeclaration != null)
                return bufferDeclaration;

            return valueType switch
            {
                VFXValueType.Texture2D => "VFXSampler2D",
                VFXValueType.Texture2DArray => "VFXSampler2DArray",
                VFXValueType.Texture3D => "VFXSampler3D",
                VFXValueType.TextureCube => "VFXSamplerCube",
                VFXValueType.TextureCubeArray => "VFXSamplerCubeArray",
                VFXValueType.CameraBuffer => "VFXSamplerCameraBuffer",
                _ => VFXExpression.TypeToCode(valueType)
            };
        }

        static string GetResourceParameterValue(VFXValueType valueType, BufferType bufferType, string bindingName)
        {
            if (VFXExpression.IsTexture(valueType) && !bufferType.valid)
                return $"VFX_SAMPLER({bindingName})";
            return bindingName;
        }
    }
}
