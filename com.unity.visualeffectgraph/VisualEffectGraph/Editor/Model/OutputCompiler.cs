using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental;

namespace UnityEditor.Experimental
{
    abstract class OutputCompiler
    {
        // Flag attributes as being needed or not in the output
        public abstract bool MarkAttributes(Dictionary<VFXAttrib, int> attribs);
        public abstract string GenerateSource(ShaderMetaData data);

        protected static bool UpdateFlag(Dictionary<VFXAttrib, int> attribs, VFXAttrib attrib)
        {
            int attribFlag;
            if (attribs.TryGetValue(attrib, out attribFlag))
            {
                attribFlag |= 0x10; // Readable in output
                attribs[attrib] = attribFlag;
                return true;
            }

            return false;
        }

        protected static void WriteCommonSourceHead(StringBuilder builder,ShaderMetaData data)
        {
            builder.AppendLine("Shader \"Custom/PointShader\"");
            builder.AppendLine("{");
            builder.AppendLine("\tSubShader");
            builder.AppendLine("\t{");

            builder.AppendLine("\t\tTags { \"Queue\"=\"Transparent\" \"IgnoreProjector\"=\"True\" \"RenderType\"=\"Transparent\" }");
            builder.AppendLine("\t\tPass");
            builder.AppendLine("\t\t{");
            builder.AppendLine("\t\tBlend SrcAlpha One");
            builder.AppendLine("\t\tZTest LEqual");
            builder.AppendLine("\t\tZWrite Off");
            builder.AppendLine("\t\t\tCGPROGRAM");
            builder.AppendLine("\t\t\t#pragma target 5.0");
            builder.AppendLine();
            builder.AppendLine("\t\t\t#pragma vertex vert");
            builder.AppendLine("\t\t\t#pragma fragment frag");
            builder.AppendLine();
            builder.AppendLine("\t\t\t#include \"UnityCG.cginc\"");
            builder.AppendLine();

            foreach (AttributeBuffer buffer in data.attributeBuffers)
                if (buffer.Used(VFXContextDesc.Type.kTypeOutput))
                    builder.WriteAttributeBuffer(buffer);

            foreach (AttributeBuffer buffer in data.attributeBuffers)
                if (buffer.Used(VFXContextDesc.Type.kTypeOutput))
                {
                    builder.Append("\t\t\tStructuredBuffer<Attribute");
                    builder.Append(buffer.Index);
                    builder.Append("> attribBuffer");
                    builder.Append(buffer.Index);
                    builder.AppendLine(";");
                }

            if (data.hasKill)
                builder.AppendLine("\t\t\tStructuredBuffer<int> flags;");
        }
    }

    class PointOutputCompiler : OutputCompiler
    {
        public override bool MarkAttributes(Dictionary<VFXAttrib, int> attribs)
        {
            if (!UpdateFlag(attribs,CommonAttrib.Position))
                return false;

            hasColor = UpdateFlag(attribs,CommonAttrib.Color);
            hasAlpha = UpdateFlag(attribs,CommonAttrib.Alpha);
            return true;
        }

