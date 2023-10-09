using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [Flags]
    enum HLSLAccess
    {
        NONE = 0,
        IN = 1 << 0,
        OUT = 1 << 1,
        INOUT = IN | OUT,
    }

    interface IHLSMessage
    {
        string message { get; }
        VFXErrorType type { get; }
    }

    class HLSLAttributeError : IHLSMessage
    {
        private static readonly Dictionary<VFXAttributeMode, string> s_ErrorMapping = new()
        {
            { VFXAttributeMode.Write, "writable" },
            { VFXAttributeMode.Read, "readable" },
            { VFXAttributeMode.ReadWrite, "readable and writable" },
            { VFXAttributeMode.None, "found" },
        };

        public HLSLAttributeError(string name, VFXAttributeMode mode)
        {
            message = $"The attribute `{name}` is not {s_ErrorMapping[mode]}";
        }

        public string message { get; }
        public VFXErrorType type => VFXErrorType.Error;
    }

    class HLSLVFXAttributeAccessError : IHLSMessage
    {
        public string message => "Missing `inout` access modifier before the VFXAttributes type.\nNeeded because your code writes to at least one attribute.";
        public VFXErrorType type => VFXErrorType.Error;
    }

    class HLSLMissingAccessError : IHLSMessage
    {
        string m_Name;
        HLSLAccess[] m_ExpectedAccess;
        HLSLAccess m_Fallback;

        public HLSLMissingAccessError(string parameterName, HLSLAccess[] expectedAccess, HLSLAccess fallback = HLSLAccess.NONE)
        {
            m_Name = parameterName;
            m_ExpectedAccess = expectedAccess;
            m_Fallback = fallback;
        }

        public string message
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append($"Missing access modifier for parameter {m_Name}. Expected ");
                foreach (var access in m_ExpectedAccess)
                {
                    sb.Append($" or '{access.ToString().ToLower()}'");
                }
                if (m_Fallback != HLSLAccess.NONE)
                {
                    sb.AppendLine();
                    sb.Append($"Fallback to '{m_Fallback.ToString().ToLower()}'.");
                }

                return sb.ToString();
            }
        }

        public VFXErrorType type => VFXErrorType.Warning;
    }

    class HLSLMissingVFXAttribute : IHLSMessage
    {
        public string message => "Missing `VFXAttributes attributes` as function's parameter.\nNeeded because your code access (read or write) to at least one attribute.\nIt has been automatically fixed for you";
        public VFXErrorType type => VFXErrorType.Warning;
    }

    class HLSLUnknownParameterType : IHLSMessage
    {
        public HLSLUnknownParameterType(string type)
        {
            message = $"Unknown parameter type '{type}'";
        }
        public string message { get; }
        public VFXErrorType type => VFXErrorType.Error;
    }

    class HLSLTexture2DShouldNotBeUsed : IHLSMessage
    {
        public HLSLTexture2DShouldNotBeUsed(string paramName)
        {
            message = $"The function parameter '{paramName}' is of type Texture2D.\nPlease use VFXSampler2D type instead (see documentation)";
        }
        public string message { get; }
        public VFXErrorType type => VFXErrorType.Error;
    }

    class HLSLUnsupportedAttributes : IHLSMessage
    {
        public HLSLUnsupportedAttributes(IEnumerable<string> attributesName)
        {
            message = $"No VFXAttributes can be used here:\n\t{string.Join("\n\t", attributesName)}";
        }
        public string message { get; }
        public VFXErrorType type => VFXErrorType.Error;
    }

    class HLSLVoidReturnTypeUnsupported : IHLSMessage
    {
        public HLSLVoidReturnTypeUnsupported(string functionName)
        {
            message = $"HLSL function '{functionName}' must return a value";
        }
        public string message { get; }
        public VFXErrorType type => VFXErrorType.Error;
    }

    class HLSLVoidReturnTypeOnlyIsSupported : IHLSMessage
    {
        public HLSLVoidReturnTypeOnlyIsSupported(string functionName)
        {
            message = $"HLSL function '{functionName}' must return a 'void' type";
        }
        public string message { get; }
        public VFXErrorType type => VFXErrorType.Error;
    }

    class HLSLSameFunctionName : IHLSMessage
    {
        public HLSLSameFunctionName(string name)
        {
            message = $"Multiple functions with same name '{name}' are declared, only the first one can be selected";
        }
        public string message { get; }
        public VFXErrorType type => VFXErrorType.Warning;
    }

    class HLSLMissingFunction : IHLSMessage
    {
        public HLSLMissingFunction()
        {
            message = "No valid HLSL function has been provided. You should write at least one function that returns a value";
        }
        public string message { get; }
        public VFXErrorType type => VFXErrorType.Error;
    }

    class HLSLFunctionParameter
    {
        // Match inout/in/out accessor then any whitespace then the parameter type then optionally a template type any whitespace and then the parameter name
        static readonly Regex s_ParametersParser = new Regex(@"(?<access>inout|in|out)?\s*(?<type>\w+)(?:[<](?<template>\w+)[>])?\s*(?<parameter>\w+)(?:,\s*)?", RegexOptions.Compiled);

        readonly string m_RawCode;

        public Type type { get; }
        public string rawType { get; }
        public string name { get; }
        public string tooltip { get; }
        public string templatedType { get; }
        public HLSLAccess access { get; }
        public IReadOnlyCollection<IHLSMessage> errors { get; }

        public static IEnumerable<HLSLFunctionParameter> Parse(string hlsl, Dictionary<string, string> doc)
        {
            var matches = s_ParametersParser.Matches(hlsl.Trim());
            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                yield return new HLSLFunctionParameter(
                    match.Groups["access"].Value,
                    match.Groups["type"].Value,
                    match.Groups["template"].Value,
                    match.Groups["parameter"].Value,
                    doc.TryGetValue(match.Groups["parameter"].Value, out var tooltip) ? tooltip : null);
            }
        }

        HLSLFunctionParameter(string access, string type, string template, string name, string tooltip)
        {
            this.name = name;
            this.tooltip = tooltip;
            this.rawType = type;
            this.type = HLSLParser.HLSLToUnityType(this.rawType);
            this.templatedType = template;
            this.access = HLSLParser.HLSLAccessToEnum(access);
            this.m_RawCode = $"{access} {type}{(string.IsNullOrEmpty(this.templatedType) ?"" : $"<{template}>")} {name}";

            if (this.type == null)
            {
                errors = new[] { new HLSLUnknownParameterType(type) };
            }
        }

        public override string ToString() => m_RawCode;
    }

    class HLSLFunction
    {
        // Match all attributes not followed by a single '=' character and also catch ++ and -- prefix (for read+write case)
        static readonly string s_AttributeReadPattern = @"(?<op>\+\+|\-\-)?{0}.(?<name>\w+\b)(?!.*(?:[^=]=[^=]))";
        // Match all attributes followed by an assigment operator (=, ++, --, +=, -= ...) even if there's multiple assignments on the same line
        private static readonly string s_AttributeWritePattern = @"{0}.(?<name>\w+)(?:\.\w)?\s*(?<op>[\/\-+*%&\|\^]?=[^=]|(\+\+|\-\-|<<=|>>=))";
        // Simply match lines where a 'return' statement is used
        // Capture the function documentation, then return type then capture the function name then capture the function parameters and each body line
        static readonly Regex s_FunctionParser;
        // Capture VFXRAND and VFXFIXED_RAND patterns
        static readonly Regex s_RandMatcher = new Regex(@"VFX(?<fixed>FIXED_)?RAND\d?", RegexOptions.Compiled | RegexOptions.Multiline);
        // Look for all lines starting with /// then capture the parameter name and then the description (used as tooltip)
        static readonly Regex s_DocParser = new Regex(@"/{3}\s*(?<parameter>\w+)\s*:\s*(?<tooltip>.*)$", RegexOptions.Compiled | RegexOptions.Multiline);
        // Special doc instruction to prevent the function from being exposed
        static readonly Regex s_HiddenTagParser = new Regex(@"^/{3}\s*Hidden\s*$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        private List<HLSLFunctionParameter> m_Inputs;

        static HLSLFunction()
        {
            var supportedReturnTypes = string.Join("|", HLSLParser.s_KnownTypes.Keys);
            var pattern = @"^(?<doc>(^/{3}.*\n)*)(?<returnType>" + supportedReturnTypes + @")\s+(?<name>\w+)\((?<parameters>[^\)]+)\)\s*";
            s_FunctionParser = new Regex(pattern, RegexOptions.Compiled | RegexOptions.Multiline);
        }

        public static IEnumerable<HLSLFunction> Parse(string hlsl)
        {
            var matches = s_FunctionParser.Matches(hlsl.Trim());
            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var isHidden = s_HiddenTagParser.IsMatch(match.Groups["doc"].Value);
                if (!isHidden)
                {
                    var bodyStartIndex = match.Index + match.Length;
                    var bodyEndIndex = (i + 1) < matches.Count ? matches[i + 1].Index - 1 : hlsl.Length - 1;
                    var body = hlsl.Substring(bodyStartIndex, bodyEndIndex - bodyStartIndex + 1).TrimEnd();

                    yield return new HLSLFunction(
                        match.Index,
                        match.Groups["doc"].Value,
                        match.Groups["name"].Value,
                        match.Groups["returnType"].Value,
                        match.Groups["parameters"].Value,
                        body);
                }
            }
        }

        HLSLFunction(int matchIndex, string rawDoc, string name, string returnType, string parameters, string body)
        {
            var errors = new List<IHLSMessage>();
            this.index = matchIndex;
            this.name = name;
            this.rawReturnType = returnType;
            this.returnType = HLSLParser.HLSLToUnityType(returnType);
            var doc = this.GetDoc(rawDoc);
            this.m_Inputs = new List<HLSLFunctionParameter>(HLSLFunctionParameter.Parse(parameters, doc));
            this.body = body.Trim('\n');
            this.attributes = new List<VFXAttributeInfo>(GetAttributes(this.body, errors));
            this.errorList = errors;
        }

        public int index { get; }
        public string name { get; }
        public Type returnType { get; }
        public IReadOnlyCollection<HLSLFunctionParameter> inputs => m_Inputs;
        public IReadOnlyCollection<VFXAttributeInfo> attributes { get; }
        public string rawReturnType { get; }
        public string body { get; }
        public IReadOnlyCollection<IHLSMessage> errorList { get; }

        public string GetTransformedHLSL()
        {
            var transformedBody = new StringBuilder();
            var hlslType = HLSLParser.UnityHLSLType(returnType);

            transformedBody.Append($"{hlslType} {GetNameWithHashCode()}(");

            transformedBody.AppendJoin(", ", inputs);
            transformedBody.AppendLine(")");
            transformedBody.AppendLine(body);

            return transformedBody.ToString();
        }

        public string GetNameWithHashCode() => $"{name}_{GetHashCode():X}";

        private Dictionary<string, string> GetDoc(string rawDoc)
        {
            var doc = new Dictionary<string, string>();
            foreach (var m in s_DocParser.Matches(rawDoc))
            {
                var match = (Match)m;
                doc[match.Groups["parameter"].Value] = match.Groups["tooltip"].Value;
            }

            return doc;
        }

        private IEnumerable<VFXAttributeInfo> GetAttributes(string hlsl, List<IHLSMessage> errorList)
        {
            var attributeVariable = m_Inputs.Find(x => x.type == typeof(VFXAttribute))?.name;
            if (string.IsNullOrEmpty(attributeVariable))
            {
                yield break;
            }

            var attributeReadMatcher = new Regex(string.Format(s_AttributeReadPattern, attributeVariable), RegexOptions.Multiline);
            var attributeWriteMatcher = new Regex(string.Format(s_AttributeWritePattern, attributeVariable), RegexOptions.Multiline);
            var writeAttributes = new HashSet<string>();
            var readAttributes = new HashSet<string>();
            foreach (Match read in attributeReadMatcher.Matches(hlsl))
            {
                var attribute = read.Groups["name"];
                if (attribute.Success)
                {
                    readAttributes.Add(attribute.Value);
                }

                // For assignment of the form ++x or --x the attribute is both read and write
                var op = read.Groups["op"];
                if (op.Success && op.Value.Trim().Length == 2)
                {
                    writeAttributes.Add(attribute.Value);
                }
            }

            foreach (Match read in attributeWriteMatcher.Matches(hlsl))
            {
                var attribute = read.Groups["name"];
                if (attribute.Success)
                {
                    writeAttributes.Add(attribute.Value);
                }

                // For assignment of the form x++, x--, x +=, x -= etc... the attribute is both read and write
                var op = read.Groups["op"];
                if (op.Success && op.Value.Trim().Length > 1)
                {
                    readAttributes.Add(attribute.Value);
                }
            }

            // Handle VFXRAND and VFXFIXED_RAND required attributes
            var randMatches = s_RandMatcher.Matches(hlsl);
            if (randMatches.Count > 0)
            {
                readAttributes.Add(VFXAttribute.Seed.name);
                for (int i = 0; i < randMatches.Count; i++)
                {
                    if (randMatches[i].Groups["fixed"].Success)
                    {
                        readAttributes.Add(VFXAttribute.ParticleId.name);
                        break;
                    }
                }
            }

            // Read and Write attributes
            foreach (var readWriteAttribute in writeAttributes)
            {
                if (!readAttributes.Contains(readWriteAttribute))
                {
                    continue;
                }

                var attribute = VFXAttribute.FindWithMode(readWriteAttribute, VFXAttributeMode.ReadWrite);
                if (!string.IsNullOrEmpty(attribute.name))
                {
                    yield return new VFXAttributeInfo(attribute, VFXAttributeMode.ReadWrite);
                }
                else
                {
                    errorList.Add(new HLSLAttributeError(readWriteAttribute, Array.FindIndex(VFXAttribute.All, x => x == readWriteAttribute) != -1 ? VFXAttributeMode.ReadWrite : VFXAttributeMode.None));
                }
            }

            // Write attributes
            foreach (var writeAttribute in writeAttributes)
            {
                if (readAttributes.Contains(writeAttribute))
                {
                    continue;
                }

                var attribute = VFXAttribute.FindWithMode(writeAttribute, VFXAttributeMode.Write);
                if (!string.IsNullOrEmpty(attribute.name))
                {
                    yield return new VFXAttributeInfo(attribute, VFXAttributeMode.Write);
                }
                else
                {
                    errorList.Add(new HLSLAttributeError(writeAttribute, Array.FindIndex(VFXAttribute.All, x => x == writeAttribute) != -1 ? VFXAttributeMode.Write : VFXAttributeMode.None));
                }
            }

            // Read attributes
            foreach (var readAttribute in readAttributes)
            {
                if (writeAttributes.Contains(readAttribute))
                {
                    continue;
                }

                var attribute = VFXAttribute.FindWithMode(readAttribute, VFXAttributeMode.Read);
                if (!string.IsNullOrEmpty(attribute.name))
                {
                    yield return new VFXAttributeInfo(attribute, VFXAttributeMode.Read);
                }
                else
                {
                    errorList.Add(new HLSLAttributeError(readAttribute, Array.FindIndex(VFXAttribute.All, x => x == readAttribute) != -1 ? VFXAttributeMode.Read : VFXAttributeMode.None));
                }
            }
        }
    }

    static class HLSLParser
    {
        public static readonly Dictionary<string, Type> s_KnownTypes = new()
        {
            { "void", typeof(void) },
            { "float", typeof(float) },
            { "float2", typeof(Vector2) },
            { "float3", typeof(Vector3) },
            { "float4", typeof(Vector4) },
            { "float4x4", typeof(Matrix4x4) },
            { "Texture2D", typeof(Texture2D) },
            { "VFXSampler2D", typeof(Texture2D) },
            { "VFXSampler3D", typeof(Texture3D) },
            { "VFXGradient", typeof(Gradient) },
            { "VFXCurve", typeof(AnimationCurve) },
            { "bool", typeof(bool) },
            { "uint", typeof(uint) },
            { "int", typeof(int) },
            { "StructuredBuffer", typeof(GraphicsBuffer) },
            { "ByteAddressBuffer", typeof(GraphicsBuffer) },
            { "VFXAttributes", typeof(VFXAttribute) },
        };

        static readonly Dictionary<string, HLSLAccess> s_AccessMap = new()
        {
            { "", HLSLAccess.NONE },
            { "in", HLSLAccess.IN },
            { "out", HLSLAccess.OUT },
            { "inout", HLSLAccess.INOUT },
        };

        static readonly Regex s_IncludeParser = new Regex(@"^#include ""(?<filepath>.*)""", RegexOptions.Compiled);
        static readonly Regex s_MultilineCommentsParser = new Regex(@"/\*[\s\S]*?\*/", RegexOptions.Compiled|RegexOptions.Multiline);
        static readonly Regex s_SinglelineCommentsParser = new Regex(@"^\s*/{2}[^/].*$", RegexOptions.Compiled|RegexOptions.Multiline|RegexOptions.IgnorePatternWhitespace);

        public static Type HLSLToUnityType(string type)
        {
            if (s_KnownTypes.TryGetValue(type, out var value))
            {
                return value;
            }

            return null;
        }

        public static string UnityHLSLType(Type type)
        {
            foreach (var knownType in s_KnownTypes)
            {
                if (knownType.Value == type)
                    return string.IsNullOrEmpty(knownType.Key)
                        ? null
                        : knownType.Key;
            }
            return null;
        }

        public static HLSLAccess HLSLAccessToEnum(string access) => s_AccessMap[access];

        public static IEnumerable<string> ParseIncludes(string hlsl)
        {
            foreach (var include in s_IncludeParser.Matches(hlsl))
            {
                yield return ((Match)include).Groups["filepath"].Value;
            }
        }

        public static string StripCommentedCode(string hlsl)
        {
            return string.IsNullOrEmpty(hlsl)
                ? string.Empty
                : s_SinglelineCommentsParser.Replace(s_MultilineCommentsParser.Replace(hlsl, string.Empty), string.Empty);
        }
    }
}
