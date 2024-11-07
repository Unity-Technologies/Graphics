using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEngine.VFX;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;

namespace UnityEditor.VFX
{
    static class VFXCodeGeneratorHelper
    {
        private static readonly char[] kAlpha = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
        public static string GeneratePrefix(uint index)
        {
            if (index == 0u) return "a";

            var prefix = "";
            while (index != 0u)
            {
                prefix = kAlpha[index % kAlpha.Length] + prefix;
                index /= (uint)kAlpha.Length;
            }
            return prefix;
        }
    }

    interface IHLSLCodeHolder : IEquatable<IHLSLCodeHolder>
    {
        IEnumerable<string> includes { get; }
        ShaderInclude shaderFile { get; }
        string sourceCode { get; set; }
        string customCode { get; }
        bool HasShaderFile();
    }

    class VFXShaderWriter
    {
        public VFXShaderWriter()
        { }

        public VFXShaderWriter(string initialValue)
        {
            builder.Append(initialValue);
        }

        public static string GetValueString(VFXValueType type, object value)
        {
            var format = "";
            switch (type)
            {
                case VFXValueType.Boolean:
                case VFXValueType.Int32:
                case VFXValueType.Uint32:
                case VFXValueType.Float:
                    format = "({0}){1}";
                    break;
                case VFXValueType.Float2:
                case VFXValueType.Float3:
                case VFXValueType.Float4:
                case VFXValueType.Matrix4x4:
                    format = "{0}{1}";
                    break;
                default: throw new Exception("GetValueString missing type: " + type);
            }
            // special cases of ToString
            switch (type)
            {
                case VFXValueType.Boolean:
                    value = value.ToString().ToLower();
                    break;
                case VFXValueType.Float:
                    value = FormatFloat((float)value);
                    break;
                case VFXValueType.Float2:
                    value = $"({FormatFloat(((Vector2)value).x)}, {FormatFloat(((Vector2)value).y)})";
                    break;
                case VFXValueType.Float3:
                    value = $"({FormatFloat(((Vector3)value).x)}, {FormatFloat(((Vector3)value).y)}, {FormatFloat(((Vector3)value).z)})";
                    break;
                case VFXValueType.Float4:
                    value = $"({FormatFloat(((Vector4)value).x)}, {FormatFloat(((Vector4)value).y)}, {FormatFloat(((Vector4)value).z)}, {FormatFloat(((Vector4)value).w)})";
                    break;
                case VFXValueType.Matrix4x4:
                {
                    var matrix = ((Matrix4x4)value).transpose;
                    value = "(";
                    for (int i = 0; i < 16; ++i)
                        value += string.Format(CultureInfo.InvariantCulture, i == 15 ? "{0}" : "{0},", FormatFloat(matrix[i]));
                    value += ")";
                }
                break;
            }
            return string.Format(CultureInfo.InvariantCulture, format, VFXExpression.TypeToCode(type), value);
        }

        private static string FormatFloat(float f)
        {
            if (float.IsInfinity(f))
                return f > 0.0f ? "VFX_INFINITY" : "-VFX_INFINITY";
            else if (float.IsNaN(f))
                return "VFX_NAN";
            else
                return f.ToString("G9", CultureInfo.InvariantCulture);
        }

        public static string GetMultilineWithPrefix(string str, string linePrefix)
        {
            if (linePrefix.Length == 0)
                return str;

            if (str.Length == 0)
                return linePrefix;

            string[] delim = { System.Environment.NewLine, "\n" };
            var lines = str.Split(delim, System.StringSplitOptions.None);
            var dst = new StringBuilder(linePrefix.Length * lines.Length + str.Length);

            foreach (var line in lines)
            {
                dst.Append(linePrefix);
                dst.Append(line);
                dst.Append('\n');
            }

            return dst.ToString(0, dst.Length - 1); // Remove the last line terminator
        }