        public override string GenerateSource(ShaderMetaData data)
        {
            StringBuilder builder = new StringBuilder();
            WriteCommonSourceHead(builder, data);

            builder.AppendLine();
            builder.AppendLine("\t\t\tstruct ps_input {");
            builder.AppendLine("\t\t\t\tfloat4 pos : SV_POSITION;");

            if (hasColor || hasAlpha)
                builder.AppendLine("\t\t\t\tnointerpolation float4 col : COLOR0;");

            builder.AppendLine("\t\t\t};");
            builder.AppendLine();
            builder.AppendLine("\t\t\tps_input vert (uint id : SV_VertexID)");
            builder.AppendLine("\t\t\t{");
            builder.AppendLine("\t\t\t\tps_input o;");

            if (data.hasKill)
            {
                builder.AppendLine("\t\t\t\tif (flags[id] == 1)");
                builder.AppendLine("\t\t\t\t{");
            }

            foreach (var buffer in data.attributeBuffers)
                if (buffer.Used(VFXContextDesc.Type.kTypeOutput))
                {
                    builder.Append("\t\t\t\t\tAttribute");
                    builder.Append(buffer.Index);
                    builder.Append(" attrib");
                    builder.Append(buffer.Index);
                    builder.Append(" = attribBuffer");
                    builder.Append(buffer.Index);
                    builder.AppendLine("[id];");
                }
            builder.AppendLine();

            builder.Append("\t\t\t\t\tfloat3 worldPos = ");
            builder.WriteAttrib(CommonAttrib.Position,data);
            builder.AppendLine(";");
            builder.AppendLine("\t\t\t\t\to.pos = mul (UNITY_MATRIX_MVP, float4(worldPos,1.0f));");

            if (hasColor || hasAlpha)
            {
                builder.Append("\t\t\t\t\to.col = float4(");

                if (hasColor)
                {
                    builder.WriteAttrib(CommonAttrib.Color, data);
                    builder.Append(".xyz,");
                }
                else
                    builder.Append("1.0,1.0,1.0,");

                if (hasAlpha)
                {
                    builder.WriteAttrib(CommonAttrib.Alpha, data);
                    builder.AppendLine(");");
                }
                else
                    builder.AppendLine("0.5);");
            }

            if (data.hasKill)
            {
                // clip the vertex if not alive
                builder.AppendLine("\t\t\t\t}");
                builder.AppendLine("\t\t\t\telse");
                builder.AppendLine("\t\t\t\t{");
                builder.AppendLine("\t\t\t\t\to.pos = -1.0;");

                if (hasColor)
                    builder.AppendLine("\t\t\t\t\to.col = 0;");

                builder.AppendLine("\t\t\t\t}");
                builder.AppendLine();
            }

            builder.AppendLine("\t\t\t\treturn o;");
            builder.AppendLine("\t\t\t}");
            builder.AppendLine();
            builder.AppendLine("\t\t\tfloat4 frag (ps_input i) : COLOR");
            builder.AppendLine("\t\t\t{");

            if (hasColor)
                builder.AppendLine("\t\t\t\treturn i.col;");
            else
                builder.AppendLine("\t\t\t\treturn float4(1.0,1.0,1.0,0.5);");

            builder.AppendLine("\t\t\t}");
            builder.AppendLine();
            builder.AppendLine("\t\t\tENDCG");
            builder.AppendLine("\t\t}");
            builder.AppendLine("\t}");
            builder.AppendLine("\tFallBack Off");
            builder.AppendLine("}");

            return builder.ToString();
        }

        private bool hasColor;
        private bool hasAlpha;
    }

    class BillboardOutputCompiler : OutputCompiler
    {
        public BillboardOutputCompiler(bool OrientAlongVelocity)
        {
            m_OrientAlongVelocity = OrientAlongVelocity;
        }

        public override bool MarkAttributes(Dictionary<VFXAttrib, int> attribs)
        {
            if (!UpdateFlag(attribs, CommonAttrib.Position))
                return false;

            hasColor = UpdateFlag(attribs, CommonAttrib.Color);
            hasAlpha = UpdateFlag(attribs, CommonAttrib.Alpha);
            hasSize = UpdateFlag(attribs, CommonAttrib.Size);

            if (m_OrientAlongVelocity)
                m_OrientAlongVelocity = UpdateFlag(attribs, CommonAttrib.Velocity);

            return true;
        }

