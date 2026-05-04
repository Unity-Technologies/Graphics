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

        internal const string kSetting = "sg:Setting";
        internal const string kLinkage = "sg:Linkage";
        internal const string kDynamic = "sg:DynamicVector";
        internal const string kReferable = "sg:Referable";

        internal const string kCustomBinding = "sg:CustomBinding";

        internal static class Ref
        {
            internal const string kUV = "UV";
            internal const string kPosition = "Position";
            internal const string kNormal = "Normal";
            internal const string kBitangent = "Bitangent";
            internal const string kTangent = "Tangent";
            internal const string kViewDirection = "ViewDirection";
            internal const string kScreenPosition = "ScreenPosition";
            internal const string kVertexColor = "VertexColor";
        }
    }

    internal class Range : IStrongHint<IShaderField>
    {
        public string Key => Param.kRange;
        public IReadOnlyCollection<string> Conflicts { get; } = new string[] { Param.kCustomEditor };
        public IReadOnlyCollection<string> Synonyms { get; } = new string[] { "Slider" };

        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg, string actualHintKey)
        {
            msg = null;
            value = rawValue;
            if (!found)
                return false;

            if (!obj.IsInput)
            {
                msg = $"Expected input parameter.";
                return false;
            }

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

        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg, string actualHintKey)
        {
            msg = null;
            value = rawValue;
            if (!found)
                return false;

            if (!obj.IsInput)
            {
                msg = $"Expected input parameter.";
                return false;
            }

            switch (obj.ShaderType.Name)
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

        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg, string actualHintKey)
        {
            msg = null;
            value = rawValue;
            if (found)
            {
                if (!obj.IsInput)
                {
                    msg = $"Expected input parameter.";
                    return false;
                }

                switch (obj.ShaderType.Name)
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
        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg, string actualHintKey)
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

        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg, string actualHintKey)
        {
            msg = null;
            value = rawValue;
            if (!found)
                return false;

            if (!obj.IsInput)
            {
                msg = $"Expected input parameter.";
                return false;
            }

            switch (obj.ShaderType.Name)
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

        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg, string actualHintKey)
        {
            msg = null;
            value = rawValue;
            return true;
        }
    }

    internal class Default : IStrongHint<IShaderField>
    {
        public string Key => Param.kDefault;

        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg, string actualHintKey)
        {
            // TODO(SVFXG-868): This is tricky because there are multiple formats we need to support.
            // for now, we can pass along the raw value and allow the header to process it.
            msg = null;
            value = rawValue;

            if (!obj.IsInput)
            {
                msg = $"Expected input parameter.";
                return false;
            }

            return found;
        }
    }

    internal class Dynamic : IStrongHint<IShaderField>
    {
        public string Key => Param.kDynamic;

        public IReadOnlyCollection<string> Conflicts { get; } = new string[] { Param.kCustomEditor };
        public IReadOnlyCollection<string> Synonyms { get; } = new string[] { "Dynamic" };

        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg, string actualHintKey)
        {
            msg = null;
            value = rawValue;
            if (!found)
                return false;

            switch(obj.ShaderType.Name)
            {
                case "float": case "float2": case "float3": case "float4":
                case "half": case "half2": case "half3": case "half4":
                    return true;
                default:
                    msg = $"Expected floating point vector or scalar, but found {obj.ShaderType.Name}.";
                    return false;
            }
        }
    }

    internal class Linkage : IStrongHint<IShaderField>
    {
        public string Key => Param.kLinkage;
        public IReadOnlyCollection<string> Conflicts { get; } = new string[] { Param.kAccessModifier };

        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg, string actualHintKey)
        {
            value = rawValue;
            msg = null;
            if (!found)
                return false;

            if (!obj.IsInput)
            {
                msg = $"Expected input parameter.";
                return false;
            }

            if (obj.ShaderType.Name != "bool")
            {
                msg = $"Expected type bool, but found '{obj.ShaderType.Name}'.";
                return false;
            }

            var funcProvider = provider as IProvider<IShaderFunction>;
            if (funcProvider == null)
            {
                msg = "Expected to be associated with a function.";
                return false;
            }

            if (rawValue == obj.Name)
            {
                msg = $"Cannot check linkage state against itself.";
                return false;
            }

            var func = funcProvider.Definition;

            bool hasMatch = false;
            foreach(var param in func.Parameters)
                if (param.Name == rawValue)
                {
                    hasMatch = true;
                    break;
                }

            if (!hasMatch)
            {
                msg = $"Could not find expected parameter '{rawValue}'.";
                return false;
            }
            return true;
        }
    }

    internal class Referable : IStrongHint<IShaderField>
    {
        public string Key => Param.kReferable;

        static HashSet<string> s_commonReferables;

        private static HashSet<string> GetCommonReferables()
        {
            if (s_commonReferables == null)
            {
                 s_commonReferables = new HashSet<string>() {
                    Param.Ref.kUV, // only supports vec2

                    // vec3 only
                    Param.Ref.kPosition,
                    Param.Ref.kTangent,
                    Param.Ref.kNormal,
                    Param.Ref.kBitangent,
                    Param.Ref.kViewDirection,

                    // vec4 only
                    Param.Ref.kVertexColor,
                    Param.Ref.kScreenPosition,
                };
            }
            return s_commonReferables;
        }

        public IReadOnlyCollection<string> Synonyms => GetCommonReferables();
        public IReadOnlyCollection<string> Conflicts { get; } = new string[] { Param.kCustomEditor };

        // This hint allows `sg:Referable(UV)` or just `UV`; this is a case where the synonym is _functional_.
        // Note also that this hint relies on the Default hint to be used in tandem to set the initial referable value (ie. space or channel);
        // When we move to GTK, we should allow the default value to be handled as well.

        // Another note: we would rather query the Interface/Target for available referables and their possible values.
        // We'd need a way to push that information down (ie. an untyped context object that can be passed).
        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg, string actualHintKey)
        {
            msg = null;
            value = Key == actualHintKey ? rawValue : actualHintKey;

            if (!obj.IsInput)
            {
                msg = $"Expected input parameter.";
                return false;
            }

            if (value == null)
            {
                msg = $"Could not resolve a referable type.";
                return false;
            }
            else if (!GetCommonReferables().Contains((string)value))
            {
                msg = $"'{value}' is not a supported referable key.";
                return false;
            }

            // TODO: The underlying type shouldn't matter, but SG only supports these configurations at the moment.
            switch ((string)value)
            {
                case Param.Ref.kUV:
                    if (obj.ShaderType.Name != "half2" && obj.ShaderType.Name != "float2")
                    {
                        msg = $"'{(string)value}' expects floating point vector of length 2, but found '{obj.ShaderType.Name}'.";
                        return false;
                    }
                    break;
                case Param.Ref.kVertexColor:
                case Param.Ref.kScreenPosition:
                    if (obj.ShaderType.Name != "half4" && obj.ShaderType.Name != "float4")
                    {
                        msg = $"'{(string)value}' expects floating point vector of length 4, but found '{obj.ShaderType.Name}'.";
                        return false;
                    }
                    break;
                default:
                    if (obj.ShaderType.Name != "half3" && obj.ShaderType.Name != "float3")
                    {
                        msg = $"'{(string)value}' expects floating point vector of length 3, but found '{obj.ShaderType.Name}'.";
                        return false;
                    }
                    break;
            }
            return true;
        }
    }

    internal class CustomBinding : IStrongHint<IShaderField>
    {
        public string Key => Param.kCustomBinding;
        public IReadOnlyCollection<string> Conflicts { get; } = new string[] { Param.kCustomEditor };

        public bool Process(bool found, string rawValue, IShaderField obj, IProvider provider, out object value, out string msg, string actualHintKey)
        {
            value = rawValue;
            msg = null;
            if (!found)
                return false;

            if (!obj.IsInput)
            {
                msg = $"Expected input parameter.";
                return false;
            }

            if (string.IsNullOrEmpty(rawValue))
            {
                msg = "Invalid value.";
                return false;
            }

            return true;
        }
    }

}