        public void WriteFormat(string str, object arg0) { m_Builder.AppendFormat(str, arg0); }
        public void WriteFormat(string str, object arg0, object arg1) { m_Builder.AppendFormat(str, arg0, arg1); }
        public void WriteFormat(string str, object arg0, object arg1, object arg2) { m_Builder.AppendFormat(str, arg0, arg1, arg2); }

        public void WriteLineFormat(string str, object arg0) { WriteFormat(str, arg0); WriteLine(); }
        public void WriteLineFormat(string str, object arg0, object arg1) { WriteFormat(str, arg0, arg1); WriteLine(); }
        public void WriteLineFormat(string str, object arg0, object arg1, object arg2) { WriteFormat(str, arg0, arg1, arg2); WriteLine(); }

        // Generic builder method
        public void Write<T>(T t)
        {
            m_Builder.Append(t);
        }

        // Optimize version to append substring and avoid useless allocation
        public void Write(String s, int start, int length)
        {
            m_Builder.Append(s, start, length);
        }

        public void WriteLine<T>(T t)
        {
            Write(t);
            WriteLine();
        }

        public void WriteLine()
        {
            m_Builder.Append('\n');
            WriteIndent();
        }

        public void EnterScope()
        {
            WriteLine('{');
            Indent();
        }

        public void ExitScope()
        {
            Deindent();
            WriteLine('}');
        }

        public void ExitScopeStruct()
        {
            Deindent();
            WriteLine("};");
        }

        public void ReplaceMultilineWithIndent(string tag, string src)
        {
            var str = m_Builder.ToString();
            int startIndex = 0;
            while (true)
            {
                int index = str.IndexOf(tag, startIndex, StringComparison.Ordinal);
                if (index == -1)
                    break;

                var lastPrefixIndex = index;
                while (index > 0 && (str[index] == ' ' || str[index] == '\t'))
                    --index;

                var prefix = str.Substring(index, lastPrefixIndex - index);
                var formattedStr = GetMultilineWithPrefix(src, prefix).Substring(prefix.Length);
                m_Builder.Replace(tag, formattedStr, lastPrefixIndex, tag.Length);

                startIndex = index;
            }
        }

        public void WriteMultilineWithIndent<T>(T str)
        {
            if (m_Indent == 0)
                Write(str);
            else
            {
                var indentStr = new StringBuilder(m_Indent * kIndentStr.Length);
                for (int i = 0; i < m_Indent; ++i)
                    indentStr.Append(kIndentStr);
                WriteMultilineWithPrefix(str, indentStr.ToString());
            }
        }

        public void WriteMultilineWithPrefix<T>(T str, string linePrefix)
        {
            if (linePrefix.Length == 0)
                Write(str);
            else
            {
                var res = GetMultilineWithPrefix(str.ToString(), linePrefix);
                WriteLine(res.Substring(linePrefix.Length)); // Remove first line length;
            }
        }

        public override string ToString()
        {
            return m_Builder.ToString();
        }

        private static bool IsBufferBuiltinType(Type type)
        {
            return VFXExpression.IsUniform(VFXExpression.GetVFXValueTypeFromType(type));
        }

        static string GetStructureName(Type type)
        {
            if (IsBufferBuiltinType(type))
                return VFXExpression.TypeToCode(VFXExpression.GetVFXValueTypeFromType(type));
            else
                return type.Name;
        }

        static void GenerateStructureCode(Type type, VFXShaderWriter structureDeclaration, HashSet<Type> alreadyGeneratedStructure)
        {
            if (IsBufferBuiltinType(type))
                return; // No structure to generate, it is a builtin type

            if (alreadyGeneratedStructure.Contains(type))
                return;

            var structureName = GetStructureName(type);

            var prerequisite = new VFXShaderWriter();
            var currentStructure = new VFXShaderWriter();
            currentStructure.WriteLineFormat("struct {0}", structureName);
            currentStructure.WriteLine("{");
            currentStructure.Indent();
            foreach (var field in VFXLibrary.GetFieldFromType(type))
            {
                string typeName;
                if (field.valueType == VFXValueType.None)
                {
                    typeName = GetStructureName(field.type);
                    GenerateStructureCode(field.type, prerequisite, alreadyGeneratedStructure);
                }
                else
                    typeName = VFXExpression.TypeToCode(field.valueType);

                currentStructure.WriteLineFormat("{0} {1};", typeName, field.name);
            }

            currentStructure.Deindent();
            currentStructure.WriteLine("};");

            prerequisite.WriteLine(currentStructure.ToString());
            structureDeclaration.Write(prerequisite.ToString());
            alreadyGeneratedStructure.Add(type);
        }

