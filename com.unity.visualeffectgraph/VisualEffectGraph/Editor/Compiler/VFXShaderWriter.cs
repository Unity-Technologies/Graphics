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

    static class VFXShaderWriter
    {
        private static int WritePadding(int alignment, int offset, ref int index, StringBuilder builder)
        {
            int padding = (alignment - (offset % alignment)) % alignment;
            if (padding != 0)
                builder.AppendLine(string.Format("\tuint{0} PADDING_{1};", padding == 1 ? "" : padding.ToString(), index++));
            return padding;
        }

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
            return string.Format(format, VFXExpression.TypeToCode(type), value.ToString());
        }

        public static void WriteCBuffer(VFXUniformMapper mapper, StringBuilder builder, string bufferName)
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
                builder.AppendFormat("CBUFFER_START({0})", bufferName);
                builder.AppendLine();

                int paddingIndex = 0;
                foreach (var block in uniformBlocks)
                {
                    int currentSize = 0;
                    foreach (var value in block)
                    {
                        string type = VFXExpression.TypeToCode(value.ValueType);
                        string name = mapper.GetName(value);
                        currentSize += VFXExpression.TypeToSize(value.ValueType);

                        builder.AppendLine(string.Format("\t{0} {1};", type, name));
                    }

                    WritePadding(4, currentSize, ref paddingIndex, builder);
                }

                builder.AppendLine("CBUFFER_END");
            }
        }

        private static string AggregateParameters(List<string> parameters)
        {
            return parameters.Count == 0 ? "" : parameters.Aggregate((a, b) => a + ", " + b);
        }

        public static void WriteBlockFunction(StringBuilder builder, VFXBlock block)
        {
            var parameters = new List<string>();
            foreach (var attribute in block.attributes)
            {
                parameters.Add(string.Format("{0}{1} {2}", (attribute.mode & VFXAttributeMode.Write) != 0 ? "inout " : "", VFXExpression.TypeToCode(attribute.attrib.type), attribute.attrib.name));
            }

            foreach (var parameter in block.parameters)
            {
                parameters.Add(string.Format("{0} {1}", VFXExpression.TypeToCode(parameter.exp.ValueType), parameter.name));
            }

            builder.AppendFormat("void {0}({1})", block.GetType().Name, AggregateParameters(parameters));
            builder.AppendLine();
            builder.AppendLine("{");
            if (block.source != null)
            {
                builder.AppendLine(block.source);
            }
            builder.AppendLine("}");
        }

        public static void WriteCallFunction(StringBuilder builder, VFXBlock block, VFXExpressionMapper mapper, Dictionary<VFXExpression, string> variableNames)
        {
            var parameters = new List<string>();
            foreach (var attribute in block.attributes)
            {
                parameters.Add(variableNames[new VFXAttributeExpression(attribute.attrib)]);
            }

            foreach (var parameter in block.parameters)
            {
                var expReduced = mapper.FromNameAndId(parameter.name, block.GetParent().GetIndex(block));
                if (!variableNames.ContainsKey(expReduced))
                {
                    throw new Exception(string.Format("Cannot find variable name for {0}", expReduced));
                }
                parameters.Add(variableNames[expReduced]);
            }

            builder.AppendFormat("{0}({1});", block.GetType().Name, AggregateParameters(parameters));
            builder.AppendLine();
        }

        public static void WriteAssignement(StringBuilder builder, VFXValueType type, string variableName, string value)
        {
            var format = value == "0" ? "{1} = ({0}){2};" : "{1} = {2};";
            builder.AppendFormat(format, VFXExpression.TypeToCode(type), variableName, value);
        }

        public static void WriteVariable(StringBuilder builder, VFXValueType type, string variableName, string value, string comment = null)
        {
            if (!VFXExpression.IsTypeValidOnGPU(type))
                throw new ArgumentException(string.Format("Invalid GPU Type: {0}", type));

            builder.AppendFormat("{0} ", VFXExpression.TypeToCode(type));
            WriteAssignement(builder, type, variableName, value);
            builder.AppendFormat(comment == null ? "" : "//" + comment);
            builder.AppendLine();
        }

        public static void WriteVariable(StringBuilder builder, VFXExpression exp, Dictionary<VFXExpression, string> variableNames, VFXUniformMapper uniformMapper)
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
                        WriteVariable(builder, parent, variableNames, uniformMapper);

                    // Generate a new variable name
                    entry = "tmp_" + VFXCodeGeneratorHelper.GeneratePrefix((uint)variableNames.Count());
                    string value = exp.GetCodeString(exp.Parents.Select(p => variableNames[p]).ToArray());

                    WriteVariable(builder, exp.ValueType, entry, value);
                }

                variableNames[exp] = entry;
            }
        }

        public static void WriteParameter(StringBuilder builder, VFXExpression exp, VFXUniformMapper uniformMapper, string paramName)
        {
            var variableNames = new Dictionary<VFXExpression, string>();
            WriteVariable(builder, exp, variableNames, uniformMapper);
            WriteVariable(builder, exp.ValueType, paramName, variableNames[exp]);
        }
    }
}
