using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem.Hints
{
    internal static class Param
    {
        internal const string kAccessModifier = "AccessModifier";
        internal const string kCustomEditor = "CustomEditor";

        internal const string kStatic = "sg:Static";
        internal const string kLocal = "sg:Local";
        internal const string kLiteral = "sg:Literal";
        internal const string kColor = "sg:Color";
        internal const string kRange = "sg:Range";
        internal const string kDropdown = "sg:Dropdown";
        internal const string kDefault = "sg:Default";
        internal const string kExternal = "sg:External";

        // Not yet implemented.
        internal const string kSetting = "sg:Setting";
        internal const string kLinkage = "sg:Linkage";
        internal const string kPrecision = "sg:Precision";
        internal const string kDynamic = "sg:Dynamic";
        internal const string kReferable = "sg:Referable";        

        // Hard coded referables, not yet implemented.
        internal const string kUV = "sg:ref:UV";
        internal const string kPosition = "sg:ref:Position";
        internal const string kNormal = "sg:ref:Normal";
        internal const string kBitangent = "sg:ref:Bitangent";
        internal const string kTangent = "sg:ref:Tangent";
        internal const string kViewDirection = "sg:ref:ViewDirection";
        internal const string kScreenPosition = "sg:ref:ScreenPosition";
        internal const string kVertexColor = "sg:ref:VertexColor";
    }

    internal class Range : IStrongHint<IShaderField>
    {
        public string Key => Param.kRange;
        public IReadOnlyCollection<string> Conflicts { get; } = new string[] { Param.kCustomEditor };
        public IReadOnlyCollection<string> Synonyms { get; } = new string[] { "Slider" };

        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg)
        {
            msg = null;
            value = rawValue;
            if (!found)
                return false;

            if (obj.ShaderType.Name != "half" && obj.ShaderType.Name != "float")
            {
                msg = $"Expected floating point scalar, but found '{obj.ShaderType.Name}'.";
                return false;

            }

            float min = 0;
            float max = 1;

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                value = new float[] { min, max };
                return true;
            }

            float[] values = HeaderUtils.LazyTokenFloat(rawValue);

            if (values.Length > 2 || !string.IsNullOrEmpty(rawValue) && values.Length == 0)
            {
                msg = $"Expected 0, 1, or 2 floating point values, but found '{values.Length}'.";
                value = new float[] { min, max };
                return false;
            }

            if (values.Length == 1)
            {
                if (values[0] < 0)
                {
                    min = values[0];
                    max = 0;
                }
                else max = values[0];
            }
            else if (values.Length >= 2)
            {
                min = Mathf.Min(values);
                max = Mathf.Max(values);
            }

            if (min == max)
            {
                msg = $"Expected min and max to be different values, but both are '{min}'.";
            }

            value = new float[2] { min, max };
            return true;
        }
    }

    internal class Dropdown : IStrongHint<IShaderField>
    {
        public string Key => Param.kDropdown;
        public IReadOnlyCollection<string> Conflicts { get; } = new string[] { Param.kCustomEditor };
        public IReadOnlyCollection<string> Synonyms { get; } = new string[] { "Enum" };

        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg)
        {
            msg = null;
            value = rawValue;
            if (!found)
            {
                return false;
            }

            switch(obj.ShaderType.Name)
            {
                case "int": case "uint": case "float": case "half": break;
                default:
                    msg = $"Expected numeric scalar, but found '{obj.ShaderType.Name}'.";
                    return false;
            }

            string[] options = HeaderUtils.LazyTokenString(rawValue);

            if (options.Length == 0)
            {
                msg = $"Expected at least 1 comma separated option, but found none.";
                return false;
            }

            value = options;
            return true;
        }
    }

    internal class Color : IStrongHint<IShaderField>
    {
        public string Key => Param.kColor;
        public IReadOnlyCollection<string> Conflicts { get; } = new string[] { Param.kCustomEditor };

        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg)
        {
            msg = null;
            value = rawValue;
            if (found)
            {
                switch(obj.ShaderType.Name)
                {
                    case "float3": case "float4":  case "half3": case "half4": return true;
                    default:
                        msg = $"Expected floating point vector of length 3 or 4, but found '{obj.ShaderType.Name}'.";
                        return false;
                }
            }
            return false;
        }
    }

    internal class Literal : IStrongHint<IShaderField>
    {
        public string Key => Param.kLiteral;
        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg)
        {
            msg = null;
            value = rawValue;

            if (found)
            {
                switch (obj.ShaderType.Name)
                {
                    case "float": case "half": case "int": case "uint": return true;
                    default:
                        msg = $"Expected numeric scalar, but found '{obj.ShaderType.Name}'.";
                        return false;
                }
            }
            return found;
        }
    }

    internal class Static : IStrongHint<IShaderField>
    {
        public string Key => Param.kStatic;
        public IReadOnlyCollection<string> Conflicts { get; } = new string[] { Param.kAccessModifier };

        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg)
        {
            msg = null;
            value = rawValue;
            if (!found)
                return false;

            switch(obj.ShaderType.Name)
            {
                case "float": case "half": case "int": case "uint": case "bool":
                    return true;

                case "float3": case "float4": case "half3": case "half4":
                    if (obj.Hints.ContainsKey(Param.kColor))
                        return true;

                    msg = $"Requires '{Param.kColor}' to support '{obj.ShaderType.Name}'.";
                    return false;

                default:
                    msg = $"Expected '{Param.kColor}' or scalar, but found '{obj.ShaderType.Name}'.";
                    return false;
            }
        }
    }

    internal class External : IStrongHint<IShaderField>
    {
        public string Key => Param.kExternal;
        public IReadOnlyCollection<string> Synonyms { get; } = new string[] { "ExternalNamespace" };

        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg)
        {
            msg = null;
            value = rawValue;
            return true;
        }
    }

    internal class Default : IStrongHint<IShaderField>
    {
        public string Key => Param.kDefault;

        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg)
        {
            // TODO(SVFXG-868): This is tricky because there are multiple formats we need to support.
            // for now, we can pass along the raw value and allow the header to process it.
            msg = null;
            value = rawValue;
            return found;
        }
    }
}
