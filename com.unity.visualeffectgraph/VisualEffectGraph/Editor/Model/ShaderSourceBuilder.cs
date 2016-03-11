using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental;

namespace UnityEditor.Experimental
{
    public static class ShaderSourceStringBuilderExtensions
    {
        public static void WriteAttributeBuffer(this StringBuilder builder, AttributeBuffer attributeBuffer)
        {
            builder.Append("struct ");
            builder.Append("Attribute");
            builder.Append(attributeBuffer.Index);
            builder.AppendLine();
            builder.AppendLine("{");
            for (int i = 0; i < attributeBuffer.Count; ++i)
            {
                builder.Append('\t');
                builder.WriteType(attributeBuffer[i].m_Param.m_Type);
                builder.Append(" ");
                builder.Append(attributeBuffer[i].m_Param.m_Name);
                builder.AppendLine(";");
            }

            if (attributeBuffer.GetSizeInBytes() == 3)
                builder.AppendLine("\tfloat _PADDING_;");

            builder.AppendLine("};");
            builder.AppendLine();
        }

        public static void WriteCBuffer(this StringBuilder builder, string cbufferName, HashSet<VFXParamValue> uniforms, Dictionary<VFXParamValue, string> uniformsToName)
        {
            if (uniforms.Count > 0)
            {
                builder.Append("CBUFFER_START(");
                builder.Append(cbufferName);
                builder.AppendLine(")");
                foreach (var uniform in uniforms)
                {
                    builder.Append('\t');
                    builder.WriteType(uniform.ValueType);
                    builder.Append(" ");
                    builder.Append(uniformsToName[uniform]);
                    builder.AppendLine(";");
                }
                builder.AppendLine("CBUFFER_END");
                builder.AppendLine();
            }
        }

        public static void WriteSamplers(this StringBuilder builder, HashSet<VFXParamValue> samplers, Dictionary<VFXParamValue, string> samplersToName)
        {
            foreach (var sampler in samplers)
            {
                if (sampler.ValueType == VFXParam.Type.kTypeTexture2D)
                    builder.Append("sampler2D ");
                else if (sampler.ValueType == VFXParam.Type.kTypeTexture3D)
                    builder.Append("sampler3D ");
                else
                    continue;

                builder.Append(samplersToName[sampler]);
                builder.AppendLine(";");
                builder.AppendLine();
            }
        }

        public static void WriteType(this StringBuilder builder, VFXParam.Type type)
        {
            // tmp transform texture to sampler TODO This must be handled directly in C++ conversion array
            if (type == VFXParam.Type.kTypeTexture2D)
                builder.Append("sampler2D");
            else if (type == VFXParam.Type.kTypeTexture3D)
                builder.Append("sampler3D");
            else
                builder.Append(VFXParam.GetNameFromType(type));
        }

        public static void WriteFunction(this StringBuilder builder, VFXBlockModel block, Dictionary<Hash128, string> functions)
        {
            if (!functions.ContainsKey(block.Desc.m_Hash)) // if not already defined
            {
                // generate function name
                string name = new string((from c in block.Desc.m_Name where char.IsLetterOrDigit(c) select c).ToArray());
                functions[block.Desc.m_Hash] = name;

                string source = block.Desc.m_Source;

                // function signature
                builder.Append("void ");
                builder.Append(name);
                builder.Append("(");

                char separator = ' ';
                foreach (var arg in block.Desc.m_Attribs)
                {
                    builder.Append(separator);
                    separator = ',';

                    if (arg.m_Writable)
                        builder.Append("inout ");
                    builder.WriteType(arg.m_Param.m_Type);
                    builder.Append(" ");
                    builder.Append(arg.m_Param.m_Name);
                }

                foreach (var arg in block.Desc.m_Params)
                {
                    builder.Append(separator);
                    separator = ',';

                    builder.WriteType(arg.m_Type);
                    builder.Append(" ");
                    builder.Append(arg.m_Name);
                }

                if ((block.Desc.m_Flags & (int)VFXBlock.Flag.kHasRand) != 0)
                {
                    builder.Append(separator);
                    separator = ',';
                    builder.Append("inout uint seed");
                    source = source.Replace("RAND", "rand(seed)"); // TODO Not needed anymore (done in the importer)
                }

                if ((block.Desc.m_Flags & (int)VFXBlock.Flag.kHasKill) != 0)
                {
                    builder.Append(separator);
                    separator = ',';
                    builder.Append("inout bool kill");
                    source = source.Replace("KILL", "kill = true"); // TODO Not needed anymore (done in the importer)
                }

                builder.AppendLine(")");

                // function body
                builder.AppendLine("{");

                // Add tab for formatting
                //source = source.Replace("\n", "\n\t");
                builder.Append(source);
                builder.AppendLine();

                builder.AppendLine("}");
                builder.AppendLine();
            }
        }

