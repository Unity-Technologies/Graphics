using System.Collections.Generic;
using System.Text;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class SubtaskBuilder
    {
        private ExpressionWriter m_ExpressionWriter = new ExpressionWriter();

        readonly Dictionary<VFXExpression, VfxResourceBinding> m_BlockResources = new();

        // Per block caches to early exit expression tree walk
        readonly HashSet<VFXExpression> m_WalkedResourceExpressions = new();
        readonly HashSet<VFXExpression> m_WalkedAttributeExpressions = new();

        public List<SubtaskDescription> GenerateSubtasks(VFXContext context, ContextExpressions contextExpressions)
        {
            List<SubtaskDescription> subtaskDescriptions = new List<SubtaskDescription>();
            var gpuMapper = contextExpressions.GpuMapper;
            var uniformMapper = contextExpressions.UniformMapper;
            m_ExpressionWriter.Initialize(uniformMapper);

            int blockIndex = 0;
            foreach (var block in context.activeFlattenedChildrenWithImplicit)
            {
                m_ExpressionWriter.StartBlock();
                var subTaskDesc = new SubtaskDescription();
                subTaskDesc.Name = block.name;
                subTaskDesc.Bindings = new List<TemplatedTaskBinding>();

                StringBuilder blockCodeBuilder = new StringBuilder(block.source);
                StringBuilder expressionCodeBuilder = new StringBuilder();

                var defaultAttributeSet = new AttributeSet();
                var sourceAttributeSet = new AttributeSet();
                m_BlockResources.Clear();
                m_WalkedResourceExpressions.Clear();
                m_WalkedAttributeExpressions.Clear();

                foreach (var attributeInfo in block.attributes)
                {
                    var attribute = VFXAttributesManager.ConvertToNewCompiler(attributeInfo.attrib);
                    var usage = VFXAttributesManager.ConvertToNewCompiler(attributeInfo.mode);
                    defaultAttributeSet.AddAttribute(attribute, usage);
                }

                // Workaround to handle RAND which implicitly uses the seed attribute
                if (block.source != null && block.source.Contains("RAND"))
                {
                    defaultAttributeSet.AddAttribute(VFXAttributesManager.ConvertToNewCompiler(VFXAttribute.Seed), AttributeUsage.Read | AttributeUsage.Write);
                }

                // Generate parameters assignments and register attributes used by expressions
                foreach (var parameter in block.parameters)
                {
                    var reduced = gpuMapper.FromNameAndId(parameter.name, blockIndex);
                    Debug.Assert(reduced != null);
                    var reducedValueType = reduced.valueType;

                    if (!VFXExpression.IsUniform(reducedValueType))
                    {
                        WriteResourceParameter(subTaskDesc, expressionCodeBuilder, contextExpressions, parameter.name, reduced, reducedValueType);
                        RegisterBlockAttributesFromExpression(defaultAttributeSet, sourceAttributeSet, reduced);
                        continue;
                    }

                    string leftHandSide = $"{VFXExpression.TypeToCode(reducedValueType)} {parameter.name}";
                    string rightHandSide;
                    if (reduced.IsAny(VFXExpression.Flags.NotCompilableOnCPU))
                    {
                        // The expression is evaluated inline in the block, so the resources it reads have to
                        // be requested by the subtask just like a resource parameter would be.
                        RequestResourceInputs(subTaskDesc, contextExpressions, reduced);
                        m_ExpressionWriter.WriteExpressionEvaluation(expressionCodeBuilder, reduced);
                        rightHandSide = m_ExpressionWriter.GetVariableValueString(reduced);
                    }
                    else
                    {
                        string bindingName = uniformMapper.GetName(reduced);
                        var valueType = VFXExpression.TypeToType(reducedValueType);
                        var dataDescriptionType = ValueData.Create(valueType).GetType();
                        var binding = TemplatedTaskBinding<IDataDescription>.Create(dataDescriptionType, new NameDataKey(bindingName));
                        subTaskDesc.Bindings.Add(binding);
                        rightHandSide = reduced.Is(VFXExpression.Flags.Constant) ? reduced.GetCodeString(null) : bindingName;
                    }
                    expressionCodeBuilder.Append($"{leftHandSide} = {rightHandSide};\n");

                    RegisterBlockAttributesFromExpression(defaultAttributeSet, sourceAttributeSet, reduced);
                }

                blockCodeBuilder.Insert(0, expressionCodeBuilder.ToString());

                Dictionary<IDataKey, AttributeSet> attributeSets = new Dictionary<IDataKey, AttributeSet>();
                attributeSets.Add(IntermediateGraphBuilder.DefaultAttributeKey, defaultAttributeSet);
                if (!sourceAttributeSet.IsEmpty())
                {
                    attributeSets.Add(IntermediateGraphBuilder.SourceAttributeKey, sourceAttributeSet);
                }

                subTaskDesc.Task = new TemplateSubtask(block.name, blockCodeBuilder.ToString(), attributeSets);

                subtaskDescriptions.Add(subTaskDesc);
                blockIndex++;
            }
            return subtaskDescriptions;
        }

        /// <summary>
        /// Declares the GPU resource a block parameter reads, and aliases it to the name the block source
        /// uses. The resource is bound, and declared in the shader, under the name the uniform mapper gave
        /// it, which is not the parameter name the block was written against.
        /// </summary>
        void WriteResourceParameter(SubtaskDescription subTaskDesc, StringBuilder expressionCodeBuilder,
            ContextExpressions contextExpressions, string parameterName, VFXExpression reduced, VFXValueType reducedValueType)
        {
            var binding = RequestResource(subTaskDesc, contextExpressions, reduced);
            if (binding == null)
                return;

            var bufferType = contextExpressions.GetBufferType(reduced);

            string parameterType = GetResourceParameterType(reducedValueType, bufferType);
            string parameterValue = GetResourceParameterValue(reducedValueType, bufferType, binding.Key.ToString());
            expressionCodeBuilder.Append($"{parameterType} {parameterName} = {parameterValue};\n");
        }

        /// <summary>
        /// The type a block declares a resource parameter with. Textures are handed to blocks as the struct
        /// pairing the texture with its sampler, buffers as the container type a block declared for them.
        /// </summary>
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

        /// <summary>
        /// What a resource parameter is initialized from. Textures are paired with the sampler declared next
        /// to them; buffers are passed straight through.
        /// </summary>
        static string GetResourceParameterValue(VFXValueType valueType, BufferType bufferType, string bindingName)
        {
            if (VFXExpression.IsTexture(valueType) && !bufferType.valid)
                return $"VFX_SAMPLER({bindingName})";
            return bindingName;
        }

        /// <summary>
        /// Declares every GPU resource an inlined expression reads. Such an expression is evaluated in the
        /// block itself and refers to its resources by their binding name, so they need no alias, only a
        /// binding. Mirrors how <see cref="VFXUniformMapper"/> walks the same tree: an expression that cannot
        /// be evaluated on the CPU is inlined, anything below it is an input bound to the task.
        /// </summary>
        void RequestResourceInputs(SubtaskDescription subTaskDesc, ContextExpressions contextExpressions, VFXExpression expression)
        {
            foreach (var parent in expression.parents)
            {
                if (!m_WalkedResourceExpressions.Add(parent))
                    continue;

                // NotCompilableOnCPU is InvalidOnCPU | PerElement, propagated up from the leaves, so it
                // marks exactly the expressions that have to be evaluated in the shader.
                if (parent.IsAny(VFXExpression.Flags.NotCompilableOnCPU))
                    RequestResourceInputs(subTaskDesc, contextExpressions, parent);
                else
                    RequestResource(subTaskDesc, contextExpressions, parent);
            }
        }

        /// <summary>
        /// Adds the binding declaring <paramref name="expression"/>'s resource on the subtask, or returns the
        /// one already declared for it. Null when the expression is not a GPU resource.
        /// </summary>
        VfxResourceBinding RequestResource(SubtaskDescription subTaskDesc, ContextExpressions contextExpressions, VFXExpression expression)
        {
            if (m_BlockResources.TryGetValue(expression, out var binding))
                return binding;

            if (!contextExpressions.IsResource(expression))
                return null;

            binding = contextExpressions.CreateResourceBinding(expression);
            m_BlockResources.Add(expression, binding);
            subTaskDesc.Bindings.Add(binding);
            return binding;
        }

        void RegisterBlockAttributesFromExpression(AttributeSet defaultAttributeSet, AttributeSet sourceAttributeSet, VFXExpression expression)
        {
            // An attribute expression is per element, and that flag propagates to everything built on top of
            // it, so a subexpression without it cannot hold one and the whole subtree can be skipped.
            if (!expression.IsAny(VFXExpression.Flags.NotCompilableOnCPU))
                return;

            if (!m_WalkedAttributeExpressions.Add(expression))
                return;

            if (expression is VFXAttributeExpression attributeExpression)
            {
                var attributeSet = attributeExpression.attributeLocation == VFXAttributeLocation.Source ? sourceAttributeSet : defaultAttributeSet;
                attributeSet.AddAttribute(VFXAttributesManager.ConvertToNewCompiler(attributeExpression.attribute), AttributeUsage.Read);
            }

            foreach (var expressionParent in expression.parents)
            {
                RegisterBlockAttributesFromExpression(defaultAttributeSet, sourceAttributeSet, expressionParent);
            }
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

            if (exp is VFXAttributeExpression attributeExpression)
            {
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