        public override string GenerateSource(ShaderMetaData data)
        {
            StringBuilder builder = new StringBuilder();
            WriteCommonSourceHead(builder, data);

            builder.AppendLine();
            builder.AppendLine("\t\t\tstruct ps_input {");
            builder.AppendLine("\t\t\t\tfloat4 pos : SV_POSITION;");

            if (hasColor || hasAlpha)
                builder.AppendLine("\t\t\t\tnointerpolation float4 col : COLOR0;");

            // Tmp to rasterize a circle
            builder.AppendLine("\t\t\t\tfloat2 offsets : TEXCOORD0;");

            builder.AppendLine("\t\t\t};");
            builder.AppendLine();
            builder.AppendLine("\t\t\tps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)");
            builder.AppendLine("\t\t\t{");
            builder.AppendLine("\t\t\t\tps_input o;");

            builder.AppendLine();
            builder.AppendLine("\t\t\t\tuint index = (id >> 2) + instanceID * 16384;");

            if (data.hasKill)
            {
                builder.AppendLine("\t\t\t\tif (flags[index] == 1)");
                builder.AppendLine("\t\t\t\t{");
            }

            foreach (var buffer in data.attributeBuffers)
                if (buffer.Used(VFXContextDesc.Type.kTypeOutput))
                {
                    builder.Append("\t\t\t\t\tAttribute");
                    builder.Append(buffer.Index);
                    builder.Append(" attrib");
                    builder.Append(buffer.Index);
                    builder.Append(" = attribBuffer");
                    builder.Append(buffer.Index);
                    builder.AppendLine("[index];");
                }
            builder.AppendLine();

            if (hasSize)
            {
                builder.Append("\t\t\t\t\tfloat2 size = ");
                builder.WriteAttrib(CommonAttrib.Size, data);
                builder.AppendLine(" * 0.5f;");
            }
            else
                builder.AppendLine("\t\t\t\t\tconst float2 size = float2(0.005,0.005);");

            builder.AppendLine("\t\t\t\t\to.offsets.x = 2.0 * float(id & 1) - 1.0;");
            builder.AppendLine("\t\t\t\t\to.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;");
            builder.AppendLine();

            builder.Append("\t\t\t\t\tfloat3 worldPos = ");
            builder.WriteAttrib(CommonAttrib.Position, data);
            builder.AppendLine(";");
            builder.AppendLine();

            if (m_OrientAlongVelocity)
            {
                builder.Append("\t\t\t\t\tfloat3 up = normalize(");
                builder.WriteAttrib(CommonAttrib.Velocity, data);
                builder.AppendLine(");");
                builder.AppendLine("\t\t\t\t\tfloat3 side = normalize(cross(UnityWorldSpaceViewDir(worldPos),up));");
                builder.AppendLine("\t\t\t\t\tworldPos += side * o.offsets.x * size.x;");
                builder.AppendLine("\t\t\t\t\tworldPos += up * o.offsets.y * size.y;");
            }
            else
            {
                builder.AppendLine("\t\t\t\t\tworldPos += UNITY_MATRIX_MV[0].xyz * o.offsets.x * size.x;");
                builder.AppendLine("\t\t\t\t\tworldPos += UNITY_MATRIX_MV[1].xyz * o.offsets.y * size.y;");
            }

            builder.AppendLine();

            builder.AppendLine("\t\t\t\t\to.pos = mul (UNITY_MATRIX_MVP, float4(worldPos,1.0f));");

            if (hasColor || hasAlpha)
            {
                builder.Append("\t\t\t\t\to.col = float4(");

                if (hasColor)
                {
                    builder.WriteAttrib(CommonAttrib.Color, data);
                    builder.Append(".xyz,");
                }
                else
                    builder.Append("1.0,1.0,1.0,");

                if (hasAlpha)
                {
                    builder.WriteAttrib(CommonAttrib.Alpha, data);
                    builder.AppendLine(");");
                }
                else
                    builder.AppendLine("0.5);");
            }

            if (data.hasKill)
            {
                // clip the vertex if not alive
                builder.AppendLine("\t\t\t\t}");
                builder.AppendLine("\t\t\t\telse");
                builder.AppendLine("\t\t\t\t{");
                builder.AppendLine("\t\t\t\t\to.pos = -1.0;");

                if (hasColor)
                    builder.AppendLine("\t\t\t\t\to.col = 0;");

                builder.AppendLine("\t\t\t\t\to.offsets = 0;");

                builder.AppendLine("\t\t\t\t}");
                builder.AppendLine();
            }

            builder.AppendLine("\t\t\t\treturn o;");
            builder.AppendLine("\t\t\t}");
            builder.AppendLine();
            builder.AppendLine("\t\t\tfloat4 frag (ps_input i) : COLOR");
            builder.AppendLine("\t\t\t{");
            builder.AppendLine();

            builder.AppendLine("\t\t\t\tfloat lsqr = dot(i.offsets, i.offsets);");
            builder.AppendLine("\t\t\t\tif (lsqr > 1.0)");
            builder.AppendLine("\t\t\t\t\tdiscard;");
            builder.AppendLine();

            if (hasColor)
                builder.AppendLine("\t\t\t\treturn i.col;");
            else
                builder.AppendLine("\t\t\t\treturn float4(1.0,1.0,1.0,0.5);");

            builder.AppendLine("\t\t\t}");
            builder.AppendLine();
            builder.AppendLine("\t\t\tENDCG");
            builder.AppendLine("\t\t}");
            builder.AppendLine("\t}");
            builder.AppendLine("\tFallBack Off");
            builder.AppendLine("}");

            return builder.ToString();
        }

        private bool hasColor;
        private bool hasAlpha;
        private bool hasSize;
        private bool m_OrientAlongVelocity;
    }
}