        public static void WriteFunctionCall(
            this StringBuilder builder, 
            VFXBlockModel block,
            Dictionary<Hash128, string> functions,
            ShaderMetaData data)
        {
            Dictionary<VFXParamValue, string> paramToName = data.paramToName;
            Dictionary<VFXAttrib, AttributeBuffer> attribToBuffer = data.attribToBuffer;

            builder.Append("\t\t");
            builder.Append(functions[block.Desc.m_Hash]);
            builder.Append("(");

            char separator = ' ';
            foreach (var arg in block.Desc.m_Attribs)
            {
                builder.Append(separator);
                separator = ',';

                int index = attribToBuffer[arg].Index;
                builder.Append("attrib");
                builder.Append(index);
                builder.Append(".");
                builder.Append(arg.m_Param.m_Name);
            }

            for (int i = 0; i < block.Desc.m_Params.Length; ++i)
            {
                builder.Append(separator);
                separator = ',';
                builder.Append(paramToName[block.GetParamValue(i)]);
            }

            if ((block.Desc.m_Flags & (int)VFXBlock.Flag.kHasRand) != 0)
            {
                builder.Append(separator);
                separator = ',';

                builder.WriteAttrib(CommonAttrib.Seed, data);
            }

            if ((block.Desc.m_Flags & (int)VFXBlock.Flag.kHasKill) != 0)
            {
                builder.Append(separator);
                separator = ',';
                builder.Append("kill");
            }

            builder.AppendLine(");");
        }

        public static void WriteAddPhaseShift(this StringBuilder builder, ShaderMetaData data)
        {
            builder.WritePhaseShift('+', data);
        }

        public static void WriteRemovePhaseShift(this StringBuilder builder, ShaderMetaData data)
        {
            builder.WritePhaseShift('-', data);
        }

        private static void WritePhaseShift(this StringBuilder builder, char op, ShaderMetaData data)
        {
            builder.Append("\t\t");
            builder.WriteAttrib(CommonAttrib.Position, data);
            builder.Append(" ");
            builder.Append(op);
            builder.Append("= (");
            builder.WriteAttrib(CommonAttrib.Phase, data);
            builder.Append(" * deltaTime) * ");
            builder.WriteAttrib(CommonAttrib.Velocity, data);
            builder.AppendLine(";");
        }

        public static void WriteAttrib(this StringBuilder builder, VFXAttrib attrib, ShaderMetaData data)
        {
            int attribIndex = data.attribToBuffer[attrib].Index;
            builder.Append("attrib");
            builder.Append(attribIndex);
            builder.Append(".");
            builder.Append(attrib.m_Param.m_Name);
        }

        public static void WriteKernelHeader(this StringBuilder builder, string name)
        {
            builder.AppendLine("[numthreads(NB_THREADS_PER_GROUP,1,1)]");
            builder.Append("void ");
            builder.Append(name);
            builder.AppendLine("(uint3 id : SV_DispatchThreadID)");
            builder.AppendLine("{");
        }
    }
}