        private void WriteBufferTypeDeclaration(Type type, HashSet<Type> alreadyGeneratedStructure)
        {
            GenerateStructureCode(type, this, alreadyGeneratedStructure);

            var structureName = GetStructureName(type);
            var expectedStride = Marshal.SizeOf(type);
            WriteLineFormat("{0} SampleStructuredBuffer(StructuredBuffer<{0}> buffer, uint index, uint actualStride, uint actualCount)", structureName);
            {
                WriteLine("{");
                Indent();
                WriteLineFormat("{0} read = ({0})0;", structureName);
                WriteLine("[branch]");
                WriteLineFormat("if (actualStride == (uint){0} && index < actualCount)", expectedStride);
                {
                    Indent();
                    WriteLine("read = buffer[index];");
                    Deindent();
                }
                WriteLineFormat("return read;", structureName);
                Deindent();
                WriteLine("}");
            }
        }

        public void WriteBufferTypeDeclaration(IEnumerable<BufferType> bufferTypeUsage)
        {
            var types = bufferTypeUsage.Select(usage =>
            {
                var type = usage.actualType;
                if (IsBufferBuiltinType(type))
                {
                    //Resolve type which are conflicting behind the same VFXValueType (Vector4 & Color for instance)
                    var valueType = VFXExpression.GetVFXValueTypeFromType(type);
                    type = VFXExpression.TypeToType(valueType);
                }
                return type;
            }).Distinct();

            var alreadyGeneratedStructure = new HashSet<Type>();
            foreach (var type in types)
            {
                if (type == typeof(void))
                    continue;
                     
                WriteBufferTypeDeclaration(type, alreadyGeneratedStructure);
            }
        }

        public void WriteBuffer(VFXUniformMapper mapper, ReadOnlyDictionary<VFXExpression, BufferType> usageBuffer)
        {
            foreach (var buffer in mapper.buffers)
            {
                var name = mapper.GetName(buffer);
                if (buffer.valueType == VFXValueType.Buffer && usageBuffer.TryGetValue(buffer, out var type))
                {
                    if (!type.valid)
                        throw new NullReferenceException("Unexpected null type in graphicsBuffer usage");

                    WriteLineFormat("{0} {1};", GetBufferDeclaration(type), name);
                }
                else
                {
                    WriteLineFormat("{0} {1};", VFXExpression.TypeToCode(buffer.valueType), name);
                }
            }
        }

        public void WriteTexture(VFXUniformMapper mapper, ReadOnlyDictionary<VFXExpression, BufferType> bufferTypeUsage, IEnumerable<string> skipNames = null)
        {
            foreach (var texture in mapper.textures)
            {
                var names = mapper.GetNames(texture);
                // TODO At the moment issue all names sharing the same texture as different texture slots. This is not optimized as it required more texture binding than necessary
                Debug.Assert(names.Distinct().Count() == names.Count);
                foreach (var name in names)
                {
                    if (skipNames != null && skipNames.Contains(name))
                        continue;

                    if (bufferTypeUsage.TryGetValue(texture, out var usage))
                    {
                        WriteLineFormat("{0} {1};", GetBufferDeclaration(usage), name);
                    }
                    else
                    {
                        WriteLineFormat("{0} {1};", VFXExpression.TypeToCode(texture.valueType), name);
                    }

                    if (VFXExpression.IsTexture(texture.valueType)) //Mesh doesn't require a sampler or texel helper
                    {
                        WriteLineFormat("SamplerState sampler{0};", name);
                        WriteLineFormat("float4 {0}_TexelSize;", name); // TODO This is not very good to add a uniform for each texture that is hardly ever used
                    }
                    WriteLine();
                }
            }
        }

