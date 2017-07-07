using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.VFX
{
    static class VFXShaderWriter
    {
        private static int WritePadding(int alignment, int offset, ref int index, StringBuilder builder)
        {
            int padding = (alignment - (offset % alignment)) % alignment;
            if (padding != 0)
                builder.AppendLine(string.Format("\tuint{0} PADDING_{1};", padding == 1 ? "" : padding.ToString(), index++));
            return padding;
        }

        public static string WriteCBuffer(VFXUniformMapper mapper)
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
                var builder = new StringBuilder();

                builder.AppendLine("CBUFFER_START(test)");
                builder.AppendLine("{");

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

                builder.AppendLine("}");
                return builder.ToString();
            }

            return string.Empty;
        }

        private static void WriteVariable(StringBuilder builder, VFXValueType type, string variableName, string value)
        {
            if (!VFXExpression.IsTypeValidOnGPU(type))
                throw new ArgumentException(string.Format("Invalid GPU Type: {0}", type));

            builder.Append(VFXExpression.TypeToCode(type));
            builder.Append(" ");
            builder.Append(variableName);
            builder.Append(" = ");
            builder.Append(value);
            builder.AppendLine(";");
        }

        private static void WriteVariable(StringBuilder builder, VFXExpression exp, Dictionary<VFXExpression, string> variableNames, VFXUniformMapper uniformMapper)
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
                    entry = "tmp" + variableNames.Count();
                    string value = exp.GetCodeString(exp.Parents.Select(p => variableNames[p]).ToArray());

                    WriteVariable(builder, exp.ValueType, entry, value);
                }

                variableNames[exp] = entry;
            }
        }

        public static string WriteParameter(VFXExpression exp, VFXUniformMapper uniformMapper)
        {
            var builder = new StringBuilder();
            var variableNames = new Dictionary<VFXExpression, string>();

            WriteVariable(builder, exp, variableNames, uniformMapper);
            WriteVariable(builder, exp.ValueType, "param", variableNames[exp]);

            return builder.ToString();
        }
    }
}
