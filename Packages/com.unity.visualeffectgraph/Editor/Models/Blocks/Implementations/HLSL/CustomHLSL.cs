using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEditor.VFX.UI;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    sealed class CustomHLSLBlockFunctionValidator : Operator.CustomHLSLFunctionValidator
    {
        protected override IEnumerable<IHLSMessage> ValidateImpl(IEnumerable<string> functions, HLSLFunction selectedFunction)
        {
            if (selectedFunction.returnType != typeof(void))
            {
                yield return new HLSLVoidReturnTypeOnlyIsSupported(selectedFunction.name);
            }
            if (selectedFunction.returnType == null)
            {
                yield return new HLSLUnknownParameterType(selectedFunction.rawReturnType);
            }

            HLSLFunctionParameter attributesInput = null;
            foreach (var input in selectedFunction.inputs)
            {
                if (input.type == typeof(VFXAttribute))
                {
                    attributesInput = input;
                }
                else if (input.access is HLSLAccess.OUT or HLSLAccess.INOUT)
                {
                    yield return new HLSLOutParameterNotAllowed(input.name);
                }
            }
            if (attributesInput == null)
            {
                yield return new HLSLMissingVFXAttribute();
            }
            if (attributesInput != null && selectedFunction.attributes.Count > 0)
            {
                if (!attributesInput.access.HasFlag(HLSLAccess.OUT))
                {
                    foreach (var attribute in selectedFunction.attributes)
                    {
                        if (attribute.mode.HasFlag(VFXAttributeMode.Write))
                        {
                            yield return new HLSLVFXAttributeAccessError();
                            break;
                        }
                    }
                }
            }
        }
    }

    [VFXInfo(category = "HLSL")]
    class CustomHLSL : VFXBlock, IHLSLCodeHolder
    {
        public const string FunctionNameSuffix = "Block";
        public const string parameterPrefix = "_";

        const string defaultHlslCode =
            "void CustomHLSL(inout VFXAttributes attributes, in float3 offset, in float speedFactor)" + "\n" +
            "{" + "\n" +
            "  attributes.position += offset;" + "\n" +
            "  attributes.velocity *= speedFactor;" + "\n" +
            "}";

        [NonSerialized] List<VFXAttributeInfo> m_Attributes;
        [NonSerialized] List<VFXPropertyWithValue> m_Properties;
        [NonSerialized] HLSLFunction m_Function;
        [NonSerialized] string cachedHLSLCode;
        [NonSerialized] string m_SelectedFunction;

        [Tooltip("Name of the block displayed in the VFX Graph editor")]
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Delayed]
        string m_BlockName = "Custom HLSL";
        // Keep hlsl code setting disabled because multiline + delayed text area is not supported
        [Tooltip("HLSL code embedded with the block node")]
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector|VFXSettingAttribute.VisibleFlags.InGraph|VFXSettingAttribute.VisibleFlags.ReadOnly), VFXSettingFieldType(typeof(HLSLPropertyRM)), Multiline(10), SerializeField]
        string m_HLSLCode;
        [Tooltip("External file containing the HLSL code to execute in this block.\nNote that if a shader file is provided, the embedded code is kept but ignored.")]
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector|VFXSettingAttribute.VisibleFlags.InGraph), VFXSettingFieldType(typeof(HLSLPropertyRM)), SerializeField]
        private ShaderInclude m_ShaderFile;
        [Tooltip("Select which function to execute")]
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InGraph), VFXSettingFieldType(typeof(ListPropertyRM)), SerializeField]
        MultipleValuesChoice<string> m_AvailableFunction;

        public CustomHLSL()
        {
            m_HLSLCode = defaultHlslCode;
        }

        public override VFXContextType compatibleContexts => VFXContextType.InitAndUpdateAndOutput;
        public override VFXDataType compatibleData => VFXDataType.Particle;

        public override string name => m_BlockName;

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                ParseCodeIfNeeded();
                return m_Attributes ?? new List<VFXAttributeInfo>();
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                ParseCodeIfNeeded();
                return m_Properties ?? new List<VFXPropertyWithValue>();
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var expression in base.parameters)
                {
                    if (VFXExpression.IsTexture(expression.exp.valueType))
                    {
                        var property = m_Properties.Find(x => x.property.name == expression.name);
                        bool expressionWithUsage = false;
                        foreach (var attribute in property.property.attributes.attributes)
                        {
                            if (attribute is BufferTypeUsageAttribute bufferTypeUsage)
                            {
                                var newExpressionWithUsage = new VFXExpressionBufferWithType(bufferTypeUsage.Type, expression.exp);
                                expressionWithUsage = true;
                                yield return new VFXNamedExpression(newExpressionWithUsage, expression.name);
                                break;
                            }
                        }

                        //Binding texture without usage is allowed
                        if (!expressionWithUsage)
                            yield return expression;
                    }
                    else if (expression.exp is VFXGraphicsBufferValue)
                    {
                        var usage = new BufferType();
                        var property = m_Properties.Find(x => x.property.name == expression.name);
                        foreach (var attribute in property.property.attributes.attributes)
                        {
                            if (attribute is BufferTypeUsageAttribute bufferTypeUsage)
                            {
                                usage = bufferTypeUsage.Type;
                                break;
                            }
                        }

                        if (!usage.valid)
                            throw new InvalidOperationException($"Unexpected missing GraphicsBufferUsageAttribute at {expression.name}");

                        var expressionBufferWithType = new VFXExpressionBufferWithType(usage, expression.exp);
                        yield return new VFXNamedExpression(expressionBufferWithType, expression.name);

                    }
                    else
                        yield return expression;
                }
            }
        }

        public override string source => BuildSource();

        public ShaderInclude shaderFile => m_ShaderFile;

        public string sourceCode
        {
            get => GetHLSLCode();
            set
            {
                if (HasShaderFile())
                {
                    var path = AssetDatabase.GetAssetPath(m_ShaderFile);
                    using (var stream = File.CreateText(path))
                    {
                        stream.Write(value);
                    }
                    AssetDatabase.ImportAsset(path);
                }
                else
                {
                    m_HLSLCode = value;
                }
                Invalidate(InvalidationCause.kSettingChanged);
            }
        }
        public string customCode => BuildCustomCode();
        public IEnumerable<string> includes
        {
            get
            {
                if (HasShaderFile())
                {
                    return new[] { AssetDatabase.GetAssetPath(this.m_ShaderFile) };
                }
                return HLSLParser.ParseIncludes(cachedHLSLCode);
            }
        }

        public bool HasShaderFile() => m_ShaderFile != null && !object.ReferenceEquals(m_ShaderFile, null);

        public override IEnumerable<VFXSetting> GetSettings(bool listHidden, VFXSettingAttribute.VisibleFlags flags = VFXSettingAttribute.VisibleFlags.Default)
        {
            var settings = base.GetSettings(listHidden, flags);

            var nameOfSettingToExclude = string.Empty;

            // Only for the graph
            // If the shader file is assigned filter out the user code setting, otherwise filter out the shader file setting
            if (flags == VFXSettingAttribute.VisibleFlags.InGraph)
            {
                nameOfSettingToExclude = m_ShaderFile != null && !object.ReferenceEquals(m_ShaderFile, null)
                    ? nameof(m_HLSLCode)
                    : nameof(m_ShaderFile);
            }

            foreach (var setting in settings)
            {
                if (setting.name != nameOfSettingToExclude)
                {
                    yield return setting;
                }
            }
        }

        public override void GetImportDependentAssets(HashSet<int> dependencies)
        {
            base.GetImportDependentAssets(dependencies);
            if (!ReferenceEquals(m_ShaderFile, null))
            {
                dependencies.Add(m_ShaderFile.GetInstanceID());
            }
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kSettingChanged)
            {
                ParseCodeIfNeeded();
            }
            base.OnInvalidate(model, cause);
        }

        public override void CheckGraphBeforeImport()
        {
            base.CheckGraphBeforeImport();

            // If the graph is re-imported it can be because one of its dependency such as an external hlsl file that has changed.
            if (!VFXGraph.explicitCompile)
                ResyncSlots(true);
        }

        protected override void OnAdded()
        {
            base.OnAdded();
            // Parse again now that the parent graph is accessible
            Invalidate(InvalidationCause.kSettingChanged);
        }

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);
            var hlslValidator = new CustomHLSLBlockFunctionValidator();
            ParseCodeIfNeeded();

            var basePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(GetGraph().GetResource()));
            foreach(var error in hlslValidator.Validate(m_AvailableFunction.values, m_Function, basePath, includes))
            {
                report.RegisterError(string.Empty, error.type, error.message, this);
            }

            if (m_Function?.errorList != null)
            {
                foreach (var error in m_Function.errorList)
                {
                    report.RegisterError(string.Empty, error.type, error.message, this);
                }
            }
        }

        private string GetHLSLCode()
        {
            if (HasShaderFile())
            {
                var path = AssetDatabase.GetAssetPath(this.m_ShaderFile);
                return File.ReadAllText(path);
            }

            return m_HLSLCode;
        }

        private void ParseCodeIfNeeded()
        {
            var graph = GetGraph();
            if (graph == null)
            {
                return;
            }

            var hasError = m_Function?.errorList.Count > 0;
            var hlslCode = GetHLSLCode();
            if (hasError || hlslCode != cachedHLSLCode || m_SelectedFunction != m_AvailableFunction.GetSelection() || m_AvailableFunction.values == null)
            {
                var functions = new List<HLSLFunction>(HLSLFunction.Parse(graph.attributesManager, hlslCode));

                if (functions.Count > 0)
                {
                    HLSLFunction function = null;
                    var functionNames = new List<string>(functions.Count);

                    // Pick last selected function by name
                    foreach (var f in functions)
                    {
                        if (function == null && f.name == m_AvailableFunction.GetSelection())
                        {
                            function = f;
                        }
                        // Pack this here to avoid too parsing functions one more time
                        functionNames.Add(f.name);
                    }

                    // If not found pick the last selected function by index (in case of rename for instance)
                    if (function == null)
                    {
                        foreach (var f in functions)
                        {
                            if (m_Function != null && f.index == m_Function.index)
                            {
                                function = f;
                                break;
                            }
                            // Fallback to first function if none match
                            function ??= f;
                        }
                    }

                    m_Function = function;
                    m_AvailableFunction = new MultipleValuesChoice<string> { values = functionNames };
                    m_AvailableFunction.SetSelection(m_Function.name);
                    m_SelectedFunction = m_Function.name;
                    m_Attributes = new List<VFXAttributeInfo>(m_Function.attributes);

                    m_Properties = new List<VFXPropertyWithValue>();
                    foreach (var input in m_Function.inputs)
                    {
                        if (input.type != null && input.type != typeof(VFXAttribute) && input.access is HLSLAccess.IN or HLSLAccess.NONE)
                        {
                            m_Properties.Add(CreateProperty(input));
                        }
                    }
                }
                else
                {
                    m_Function = null;
                    m_SelectedFunction = null;
                    m_AvailableFunction = new MultipleValuesChoice<string>() { values = new List<string>() };
                }

                cachedHLSLCode = hlslCode;
            }
        }

        private VFXPropertyWithValue CreateProperty(HLSLFunctionParameter parameter)
        {
            var propertyAttributes = new List<object>();
            if (parameter.bufferType.valid)
            {
                propertyAttributes.Add(new BufferTypeUsageAttribute(parameter.bufferType));
            }

            if (!string.IsNullOrEmpty(parameter.tooltip))
            {
                propertyAttributes.Add(new TooltipAttribute(parameter.tooltip));
            }

            return propertyAttributes.Count > 0
                ? new VFXPropertyWithValue(new VFXProperty(parameter.type, $"{parameterPrefix}{parameter.name}", propertyAttributes.ToArray()))
                : new VFXPropertyWithValue(new VFXProperty(parameter.type, $"{parameterPrefix}{parameter.name}"));
        }

        private string BuildSource()
        {
            ParseCodeIfNeeded();

            if (m_Function == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            var functionParameters = new List<string>();

            // Create and initialize a VFXAttributes structure
            builder.AppendLine("VFXAttributes att = (VFXAttributes)0;");
            foreach (var attribute in m_Attributes)
            {
                builder.AppendLine($"att.{attribute.attrib.name} = {attribute.attrib.name};");
            }
            functionParameters.Add("att");

            // Make the call to custom hlsl function
            var functionName = HasShaderFile()
                ? m_Function.name
                : m_Function.GetNameWithHashCode(FunctionNameSuffix);
            builder.Append($"{functionName}(");
            if (m_Properties.Count > 0)
            {
                foreach (var property in m_Properties)
                {
                    functionParameters.Add(property.property.name);
                }
            }

            builder.AppendJoin(", ", functionParameters);
            builder.Append(");");

            // Copy VFXAttributes structure values back to out attributes parameters
            foreach (var attribute in m_Attributes)
            {
                if (attribute.mode.HasFlag(VFXAttributeMode.Write))
                {
                    builder.AppendLine();
                    builder.Append($"{attribute.attrib.name} = att.{attribute.attrib.name};");
                }
            }

            return builder.ToString();
        }

        private string BuildCustomCode()
        {
            ParseCodeIfNeeded();
            if (m_Function == null || HasShaderFile())
            {
                return string.Empty;
            }

            return m_Function.GetTransformedHLSL(FunctionNameSuffix);
        }

        public bool Equals(IHLSLCodeHolder other)
        {
            if (other == null)
            {
                return false;
            }

            return ReferenceEquals(this, other) || HasShaderFile() && other.HasShaderFile() && m_ShaderFile == other.shaderFile;
        }

        public override void Rename(string oldName, string newName)
        {
            cachedHLSLCode = string.Empty;
            Invalidate(InvalidationCause.kSettingChanged);
            /*var hlslCode = GetHLSLCode();
            sourceCode = hlslCode.Replace(oldName, newName);*/
        }
    }
}
