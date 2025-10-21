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

    class HLSLVFXAttributeIsVariadic : IHLSMessage
    {
        private string attribute;
        public HLSLVFXAttributeIsVariadic(string attribute)
        {
            this.attribute = attribute;
        }
        public string message => $"Variadic attribute \"{attribute}\" is not supported in custom hlsl code, use {attribute}X, {attribute}Y or {attribute}Z";
        public VFXErrorType type => VFXErrorType.Error;
    }

    class HLSLVFXAttributeAccessError : IHLSMessage
    {
        public string message => "Missing `inout` access modifier before the VFXAttributes type.\nNeeded because your code writes to at least one attribute.";
        public VFXErrorType type => VFXErrorType.Error;
    }

    class HLSLMissingVFXAttribute : IHLSMessage
    {
        public string message => "Missing `VFXAttributes attributes` as function's parameter";
        public VFXErrorType type => VFXErrorType.Error;
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

    class HLSLOutParameterNotAllowed : IHLSMessage
    {
        public HLSLOutParameterNotAllowed(string parameterName)
        {
            message = $"Parameter {parameterName} has out or inout access which is not allowed for Custom HLSL block";
        }
        public string message { get; }
        public VFXErrorType type => VFXErrorType.Error;
    }

    class HLSLMissingIncludeFile : IHLSMessage
    {
        public HLSLMissingIncludeFile(string filePath)
        {
            message = $"Couldn't open include file '{filePath}'.";
        }
        public string message { get; }
        public VFXErrorType type => VFXErrorType.Error;
    }

    class HLSLFunctionParameter
    {
        // Match inout/in/out accessor then any whitespace then the parameter type then optionally a template type any whitespace and then the parameter name
        static readonly Regex s_ParametersParser = new Regex(@"(?<access>(inout|in|out)\b)?\s*(?<type>\w+)(?:[<](?<template>\w+)[>])?\s*(?<parameter>\w+)(?:\s*,\s*)?", RegexOptions.Compiled);

        readonly string m_RawCode;

        public Type type { get; }
        public string rawType { get; }
        public string name { get; }
        public string tooltip { get; }
        public BufferType bufferType { get; }
        public string templatedRawType { get; }
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
                    doc.GetValueOrDefault(match.Groups["parameter"].Value));
            }
        }

        static Type GetTypeFromName(string name)
        {
            foreach (var type in VFXLibrary.GetGraphicsBufferType())
            {
                if (string.CompareOrdinal(name, type.Name) == 0)
                    return type;
            }

            //Some type aren't listed but still supported in CustomHLSL like StructuredBuffer<uint4>
            return typeof(void);
        }

        HLSLFunctionParameter(string access, string type, string template, string name, string tooltip)
        {
            this.name = name;
            this.tooltip = tooltip;
            this.rawType = type;
            this.type = HLSLParser.HLSLToUnityType(this.rawType);

            if (this.type != null && typeof(Texture).IsAssignableFrom(this.type))
            {
                if (Enum.TryParse<BufferType.Container>(rawType, out var container))
                {
                    this.bufferType = new BufferType(container, template, GetTypeFromName(template));
                }
            }
            else if (this.type == typeof(GraphicsBuffer))
            {
                if (!Enum.TryParse<BufferType.Container>(rawType, out var container))
                    throw new InvalidOperationException("Unknown container type: " + rawType);

                this.bufferType = new BufferType(container, template, GetTypeFromName(template));
            }

            this.access = HLSLParser.HLSLAccessToEnum(access);
            this.templatedRawType = string.IsNullOrEmpty(template) ? this.rawType : $"{type}<{template}>";
            this.m_RawCode = $"{access} {this.templatedRawType} {name}";

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
        static readonly string s_AttributeReadPattern = @"(?<op>\+{{2}}|-{{2}})?{0}.(?<name>\w+\b)(?!(?:\.\w+)?\s*[^=<>+-]=[^=])";
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
            var pattern = @"^(?<doc>(^/{3}.*\n)*)(?<returnType>" + supportedReturnTypes + @")\s+(?<name>\w+)\((?<parameters>[^\)]*)\)\s*";
            s_FunctionParser = new Regex(pattern, RegexOptions.Compiled | RegexOptions.Multiline);
        }

        public static IEnumerable<HLSLFunction> Parse(IVFXAttributesManager attributesManager, string hlsl)
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

                    yield return new HLSLFunction(attributesManager,
                        match.Index,
                        match.Groups["doc"].Value,
                        match.Groups["name"].Value,
                        match.Groups["returnType"].Value,
                        match.Groups["parameters"].Value,
                        body);
                }
            }
        }

        HLSLFunction(IVFXAttributesManager attributesManager, int matchIndex, string rawDoc, string name, string returnType, string parameters, string body)
        {
            var errors = new List<IHLSMessage>();
            this.index = matchIndex;
            this.name = name;
            this.rawReturnType = returnType;
            this.returnType = HLSLParser.HLSLToUnityType(returnType);
            var doc = this.GetDoc(rawDoc);
            if (this.returnType != null && this.returnType != typeof(void))
            {
                this.returnName = doc.GetValueOrDefault("return", "out");
            }
            this.m_Inputs = new List<HLSLFunctionParameter>(HLSLFunctionParameter.Parse(parameters, doc));
            this.body = body.Trim('\n');
            this.attributes = new List<VFXAttributeInfo>(GetAttributes(attributesManager, this.body, errors));
            this.errorList = errors;
        }

        public int index { get; }
        public string name { get; }
        public string returnName { get; }
        public Type returnType { get; }
        public IReadOnlyCollection<HLSLFunctionParameter> inputs => m_Inputs;
        public IReadOnlyCollection<VFXAttributeInfo> attributes { get; }
        public string rawReturnType { get; }
        public string body { get; }
        public IReadOnlyCollection<IHLSMessage> errorList { get; }

        public string GetTransformedHLSL(string suffix)
        {
            var transformedBody = new StringBuilder();
            var hlslType = HLSLParser.UnityHLSLType(returnType);

            transformedBody.Append($"{hlslType} {GetNameWithHashCode(suffix)}(");

            transformedBody.AppendJoin(", ", inputs);
            transformedBody.AppendLine(")");
            transformedBody.AppendLine(body);

            return transformedBody.ToString();
        }

        public string GetNameWithHashCode(string suffix) => $"{name}_{body.GetHashCode():X}_{suffix}";

        private Dictionary<string, string> GetDoc(string rawDoc)
        {
            var doc = new Dictionary<string, string>();
            foreach (var m in s_DocParser.Matches(rawDoc))
            {
                var match = (Match)m;
                doc[match.Groups["parameter"].Value] = match.Groups["tooltip"].Value.TrimEnd('\n', '\r');
            }

            return doc;
        }

        private IEnumerable<VFXAttributeInfo> GetAttributes(IVFXAttributesManager attributesManager, string hlsl, List<IHLSMessage> errorList)
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

            var allAttributes = new List<string>(attributesManager.GetAllNamesOrCombination(false, true, true, true));
            // Read and Write attributes
            foreach (var readWriteAttribute in writeAttributes)
            {
                if (!readAttributes.Contains(readWriteAttribute))
                {
                    continue;
                }

                if (attributesManager.TryFindWithMode(readWriteAttribute, VFXAttributeMode.ReadWrite, out var attribute) && attribute.variadic != VFXVariadic.True)
                {
                    yield return new VFXAttributeInfo(attribute, VFXAttributeMode.ReadWrite);
                }
                else
                {
                    IHLSMessage error = attribute.variadic == VFXVariadic.True
                        ? new HLSLVFXAttributeIsVariadic(attribute.name)
                        : new HLSLAttributeError(readWriteAttribute, allAttributes.FindIndex(x => x == readWriteAttribute) != -1 ? VFXAttributeMode.ReadWrite : VFXAttributeMode.None);
                    errorList.Add(error);
                }
            }

            // Write attributes
            foreach (var writeAttribute in writeAttributes)
            {
                if (readAttributes.Contains(writeAttribute))
                {
                    continue;
                }

                if (attributesManager.TryFindWithMode(writeAttribute, VFXAttributeMode.Write, out var attribute) && attribute.variadic != VFXVariadic.True)
                {
                    yield return new VFXAttributeInfo(attribute, VFXAttributeMode.Write);
                }
                else
                {
                    IHLSMessage error = attribute.variadic == VFXVariadic.True
                        ? new HLSLVFXAttributeIsVariadic(attribute.name)
                        : new HLSLAttributeError(writeAttribute, allAttributes.FindIndex(x => x == writeAttribute) != -1 ? VFXAttributeMode.Write : VFXAttributeMode.None);
                    errorList.Add(error);
                }
            }

            // Read attributes
            foreach (var readAttribute in readAttributes)
            {
                if (writeAttributes.Contains(readAttribute))
                {
                    continue;
                }

                if (attributesManager.TryFindWithMode(readAttribute, VFXAttributeMode.Read, out var attribute) && attribute.variadic != VFXVariadic.True)
                {
                    yield return new VFXAttributeInfo(attribute, VFXAttributeMode.Read);
                }
                else
                {
                    IHLSMessage error = attribute.variadic == VFXVariadic.True
                        ? new HLSLVFXAttributeIsVariadic(attribute.name)
                        : new HLSLAttributeError(readAttribute, allAttributes.FindIndex(x => x == readAttribute) != -1 ? VFXAttributeMode.Read : VFXAttributeMode.None);
                    errorList.Add(error);
                }
            }
        }
    }

    static class HLSLParser
    {
        public static readonly Dictionary<string, Type> s_KnownTypes = ComputeKnownTypes();

        private static Dictionary<string, Type> ComputeKnownTypes()
        {
            var knownTypes = new Dictionary<string, Type>()
            {
                { "void", typeof(void) },
                { "float", typeof(float) },
                { "float2", typeof(Vector2) },
                { "float3", typeof(Vector3) },
                { "float4", typeof(Vector4) },
                { "float4x4", typeof(Matrix4x4) },
                { "VFXSampler2D", typeof(Texture2D) },
                { "VFXSampler3D", typeof(Texture3D) },
                { "VFXSampler2DArray", typeof(Texture2DArray) },
                { "VFXSamplerCube", typeof(Cubemap) },
                //{ "VFXSamplerCubeArray", typeof(CubemapArray) },
                { "VFXGradient", typeof(Gradient) },
                { "VFXCurve", typeof(AnimationCurve) },
                { "bool", typeof(bool) },
                { "uint", typeof(uint) },
                { "int", typeof(int) },
                { "VFXAttributes", typeof(VFXAttribute) },
            };

            foreach (BufferType.Container bufferContainer in Enum.GetValues(typeof(BufferType.Container)))
            {
                Type type;
                switch (bufferContainer)
                {
                    case BufferType.Container.Texture1D:
                    case BufferType.Container.RWTexture1D:
                        type = typeof(Texture2D);
                        break;
                    case BufferType.Container.Texture1DArray:
                    case BufferType.Container.RWTexture1DArray:
                        type = typeof(Texture2DArray);
                        break;
                    case BufferType.Container.Texture2D:
                    case BufferType.Container.RWTexture2D:
                        type = typeof(Texture2D);
                        break;
                    case BufferType.Container.Texture2DArray:
                    case BufferType.Container.RWTexture2DArray:
                        type = typeof(Texture2DArray);
                        break;
                    case BufferType.Container.Texture3D:
                    case BufferType.Container.RWTexture3D:
                        type = typeof(Texture3D);
                        break;
                    case BufferType.Container.TextureCube:
                    case BufferType.Container.RWTextureCube:
                        type = typeof(Cubemap);
                        break;
                    case BufferType.Container.TextureCubeArray:
                    case BufferType.Container.RWTextureCubeArray:
                        type = typeof(CubemapArray);
                        break;
                    default:
                        type = typeof(GraphicsBuffer);
                        break;
                }
                knownTypes.Add(bufferContainer.ToString(), type);
            }

            return knownTypes;
        }

        static readonly Dictionary<string, HLSLAccess> s_AccessMap = new()
        {
            { "", HLSLAccess.NONE },
            { "in", HLSLAccess.IN },
            { "out", HLSLAccess.OUT },
            { "inout", HLSLAccess.INOUT },
        };

        static readonly Regex s_IncludeParser = new Regex(@"^#include ""(?<filepath>.*)""", RegexOptions.Compiled | RegexOptions.Multiline);

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
    }
}
