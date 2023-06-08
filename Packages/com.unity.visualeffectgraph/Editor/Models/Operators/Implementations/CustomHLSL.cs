using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor.VFX.Block;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    class CustomHLSLOperatorFunctionValidator
    {
        public IEnumerable<IHLSMessage> Validate(IEnumerable<string> functions, HLSLFunction selectedFunction)
        {
            if (selectedFunction == null)
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

            if (selectedFunction.returnType == typeof(void))
            {
                yield return new HLSLVoidReturnTypeUnsupported(selectedFunction.name);
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
            foreach (var input in selectedFunction.inputs)
            {
                if (input.rawType == "Texture2D")
                {
                    yield return new HLSLTexture2DShouldNotBeUsed(input.name);
                }
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

            foreach (var input in selectedFunction.inputs)
            {
                if (input.access == HLSLAccess.NONE)
                {
                    yield return new HLSLMissingAccessError(input.name, new [] { HLSLAccess.IN, HLSLAccess.OUT, HLSLAccess.INOUT }, HLSLAccess.IN);
                }
            }
        }
    }

    [VFXInfo(category = "HLSL")]
    class CustomHLSL : VFXOperator, IHLSLCodeHolder
    {
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

        internal override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            base.GenerateErrors(manager);
            var hlslValidator = new CustomHLSLOperatorFunctionValidator();
            ParseCodeIfNeeded();
            foreach(var error in hlslValidator.Validate(m_AvailableFunctions.values, m_Function))
            {
                manager.RegisterError(string.Empty, error.type, error.message);
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            ParseCodeIfNeeded();

            // Specifically handle buffers to specify the templated type
            for (int i = 0; i < inputExpression.Length; i++)
            {
                if (inputExpression[i] is VFXGraphicsBufferValue bufferExpression)
                {
                    foreach (var attribute in m_InputProperties[i].property.attributes.attributes)
                    {
                        if (attribute is TemplatedTypeAttribute templatedTypeAttribute)
                        {
                            bufferExpression.templateType = templatedTypeAttribute.type;
                            break;
                        }
                    }
                }
            }

            var hasShaderFile = HasShaderFile();
            var hlslCode = hasShaderFile
                ? $"#include \"{AssetDatabase.GetAssetPath(m_ShaderFile)}\"\n"
                : BuildCustomCode();
            var functionName = hasShaderFile
                ? m_Function?.name
                : m_Function?.GetNameWithHashCode();
            var valueType = GetValueType(m_Function?.returnType);

            if (valueType == VFXValueType.None)
            {
                return new VFXExpression[] { new VFXValue<int>(0, VFXValue.Mode.Constant, VFXExpression.Flags.InvalidOnCPU) };
            }

            var expressions = new List<VFXExpression>(m_OutputProperties.Count);
            expressions.Add(new VFXExpressionHLSL(functionName, hlslCode, valueType, inputExpression));
            for (int i = 0; i < m_InputParameters.Count; i++)
            {
                var parameter = m_InputParameters[i];
                if (parameter.access is HLSLAccess.IN or HLSLAccess.NONE)
                    continue;

                expressions.Add(new VFXExpressionPassThrough(i, GetValueType(parameter.type), inputExpression));
            }

            return expressions.ToArray();
        }

        private VFXValueType GetValueType(Type type)
        {
            switch (type)
            {
                case null: return VFXValueType.None;
                case var x when x == typeof(bool): return VFXValueType.Boolean;
                case var x when x == typeof(float): return VFXValueType.Float;
                case var x when x == typeof(Vector2): return VFXValueType.Float2;
                case var x when x == typeof(Vector3): return VFXValueType.Float3;
                case var x when x == typeof(Vector4): return VFXValueType.Float4;
                case var x when x == typeof(int): return VFXValueType.Int32;
                case var x when x == typeof(uint): return VFXValueType.Uint32;
                case var x when x == typeof(Texture2D): return VFXValueType.Texture2D;
                case var x when x == typeof(Texture3D): return VFXValueType.Texture3D;
                case var x when x == typeof(Matrix4x4): return VFXValueType.Matrix4x4;
                case var x when x == typeof(Buffer): return VFXValueType.Buffer;
                case var x when x == typeof(AnimationCurve): return VFXValueType.Curve;
                case var x when x == typeof(Gradient): return VFXValueType.ColorGradient;
                default: return VFXValueType.None;
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
            var strippedHLSL = HLSLParser.StripCommentedCode(GetHLSLCode());
            if (strippedHLSL != cachedHLSLCode || m_SelectedFunction != m_AvailableFunctions.GetSelection() || m_AvailableFunctions.values == null)
            {
                var functions = new List<HLSLFunction>(HLSLFunction.Parse(strippedHLSL));

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
                    var additionalOutputProperties = new List<VFXPropertyWithValue>();

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
                                additionalOutputProperties.Add(CreateProperty(input));
                            }
                        }
                    }

                    m_OutputProperties = new List<VFXPropertyWithValue>();
                    if (m_Function.returnType != typeof(void) && m_Function.returnType != null)
                    {
                        m_OutputProperties.Add(new VFXPropertyWithValue(new VFXProperty(m_Function.returnType, "Out")));
                        if (additionalOutputProperties.Count > 0)
                        {
                            m_OutputProperties.AddRange(additionalOutputProperties);
                        }
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

                cachedHLSLCode = strippedHLSL;
            }
        }

        private VFXPropertyWithValue CreateProperty(HLSLFunctionParameter parameter)
        {
            var propertyAttributes = new List<PropertyAttribute>();
            if (!string.IsNullOrEmpty(parameter.templatedType))
            {
                propertyAttributes.Add(new TemplatedTypeAttribute(parameter.templatedType));
            }

            if (!string.IsNullOrEmpty(parameter.tooltip))
            {
                propertyAttributes.Add(new TooltipAttribute(parameter.tooltip));
            }

            return propertyAttributes.Count > 0
                ? new VFXPropertyWithValue(new VFXProperty(parameter.type, parameter.name, propertyAttributes.ToArray()))
                : new VFXPropertyWithValue(new VFXProperty(parameter.type, parameter.name));
        }

        private string BuildCustomCode()
        {
            ParseCodeIfNeeded();

            return m_Function != null
                ? m_Function.GetTransformedHLSL()
                : string.Empty;
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
