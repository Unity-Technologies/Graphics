
using System.Collections.Generic;
using System.Text;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    internal struct ParameterHeader
    {
        readonly internal bool isValid;

        readonly internal string referenceName;
        readonly internal IShaderType shaderType;
        readonly internal string typeName => shaderType.Name;

        readonly internal string displayName;
        readonly internal string tooltip;

        readonly internal bool isInput;
        readonly internal bool isOutput;

        readonly internal bool isColor;

        readonly internal bool isStatic;
        readonly internal bool isLocal;

        readonly internal bool isDropdown;
        readonly internal string[] options;

        readonly internal bool isSlider;
        readonly internal float sliderMin;
        readonly internal float sliderMax;

        readonly internal float[] defaultValue;
        readonly internal string defaultString;

        readonly internal string externalQualifiedTypeName;

        readonly internal bool isNumeric;
        readonly internal bool isTexture;
        readonly internal bool isSampler;
        readonly internal bool isStruct;
        readonly internal bool isReference;
        readonly internal bool isLiteral;

        readonly internal string referable;

        readonly internal bool isBareResource;

        readonly internal string knownIssue;

        private ParameterHeader(int a)
        {
            referenceName = null;
            displayName = null;
            shaderType = null;
            tooltip = null;
            isOutput = true;
            isInput = false;
            isColor = false;
            defaultValue = null;
            isStatic = false;
            isDropdown = false;
            options = null;
            isSlider = false;
            sliderMin = 0;
            sliderMax = 1;
            isBareResource = false;
            isLocal = false;
            isReference = false;
            isLiteral = false;
            isTexture = false;
            isSampler = false;
            isNumeric = false;
            isStruct = false;
            referable = null;
            defaultString = null;
            knownIssue = null;
            externalQualifiedTypeName = null;

            isValid = false;
        }

        // this is for return types.
        internal ParameterHeader(string referenceName, IShaderType shaderType, string tooltip = null) : this(0)
        {
            if (string.IsNullOrWhiteSpace(referenceName) || string.IsNullOrWhiteSpace(shaderType.Name))
            {
                isValid = false;
                return;
            }

            this.shaderType = shaderType;
            this.externalQualifiedTypeName = shaderType.Name;
            this.referenceName = referenceName;
            this.displayName = referenceName;
            this.tooltip = tooltip;

            string typeTest = shaderType.Name.ToLowerInvariant();

            isNumeric = HeaderUtils.TryParseTypeInfo(typeName, out _, out _, out _, out _, out _, out _, out _);
            isSampler = typeTest.Contains("sampler");
            isTexture = typeTest.Contains("texture");
            isBareResource = !typeTest.Contains("unity") && (isSampler || isTexture); // special handling case.            
            isStruct = !isTexture && !isSampler && !isNumeric;
            isValid = true;
        }

        internal ParameterHeader(IShaderField param, IShaderFunction owner) : this(param?.Name, param?.ShaderType)
        {
            if (!isValid || param == null)
                return;

            isOutput = isInput = false;
            isInput = param.IsInput;
            isOutput = param.IsOutput;

            if (!param.Hints.TryGetValue(Hints.Common.kDisplayName, out displayName))
                displayName = ObjectNames.NicifyVariableName(referenceName);

            HeaderUtils.TryParseTypeInfo(typeName, out string prim, out bool isScalar, out bool isVector, out bool isMatrix, out int rows, out int cols, out _);
            List<string> editorHintsUsed = new();


            isLiteral = param.Hints.TryGetValue(Hints.Param.kLiteral, out _); // TODO: Only relevant for int, float, half, uint

            if (param.Hints.TryGetValue(Hints.Param.kExternal, out externalQualifiedTypeName))
                externalQualifiedTypeName += $"::{typeName}";
            else externalQualifiedTypeName = typeName;

            if (param.Hints.TryGetValue(Hints.Param.kColor, out _))
            {
                editorHintsUsed.Add(Hints.Param.kColor);
                if (rows != 3 && rows != 4 && !isVector)
                {
                    knownIssue = $"'Color' hint on '{referenceName}' is not supported for '{typeName}'.";
                }
                else isColor = true;
            }

            if (param.Hints.TryGetValue(Hints.Param.kStatic, out _))
            {
                if (isOutput)
                {
                    knownIssue = $"'Static' hint on '{referenceName}' is not supported for 'out' parameters.";
                }
                else if (!isScalar && !isColor)
                {
                    knownIssue = $"'Static' hint on '{referenceName}' is not supported for '{typeName}'.";
                }
                else isStatic = true;
            }

            if (param.Hints.TryGetValue(Hints.Param.kLocal, out _))
            {
                editorHintsUsed.Add(Hints.Param.kLocal);
                if (isStatic) // All Access Modifier style hints would conflict with each other, but we currently only have Local and Static
                {
                    knownIssue = $"'Local' hint on '{referenceName}' conflicts with found 'Static' hint.";
                }
                else isLocal = true;
            }

            if (param.Hints.TryGetValue(Hints.Param.kDropdown, out var dropdownString))
            {
                editorHintsUsed.Add(Hints.Param.kDropdown);
                options = HeaderUtils.LazyTokenString(dropdownString);
                if (!isScalar || prim == "bool")
                {
                    knownIssue = $"'Dropdown' hint on '{referenceName}' is not supported for '{typeName}'.";
                }
                else if (options.Length == 0)
                {
                    knownIssue = $"'Dropdown' hint on '{referenceName}' has no options.";
                }
                else isDropdown = true;
            }

            if (param.Hints.TryGetValue(Hints.Param.kRange, out var rangeValues))
            {
                editorHintsUsed.Add(Hints.Param.kRange);
                var values = HeaderUtils.LazyTokenFloat(rangeValues);

                if (!isScalar || prim == "bool")
                {
                    knownIssue = $"'Range' hint on '{referenceName}' is not supported for '{typeName}'.";
                }
                else if (values.Length != 2 || values[0] == values[1])
                {
                    knownIssue = $"'Range' hint on '{referenceName}' expects two arguments of different values.";
                }
                else
                {
                    sliderMin = values[0] < values[1] ? values[0] : values[1];
                    sliderMax = values[0] > values[1] ? values[0] : values[1];
                    isSlider = true;
                }
            }

            if (param.Hints.TryGetValue(Hints.Param.kDefault, out defaultString))
            {
                defaultValue = HeaderUtils.LazyTokenFloat(defaultString);
            }

            if(editorHintsUsed.Count > 1)
            {
                StringBuilder sb = new();
                bool first = true;
                foreach(var hintName in editorHintsUsed)
                {
                    if (!first)
                        sb.Append(", ");
                    first = false;
                    sb.Append($"{hintName}");
                }

                knownIssue = $"Found multiple conflicting editor hints on '{referenceName}': '{sb.ToString()}'.";
            }
        }
    }
}