        public void WriteEventBuffers(string baseName, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                var prefix = VFXCodeGeneratorHelper.GeneratePrefix((uint)i);
                WriteLineFormat("RWStructuredBuffer<uint> {0}_{1};", baseName, prefix);
            }
        }

        public bool WriteGraphValuesStruct(VFXUniformMapper contextUniformMapper)
        {
            bool needsGraphValueStruct = false;
            var contextUniforms = contextUniformMapper.uniforms;
            if (contextUniforms.Any())
            {
                needsGraphValueStruct = true;

                WriteLine("struct GraphValues");
                WriteLine("{");
                Indent();

                foreach (var value in contextUniforms)
                {
                    string name = contextUniformMapper.GetName(value);
                    string type = VFXExpression.TypeToCode(value.valueType);
                    WriteLineFormat("{0} {1};", type, name);
                }
                Deindent();
                WriteLine("};");
            }
            WriteLine("ByteAddressBuffer graphValuesBuffer;");
            WriteLine();
            return needsGraphValueStruct;
        }

        public void GenerateLoadContextData(VFXDataParticle.GraphValuesLayout graphValuesLayout)
        {
            uint structSize = graphValuesLayout.paddedSizeInBytes;
            WriteLine("struct ContextData");
            WriteLine("{");
            WriteLine("    uint maxParticleCount;");
            WriteLine("    uint systemSeed;");
            WriteLine("    uint initSpawnIndex;");
            WriteLine("};");

            WriteLine("ContextData contextData;");
            WriteLine($"uint4 rawContextData = graphValuesBuffer.Load4(instanceActiveIndex * {structSize});");
            WriteLine($"contextData.maxParticleCount = rawContextData.x;");
            WriteLine($"contextData.systemSeed = rawContextData.y;");
            WriteLine($"contextData.initSpawnIndex = rawContextData.z;");
        }

