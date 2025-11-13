using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    abstract class CustomHLSLFunctionValidator
    {
        public IEnumerable<IHLSMessage> Validate(IEnumerable<string> functions, HLSLFunction selectedFunction, string basePath, IEnumerable<string> includes)
        {
            if (functions == null || selectedFunction == null)
            {
                yield return new HLSLMissingFunction();
                yield break;
            }

            var functionsFound = new Dictionary<string, int>();
            foreach (var f in functions)
            {
                if (functionsFound.TryGetValue(f, out var count) && count == 1)
                {
                    yield return new HLSLSameFunctionName(f);
                }
                else
                {
                    count = 0;
                }

                functionsFound[f] = count + 1;
            }

            foreach (var message in selectedFunction.inputs)
            {
                if (message.errors != null)
                {
                    foreach (var error in message.errors)
                    {
                        yield return error;
                    }
                }
            }

            foreach (var include in includes)
            {
                var guid = AssetDatabase.AssetPathToGUID(include);
                if (string.IsNullOrEmpty(guid))
                {
                    //Try with Relative Path
                    guid = AssetDatabase.AssetPathToGUID(Path.Combine(basePath, include));
                    if (string.IsNullOrEmpty(guid))
                        yield return new HLSLMissingIncludeFile(include);
                }
            }

            foreach (var specificContextMessage in ValidateImpl(functions, selectedFunction))
            {
                yield return specificContextMessage;
            }
        }

        protected abstract IEnumerable<IHLSMessage> ValidateImpl(IEnumerable<string> functions, HLSLFunction selectedFunction);
    }

    sealed class CustomHLSLOperatorFunctionValidator : CustomHLSLFunctionValidator
    {
        protected override IEnumerable<IHLSMessage> ValidateImpl(IEnumerable<string> functions, HLSLFunction selectedFunction)
        {
            if (selectedFunction.returnType == typeof(void))
            {
                var hasOutParameter = false;
                foreach (var input in selectedFunction.inputs)
                {
                    if (input.access is HLSLAccess.OUT or HLSLAccess.INOUT)
                    {
                        hasOutParameter = true;
                        break;
                    }
                }

                if (!hasOutParameter)
                {
                    yield return new HLSLVoidReturnTypeUnsupported(selectedFunction.name);
                }
            }
            if (selectedFunction.returnType == null)
            {
                yield return new HLSLUnknownParameterType(selectedFunction.rawReturnType);
            }
            if (selectedFunction.attributes.Count > 0)
            {
                var attributes = new List<string>(selectedFunction.attributes.Count);
                foreach (var attribute in selectedFunction.attributes)
                {
                    attributes.Add(attribute.attrib.name);
                }
                yield return new HLSLUnsupportedAttributes(attributes);
            }
        }
    }

    [VFXInfo(category = "HLSL")]
    class CustomHLSL : VFXOperator, IHLSLCodeHolder
    {
        public const string ReturnFunctionSuffix = "Return";

        const string defaultHlslCode =
            "float FloatFunction(in float value)" + "\n" +
            "{" + "\n" +
            "  return value;" + "\n" +
            "}";

        [NonSerialized] List<VFXPropertyWithValue> m_InputProperties;
        [NonSerialized] List<VFXPropertyWithValue> m_OutputProperties;
        [NonSerialized] HLSLFunction m_Function;
        [NonSerialized] string cachedHLSLCode;
        [NonSerialized] string m_SelectedFunction;
        [NonSerialized] List<HLSLFunctionParameter> m_InputParameters;

        [Tooltip("Name of the block displayed in the VFX Graph editor")]
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Delayed]
        string m_OperatorName = "Custom HLSL";
        // Keep hlsl code setting disabled because multiline + delayed text area is not supported
        [Tooltip("HLSL code embedded with the block node")]
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector|VFXSettingAttribute.VisibleFlags.InGraph|VFXSettingAttribute.VisibleFlags.ReadOnly), VFXSettingFieldType(typeof(HLSLPropertyRM)), Multiline(10), SerializeField]
        string m_HLSLCode;
        [Tooltip("External file containing the HLSL code to execute in this block.\nNote that if a shader file is provided, the embedded code is kept but ignored.")]
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector|VFXSettingAttribute.VisibleFlags.InGraph), VFXSettingFieldType(typeof(HLSLPropertyRM)), SerializeField]
        ShaderInclude m_ShaderFile;
        [Tooltip("Select which function to execute")]
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InGraph), VFXSettingFieldType(typeof(ListPropertyRM)), SerializeField]
        MultipleValuesChoice<string> m_AvailableFunctions;

        public CustomHLSL()
        {
            m_HLSLCode = defaultHlslCode;
        }

        public override string name => m_OperatorName;

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                ParseCodeIfNeeded();
                return m_InputProperties ?? new List<VFXPropertyWithValue>();
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                ParseCodeIfNeeded();
                return m_OutputProperties ?? new List<VFXPropertyWithValue>();
            }
        }

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
        public string customCode => GetHLSLCode();

        public IEnumerable<string> includes
        {
            get
            {
                if (HasShaderFile())
                {
                    return new[] { AssetDatabase.GetAssetPath(m_ShaderFile) };
                }
                return HLSLParser.ParseIncludes(m_HLSLCode);
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

        // Do not resync slots when no function to preserve existing slots and links when there's an error
        public override bool ResyncSlots(bool notify)
        {
            return m_Function != null && base.ResyncSlots(notify);
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

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);
            var hlslValidator = new CustomHLSLOperatorFunctionValidator();
            ParseCodeIfNeeded();

            var basePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(GetGraph().GetResource()));
            foreach (var error in hlslValidator.Validate(m_AvailableFunctions.values, m_Function, basePath, includes))
            {
                report.RegisterError(string.Empty, error.type, error.message, this);
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            ParseCodeIfNeeded();

            if (m_Function == null)
                return Array.Empty<VFXExpression>();

            // Specifically handle buffers to specify the templated type
            for (int i = 0; i < inputExpression.Length; i++)
            {
                if (VFXExpression.IsTexture(inputExpression[i].valueType))
                {
                    foreach (var attribute in m_InputProperties[i].property.attributes.attributes)
                    {
                        if (attribute is BufferTypeUsageAttribute bufferTypeUsageAttribute)
                        {
                            var expressionBufferWithType = new VFXExpressionBufferWithType(bufferTypeUsageAttribute.Type, inputExpression[i]);
                            inputExpression[i] = expressionBufferWithType;
                            break;
                        }
                    }
                }
                else if (inputExpression[i] is VFXGraphicsBufferValue bufferExpression)
                {
                    foreach (var attribute in m_InputProperties[i].property.attributes.attributes)
                    {
                        if (attribute is BufferTypeUsageAttribute bufferUsageAttribute)
                        {
                            var expressionBufferWithType = new VFXExpressionBufferWithType(bufferUsageAttribute.Type, bufferExpression);
                            inputExpression[i] = expressionBufferWithType;
                            break;
                        }
                    }
                }
            }

            var currentIncludes = new List<string>(includes).ToArray();
            var expressions = new List<VFXExpression>(m_OutputProperties.Count);

            for (int i = 0; i < m_InputParameters.Count; i++)
            {
                var parameter = m_InputParameters[i];
                if (parameter.access is HLSLAccess.IN or HLSLAccess.NONE)
                    continue;

                var parameterType = VFXExpression.GetVFXValueTypeFromType(parameter.type);
                var hlslCode = BuildHLSLWrapperCode(i, parameter.name, parameter.rawType, out var wrapperFunctionName);
                expressions.Add(new VFXExpressionHLSL(wrapperFunctionName, hlslCode, parameterType, inputExpression, currentIncludes));
            }

            var valueType = VFXExpression.GetVFXValueTypeFromType(m_Function?.returnType);
            if (valueType != VFXValueType.None)
            {
                var hlslCode = BuildHLSLWrapperCode(m_InputParameters.Count, ReturnFunctionSuffix, m_Function.rawReturnType, out var wrapperFunctionName);
                expressions.Add(new VFXExpressionHLSL(wrapperFunctionName, hlslCode, valueType, inputExpression, currentIncludes));
            }

            return expressions.ToArray();
        }

        protected override void OnAdded()
        {
            base.OnAdded();
            // Parse again now that the parent graph is accessible
            Invalidate(InvalidationCause.kSettingChanged);
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
            if (hasError || hlslCode != cachedHLSLCode || m_SelectedFunction != m_AvailableFunctions.GetSelection() || m_AvailableFunctions.values == null)
            {
                var functions = new List<HLSLFunction>(HLSLFunction.Parse(graph.attributesManager, hlslCode));

                if (functions.Count > 0)
                {
                    HLSLFunction function = null;
                    var functionNames = new List<string>(functions.Count);

                    // Pick last selected function by name
                    foreach (var f in functions)
                    {
                        if (function == null && f.name == m_AvailableFunctions.GetSelection())
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
                    m_AvailableFunctions = new MultipleValuesChoice<string> { values = functionNames };
                    m_AvailableFunctions.SetSelection(m_Function.name);
                    m_SelectedFunction = m_Function.name;
                    m_InputParameters = new List<HLSLFunctionParameter>(m_Function.inputs);
                    m_InputProperties = new List<VFXPropertyWithValue>();
                    m_OutputProperties = new List<VFXPropertyWithValue>();

                    foreach (var input in m_InputParameters)
                    {
                        if (input.type != null)
                        {
                            if (input.type != typeof(VFXAttribute) && input.access is HLSLAccess.IN or HLSLAccess.NONE)
                            {
                                m_InputProperties.Add(CreateProperty(input));
                            }
                            else if (input.access is HLSLAccess.OUT or HLSLAccess.INOUT)
                            {
                                m_OutputProperties.Add(CreateProperty(input));
                            }
                        }
                    }
                    if (m_Function.returnType != typeof(void) && m_Function.returnType != null)
                    {
                        m_OutputProperties.Add(new VFXPropertyWithValue(new VFXProperty(m_Function.returnType, m_Function.returnName)));
                    }
                }
                else
                {
                    m_Function = null;
                    m_SelectedFunction = null;
                    m_AvailableFunctions = new MultipleValuesChoice<string>() { values = new List<string>() };
                    m_InputParameters = new List<HLSLFunctionParameter>();
                    m_InputProperties = new List<VFXPropertyWithValue>();
                    m_OutputProperties = new List<VFXPropertyWithValue>();
                }

                cachedHLSLCode = hlslCode;
            }
        }

        public override void OnUnknownChange()
        {
            ParseCodeIfNeeded();
            base.OnUnknownChange();
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
                ? new VFXPropertyWithValue(new VFXProperty(parameter.type, parameter.name, propertyAttributes.ToArray()))
                : new VFXPropertyWithValue(new VFXProperty(parameter.type, parameter.name));
        }

        private string BuildHLSLWrapperCode(int outputIndex, string returnedParameterName, string returnType, out string wrapperFunctionName)
        {
            ParseCodeIfNeeded();

            if (m_Function != null)
            {
                var hasShaderFile = HasShaderFile();
                var functionName = hasShaderFile ? m_Function.name : m_Function.GetNameWithHashCode(returnedParameterName);

                var hlslCode = new StringBuilder();
                if (!hasShaderFile)
                    hlslCode.Append(m_Function.GetTransformedHLSL(returnedParameterName));

                wrapperFunctionName = hasShaderFile ? $"{m_Function.name}_{returnedParameterName}_Wrapper" : $"{functionName}_Wrapper";

                hlslCode.Append($"{returnType} {wrapperFunctionName}(");
                var isFirst = true;
                foreach (var parameter in m_InputParameters)
                {
                    if (parameter.access is not HLSLAccess.OUT)
                    {
                        if (!isFirst)
                            hlslCode.Append(", ");
                        else
                            isFirst = false;
                        hlslCode.Append($"{parameter.templatedRawType} {parameter.name}");
                    }
                }
                hlslCode.AppendLine(")");

                hlslCode.AppendLine("{");
                uint paramIndex = 0;
                string[] parameters = new string[m_InputParameters.Count + 1];
                foreach (var input in this.m_InputParameters)
                {
                    if (input.access is HLSLAccess.OUT)
                    {
                        var parameterName = $"var_{paramIndex}";
                        parameters[paramIndex++] = parameterName;
                        hlslCode.AppendLine($"\t{input.rawType} {parameterName};");
                    }
                    else
                    {
                        parameters[paramIndex++] = input.name;
                    }
                }

                var inputParametersString = string.Join(", ", parameters).TrimEnd(new [] { ' ', ',' });
                if (m_Function.returnType != typeof(void))
                {
                    var returnName = $"var_{paramIndex}";
                    parameters[paramIndex++] = returnName;
                    // This will put the return value as last output slot
                    hlslCode.Append($"\t{m_Function.rawReturnType} {returnName} = ");
                }

                hlslCode.AppendLine($"\t{functionName}({inputParametersString});");
                hlslCode.AppendLine($"\treturn {parameters[outputIndex]};");
                hlslCode.AppendLine("}");

                return hlslCode.ToString();
            }

            wrapperFunctionName = null;
            return string.Empty;
        }

        public bool Equals(IHLSLCodeHolder other)
        {
            if (other == null)
            {
                return false;
            }

            return ReferenceEquals(this, other) || HasShaderFile() && other.HasShaderFile() && m_ShaderFile == other.shaderFile;
        }
    }
}
