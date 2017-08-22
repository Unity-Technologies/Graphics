using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    static class VFXCodeGeneratorHelper
    {
        public static string GeneratePrefix(uint index)
        {
            var alpha = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
            string prefix = "";
            index = index + 1;
            while (index != 0u)
            {
                prefix = alpha[index % alpha.Length] + prefix;
                index /= (uint)alpha.Length;
            }
            return prefix;
        }
    }

    class VFXShaderWriter
    {
        public void WriteFormat(string str, object arg0)                                { m_Builder.AppendFormat(str, arg0); }
        public void WriteFormat(string str, object arg0, object arg1)                   { m_Builder.AppendFormat(str, arg0, arg1); }
        public void WriteFormat(string str, object arg0, object arg1, object arg2)      { m_Builder.AppendFormat(str, arg0, arg1, arg2); }

        public void WriteLineFormat(string str, object arg0)                            { WriteFormat(str, arg0); WriteLine(); }
        public void WriteLineFormat(string str, object arg0, object arg1)               { WriteFormat(str, arg0, arg1); WriteLine(); }
        public void WriteLineFormat(string str, object arg0, object arg1, object arg2)  { WriteFormat(str, arg0, arg1, arg2); WriteLine(); }

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
            m_Builder.AppendLine();
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

        public void WriteWithIndent<T>(T str)
        {
            if (m_Indent == 0)
                Write(str);
            else
            {
                var indentStr = new StringBuilder(m_Indent);
                indentStr.Append('\t', m_Indent);
                WriteMultilineWithPrefix(str, indentStr.ToString());
            }
        }

        public void WriteMultilineWithPrefix<T>(T str, string linePrefix)
        {
            if (linePrefix.Length == 0)
                Write(str);
            else
            {
                var builder = new StringBuilder(str.ToString());
                WriteMultilineWithPrefix(builder, linePrefix);
                Write(builder.ToString());
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

        // TODO Change that
        public static string WriteConstructValue(VFXValueType type, object value)
        {
            var format = "";
            switch (type)
            {
                case VFXValueType.kBool:
                case VFXValueType.kInt:
                case VFXValueType.kUint:
                case VFXValueType.kFloat:
                    format = "({0}){1}";
                    break;
                case VFXValueType.kFloat2:
                case VFXValueType.kFloat3:
                case VFXValueType.kFloat4:
                    format = "{0}{1}";
                    break;
                default: throw new Exception("WriteConstructValue missing type: " + type);
            }
            return string.Format(format, VFXExpression.TypeToCode(type), value.ToString().ToLower());
        }

        public void WriteTexture(VFXUniformMapper mapper)
        {
            foreach (var texture in mapper.textures)
                WriteLineFormat("{0} {1};", VFXExpression.TypeToCode(texture.ValueType), mapper.GetName(texture));
        }

        public void WriteCBuffer(VFXUniformMapper mapper, string bufferName)
        {
            var uniformValues = mapper.uniforms
                .Where(e => !e.IsAny(VFXExpression.Flags.Constant | VFXExpression.Flags.InvalidOnCPU)) // Filter out constant expressions
                .OrderByDescending(e => VFXValue.TypeToSize(e.ValueType));

            var uniformBlocks = new List<List<VFXExpression>>();
            foreach (var value in uniformValues)
            {
                var block = uniformBlocks.FirstOrDefault(b => b.Sum(e => VFXValue.TypeToSize(e.ValueType)) + VFXValue.TypeToSize(value.ValueType) <= 4);
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
                        string type = VFXExpression.TypeToCode(value.ValueType);
                        string name = mapper.GetName(value);
                        currentSize += VFXExpression.TypeToSize(value.ValueType);

                        WriteLineFormat("{0} {1};", type, name);
                    }

                    WritePadding(4, currentSize, ref paddingIndex);
                }

                Deindent();
                WriteLine("CBUFFER_END");
            }
        }

        private string AggregateParameters(List<string> parameters)
        {
            return parameters.Count == 0 ? "" : parameters.Aggregate((a, b) => a + ", " + b);
        }

        public void WriteBlockFunction(VFXExpressionMapper mapper, string functionName, string source, List<VFXExpression> expressions, List<string> parameterNames, List<VFXAttributeMode> modes)
        {
            var parameters = new List<string>();
            for (int i = 0; i < parameterNames.Count; ++i)
            {
                var parameter = parameterNames[i];
                var mode = modes[i];
                var expression = expressions[i];
                parameters.Add(string.Format("{0}{1} {2}", (mode & VFXAttributeMode.Write) != 0 ? "inout " : "", VFXExpression.TypeToCode(expression.ValueType), parameter));
            }

            WriteLineFormat("void {0}({1})", functionName, AggregateParameters(parameters));
            EnterScope();
            if (source != null)
                WriteLine(source);
            ExitScope();
        }

        public void WriteCallFunction(string functionName, List<VFXExpression> expressions, List<string> parameterNames, List<VFXAttributeMode> modes, VFXExpressionMapper mapper, Dictionary<VFXExpression, string> variableNames)
        {
            var parameters = new List<string>();
            for (int i = 0; i < parameterNames.Count; ++i)
            {
                var parameter = parameterNames[i];
                var mode = modes[i];
                var expression = expressions[i];
                parameters.Add(string.Format("{0} /*{1}{2}*/", variableNames[expression], (mode & VFXAttributeMode.Write) != 0 ? "inout " : "", parameter));
            }

            WriteLineFormat("{0}({1});", functionName, AggregateParameters(parameters));
        }

        public void WriteAssignement(VFXValueType type, string variableName, string value)
        {
            var format = value == "0" ? "{1} = ({0}){2};" : "{1} = {2};";
            WriteFormat(format, VFXExpression.TypeToCode(type), variableName, value);
        }

        public void WriteVariable(VFXValueType type, string variableName, string value, string comment = null)
        {
            if (!VFXExpression.IsTypeValidOnGPU(type))
                throw new ArgumentException(string.Format("Invalid GPU Type: {0}", type));

            WriteFormat("{0} ", VFXExpression.TypeToCode(type));
            WriteAssignement(type, variableName, value);
            WriteLine(comment == null ? "" : "//" + comment);
        }

        public void WriteVariable(VFXExpression exp, Dictionary<VFXExpression, string> variableNames, VFXUniformMapper uniformMapper)
        {
            if (!variableNames.ContainsKey(exp))
            {
                string entry;
                if (exp.Is(VFXExpression.Flags.Constant))
                    entry = exp.GetCodeString(null); // Patch constant directly
                else if (uniformMapper.Contains(exp))
                    entry = uniformMapper.GetName(exp);
                else
                {
                    foreach (var parent in exp.Parents)
                        WriteVariable(parent, variableNames, uniformMapper);

                    // Generate a new variable name
                    entry = "tmp_" + VFXCodeGeneratorHelper.GeneratePrefix((uint)variableNames.Count());
                    string value = exp.GetCodeString(exp.Parents.Select(p => variableNames[p]).ToArray());

                    WriteVariable(exp.ValueType, entry, value);
                }

                variableNames[exp] = entry;
            }
        }

        public void WriteParameter(VFXExpression exp, VFXUniformMapper uniformMapper, string paramName)
        {
            var variableNames = new Dictionary<VFXExpression, string>();
            WriteVariable(exp, variableNames, uniformMapper);
            WriteVariable(exp.ValueType, paramName, variableNames[exp]);
        }

        public StringBuilder Builder { get { return m_Builder; } }

        // Private stuff
        private void Indent()
        {
            ++m_Indent;
            Write('\t');
        }

        private void Deindent()
        {
            if (m_Indent == 0)
                throw new InvalidOperationException("Cannot de-indent as current indentation is 0");

            --m_Indent;
            m_Builder.Remove(m_Builder.Length - 1, 1); // remove last \t
        }

        private void WriteIndent()
        {
            for (int i = 0; i < m_Indent; ++i)
                m_Builder.Append('\t');
        }

        private static void WriteMultilineWithPrefix(StringBuilder builder, string linePrefix)
        {
            if (linePrefix.Length == 0)
                return;

            throw new NotImplementedException();
        }

        private StringBuilder m_Builder = new StringBuilder();
        private int m_Indent = 0;
    }
}