        public void GenerateFillGraphValuesStruct(VFXUniformMapper contextUniformMapper, VFXDataParticle.GraphValuesLayout graphValuesLayout)
        {
            uint structSize = graphValuesLayout.paddedSizeInBytes;
            var nameToOffset = graphValuesLayout.nameToOffset;
            var contextUniforms = contextUniformMapper.uniforms;
            if (contextUniforms.Any())
            {
                contextUniforms = contextUniforms.OrderBy(o => nameToOffset[contextUniformMapper.GetName(o)]);
                WriteLine("GraphValues graphValues;");
                WriteLine();
                foreach (var value in contextUniforms)
                {
                    string name = contextUniformMapper.GetName(value);
                    int currentOffset = nameToOffset[name];

                    int typeSize = VFXExpression.TypeToSize(value.valueType);
                    string loadType = typeSize == 1 ? "" : typeSize.ToString();
                    if (value.valueType != VFXValueType.Matrix4x4)
                    {
                        string loadInstruction =
                            $"graphValuesBuffer.Load{loadType}(instanceActiveIndex * {structSize}  + {currentOffset})";

                        switch (value.valueType)
                        {
                            case VFXValueType.Float:
                            case VFXValueType.Float2:
                            case VFXValueType.Float3:
                            case VFXValueType.Float4:
                                loadInstruction = $"asfloat({loadInstruction})";
                                break;
                            case VFXValueType.Int32:
                                loadInstruction = $"asint({loadInstruction})";
                                break;
                            case VFXValueType.Boolean:
                                loadInstruction = $"(bool){loadInstruction}";
                                break;
                        }

                        WriteLineFormat("graphValues.{0} = {1};", name, loadInstruction);
                    }
                    else
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            string loadInstruction =
                                $"asfloat(graphValuesBuffer.Load4(instanceActiveIndex * {structSize}  + {currentOffset + 16 * i}))";
                            string columnGetter = String.Format("._m0{0}_m1{0}_m2{0}_m3{0}",i);
                            WriteLineFormat("graphValues.{0}{1} = {2};", name,columnGetter, loadInstruction);
                        }
                    }

                }
            }
        }

        public void WriteAttributeStruct(IEnumerable<VFXAttribute> attributes, string name)
        {
            WriteLineFormat("struct {0}", name);
            WriteLine("{");
            Indent();

            foreach (var attribute in attributes)
                WriteLineFormat("{0} {1};", VFXExpression.TypeToCode(attribute.type), attribute.name);

            Deindent();
            WriteLine("};");
        }

        private string AggregateParameters(List<string> parameters)
        {
            return parameters.Count == 0 ? "" : parameters.Aggregate((a, b) => a + ", " + b);
        }

        private static string GetBufferDeclaration(BufferType bufferType)
        {
            if (string.IsNullOrEmpty(bufferType.verbatimType))
                return bufferType.container.ToString();

            var verbatimType = IsBufferBuiltinType(bufferType.actualType)
                ? VFXExpression.TypeToCode(VFXExpression.GetVFXValueTypeFromType(bufferType.actualType))
                : bufferType.verbatimType;

            return $"{bufferType.container}<{verbatimType}>";
        }

        private static string GetFunctionParameterType(VFXExpression exp, ReadOnlyDictionary<VFXExpression, BufferType> usages)
        {
            if (usages.TryGetValue(exp, out var usage))
                return GetBufferDeclaration(usage);

            switch (exp.valueType)
            {
                case VFXValueType.Texture2D: return "VFXSampler2D";
                case VFXValueType.Texture2DArray: return "VFXSampler2DArray";
                case VFXValueType.Texture3D: return "VFXSampler3D";
                case VFXValueType.TextureCube: return "VFXSamplerCube";
                case VFXValueType.TextureCubeArray: return "VFXSamplerCubeArray";
                case VFXValueType.CameraBuffer: return "VFXSamplerCameraBuffer";
                case VFXValueType.Buffer:
                    throw new KeyNotFoundException("Cannot find appropriate usage for " + exp);
                default:
                    return VFXExpression.TypeToCode(exp.valueType);
            }
        }

        private static string GetFunctionParameterName(VFXExpression expression, Dictionary<VFXExpression, string> names, ReadOnlyDictionary<VFXExpression, BufferType> textureBufferUsages)
        {
            var expressionName = names[expression];
            switch (expression.valueType)
            {
                case VFXValueType.Texture2D:
                case VFXValueType.Texture2DArray:
                case VFXValueType.Texture3D:
                case VFXValueType.TextureCube:
                case VFXValueType.TextureCubeArray:
                    if (textureBufferUsages.TryGetValue(expression, out var textureUsage))
                        return expressionName;
                    return string.Format("GetVFXSampler({0}, {1})", expressionName, ("sampler" + expressionName));
                case VFXValueType.CameraBuffer: return string.Format("GetVFXSampler({0}, {1})", expressionName, ("sampler" + expressionName));

                default:
                    return expressionName;
            }
        }

        private static string GetInputModifier(VFXAttributeMode mode)
        {
            if ((mode & VFXAttributeMode.Write) != 0)
                return "inout ";

            return string.Empty;
        }

        public struct FunctionParameter
        {
            public string name;
            public VFXExpression expression;
            public VFXAttributeMode mode;
        }

        public void WriteBlockFunction(VFXTaskCompiledData taskData, string functionName, string source, IEnumerable<FunctionParameter> parameters, string commentMethod)
        {
            var parametersCode = new List<string>();
            foreach (var parameter in parameters)
            {
                var inputModifier = GetInputModifier(parameter.mode);
                var parameterType = GetFunctionParameterType(parameter.expression, taskData.bufferTypeUsage);
                parametersCode.Add(string.Format("{0}{1} {2}", inputModifier, parameterType, parameter.name));
            }

            WriteFormat("void {0}({1})", functionName, AggregateParameters(parametersCode));
            if (!string.IsNullOrEmpty(commentMethod))
            {
                WriteFormat(" /*{0}*/", commentMethod);
            }
            WriteLine();
            EnterScope();
            if (source != null)
                WriteMultilineWithIndent(source);
            ExitScope();
        }

        public void WriteCallFunction(string functionName, IEnumerable<FunctionParameter> parameters, VFXExpressionMapper mapper, Dictionary<VFXExpression, string> variableNames, ReadOnlyDictionary<VFXExpression, BufferType> textureBufferUsages)
        {
            var parametersCode = new List<string>();
            foreach (var parameter in parameters)
            {
                var inputModifier = GetInputModifier(parameter.mode);
                parametersCode.Add(string.Format("{1}{0}", GetFunctionParameterName(parameter.expression, variableNames, textureBufferUsages), string.IsNullOrEmpty(inputModifier) ? string.Empty : string.Format(" /*{0}*/", inputModifier)));
            }

            WriteLineFormat("{0}({1});", functionName, AggregateParameters(parametersCode));
        }

        public void WriteAssignement(VFXValueType type, string variableName, string value)
        {
            var format = value == "0" ? "{1} = ({0}){2};" : "{1} = {2};";
            WriteFormat(format, VFXExpression.TypeToCode(type), variableName, value);
        }

        public void WriteVariable(VFXValueType type, string variableName, string value)
        {
            if (!VFXExpression.IsTypeValidOnGPU(type))
                throw new ArgumentException(string.Format("Invalid GPU Type: {0}", type));

            WriteFormat("{0} ", VFXExpression.TypeToCode(type));
            WriteAssignement(type, variableName, value);
        }

        public void WriteDeclaration(VFXValueType type, string variableName)
        {
            if (!VFXExpression.IsTypeValidOnGPU(type))
                throw new ArgumentException(string.Format("Invalid GPU Type: {0}", type));

            WriteFormat("{0} {1};\n", VFXExpression.TypeToCode(type), variableName);
        }

        public void WriteDeclaration(VFXValueType type, string variableName, string semantic)
        {
            if (!VFXExpression.IsTypeValidOnGPU(type))
                throw new ArgumentException(string.Format("Invalid GPU Type: {0}", type));

            WriteFormat("VFX_OPTIONAL_INTERPOLATION {0} {1} : {2};\n", VFXExpression.TypeToCode(type), variableName, semantic);
        }

        public void WriteVariable(VFXExpression exp, Dictionary<VFXExpression, string> variableNames)
        {
            if (!variableNames.ContainsKey(exp))
            {
                string entry;
                if (exp.Is(VFXExpression.Flags.Constant))
                    entry = exp.GetCodeString(null); // Patch constant directly
                else
                {
                    foreach (var parent in exp.parents)
                        WriteVariable(parent, variableNames);

                    // Generate a new variable name
                    entry = "tmp_" + VFXCodeGeneratorHelper.GeneratePrefix((uint)variableNames.Count());
                    string value = exp.GetCodeString(exp.parents.Select(p => variableNames[p]).ToArray());

                    WriteVariable(exp.valueType, entry, value);
                    WriteLine();
                }

                variableNames[exp] = entry;
            }
        }

        public StringBuilder builder { get { return m_Builder; } }

        // Private stuff
        private void Indent()
        {
            ++m_Indent;
            Write(kIndentStr);
        }

        private void Deindent()
        {
            if (m_Indent == 0)
                throw new InvalidOperationException("Cannot de-indent as current indentation is 0");

            --m_Indent;
            m_Builder.Remove(m_Builder.Length - kIndentStr.Length, kIndentStr.Length); // remove last indent
        }

        private void WriteIndent()
        {
            for (int i = 0; i < m_Indent; ++i)
                m_Builder.Append(kIndentStr);
        }

        private StringBuilder m_Builder = new StringBuilder();
        private int m_Indent = 0;
        private const string kIndentStr = "    ";
    }
}
