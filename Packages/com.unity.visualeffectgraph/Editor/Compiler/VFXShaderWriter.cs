using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEngine.VFX;
using System.Reflection;
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
                int index = str.IndexOf(tag, startIndex);
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

        private int WritePadding(int alignment, int offset, ref int index)
        {
            int padding = (alignment - (offset % alignment)) % alignment;
            if (padding != 0)
                WriteLineFormat("uint{0} PADDING_{1};", padding == 1 ? "" : padding.ToString(), index++);
            return padding;
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

        public void WriteBufferTypeDeclaration(IEnumerable<Type> types)
        {
            var alreadyGeneratedStructure = new HashSet<Type>();
            foreach (var type in types)
                WriteBufferTypeDeclaration(type, alreadyGeneratedStructure);
        }

        public void WriteBuffer(VFXUniformMapper mapper, ReadOnlyDictionary<VFXExpression, Type> usageGraphicsBuffer)
        {
            foreach (var buffer in mapper.buffers)
            {
                var name = mapper.GetName(buffer);

                if (buffer.valueType == VFXValueType.Buffer && usageGraphicsBuffer.TryGetValue(buffer, out var type))
                {
                    if (type == null)
                        throw new NullReferenceException("Unexpected null type in graphicsBuffer usage");

                    var structureName = GetStructureName(type);
                    WriteLineFormat("StructuredBuffer<{0}> {1};", structureName, name);
                }
                else
                {
                    WriteLineFormat("{0} {1};", VFXExpression.TypeToCode(buffer.valueType), name);
                }
            }
        }

        public void WriteTexture(VFXUniformMapper mapper, IEnumerable<string> skipNames = null)
        {
            foreach (var texture in mapper.textures)
            {
                var names = mapper.GetNames(texture);
                // TODO At the moment issue all names sharing the same texture as different texture slots. This is not optimized as it required more texture binding than necessary
                for (int i = 0; i < names.Count; ++i)
                {
                    if (skipNames != null && skipNames.Contains(names[i]))
                        continue;

                    WriteLineFormat("{0} {1};", VFXExpression.TypeToCode(texture.valueType), names[i]);
                    if (VFXExpression.IsTexture(texture.valueType)) //Mesh doesn't require a sampler or texel helper
                    {
                        WriteLineFormat("SamplerState sampler{0};", names[i]);
                        WriteLineFormat("float4 {0}_TexelSize;", names[i]); // TODO This is not very good to add a uniform for each texture that is hardly ever used
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
                WriteLineFormat("AppendStructuredBuffer<uint> {0}_{1};", baseName, prefix);
            }
        }

        public void WriteCBuffer(VFXUniformMapper mapper, string bufferName)
        {
            var uniformValues = mapper.uniforms
                .Where(e => !e.IsAny(VFXExpression.Flags.Constant | VFXExpression.Flags.InvalidOnCPU)) // Filter out constant expressions
                .OrderByDescending(e => VFXValue.TypeToSize(e.valueType));

            var uniformBlocks = new List<List<VFXExpression>>();
            foreach (var value in uniformValues)
            {
                var block = uniformBlocks.FirstOrDefault(b => b.Sum(e => VFXValue.TypeToSize(e.valueType)) + VFXValue.TypeToSize(value.valueType) <= 4);
                if (block != null)
                    block.Add(value);
                else
                    uniformBlocks.Add(new List<VFXExpression>() { value });
            }

            if (uniformBlocks.Count > 0)
            {
                WriteLineFormat("CBUFFER_START({0})", bufferName);
                Indent();

                int paddingIndex = 0;
                foreach (var block in uniformBlocks)
                {
                    int currentSize = 0;
                    foreach (var value in block)
                    {
                        string type = VFXExpression.TypeToUniformCode(value.valueType);
                        string name = mapper.GetName(value);
                        if (name.StartsWith("unity_")) //Reserved unity variable name (could be filled manually see : VFXCameraUpdate)
                            continue;

                        currentSize += VFXExpression.TypeToSize(value.valueType);

                        WriteLineFormat("{0} {1};", type, name);
                    }

                    WritePadding(4, currentSize, ref paddingIndex);
                }

                Deindent();
                WriteLine("CBUFFER_END");
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

        private static string GetFunctionParameterType(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.Texture2D: return "VFXSampler2D";
                case VFXValueType.Texture2DArray: return "VFXSampler2DArray";
                case VFXValueType.Texture3D: return "VFXSampler3D";
                case VFXValueType.TextureCube: return "VFXSamplerCube";
                case VFXValueType.TextureCubeArray: return "VFXSamplerCubeArray";
                case VFXValueType.CameraBuffer: return "VFXSamplerCameraBuffer";
                default:
                    return VFXExpression.TypeToCode(type);
            }
        }

        private static string GetFunctionParameterName(VFXExpression expression, Dictionary<VFXExpression, string> names)
        {
            var expressionName = names[expression];
            switch (expression.valueType)
            {
                case VFXValueType.Texture2D:
                case VFXValueType.Texture2DArray:
                case VFXValueType.Texture3D:
                case VFXValueType.TextureCube:
                case VFXValueType.TextureCubeArray: return string.Format("GetVFXSampler({0}, {1})", expressionName, ("sampler" + expressionName));
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

        public void WriteBlockFunction(VFXExpressionMapper mapper, string functionName, string source, IEnumerable<FunctionParameter> parameters, string commentMethod)
        {
            var parametersCode = new List<string>();
            foreach (var parameter in parameters)
            {
                var inputModifier = GetInputModifier(parameter.mode);
                var parameterType = GetFunctionParameterType(parameter.expression.valueType);
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

        public void WriteCallFunction(string functionName, IEnumerable<FunctionParameter> parameters, VFXExpressionMapper mapper, Dictionary<VFXExpression, string> variableNames)
        {
            var parametersCode = new List<string>();
            foreach (var parameter in parameters)
            {
                var inputModifier = GetInputModifier(parameter.mode);
                parametersCode.Add(string.Format("{1}{0}", GetFunctionParameterName(parameter.expression, variableNames), string.IsNullOrEmpty(inputModifier) ? string.Empty : string.Format(" /*{0}*/", inputModifier)));
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
