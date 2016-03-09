using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UnityEditor.Experimental
{
    public class VFXPointOutputShaderGeneratorModule : VFXOutputShaderGeneratorModule
    {
        public override bool UpdateAttributes(Dictionary<VFXAttrib, int> attribs, ref int flags)
        {
            if (!UpdateFlag(attribs, CommonAttrib.Position, VFXContextDesc.Type.kTypeOutput))
                return false;

            UpdateFlag(attribs, CommonAttrib.Color, VFXContextDesc.Type.kTypeOutput);
            UpdateFlag(attribs, CommonAttrib.Alpha, VFXContextDesc.Type.kTypeOutput);
            return true;
        }

        public override void WritePreBlock(StringBuilder builder, ShaderMetaData data)
        {
            builder.Append("\t\t\t\t\tfloat3 worldPos = ");
            builder.WriteAttrib(CommonAttrib.Position, data);
            builder.AppendLine(";");
            builder.AppendLine("\t\t\t\t\to.pos = mul (UNITY_MATRIX_MVP, float4(worldPos,1.0f));");
        }
    }

    public class VFXBillboardOutputShaderGeneratorModule : VFXOutputShaderGeneratorModule
    {
        public VFXBillboardOutputShaderGeneratorModule(bool orientAlongVelocity)
        {
            m_OrientAlongVelocity = orientAlongVelocity;
        }

        public override int[] GetSingleIndexBuffer(ShaderMetaData data) { return new int[0]; } // tmp

        public override bool UpdateAttributes(Dictionary<VFXAttrib, int> attribs, ref int flags)
        {
            if (!UpdateFlag(attribs, CommonAttrib.Position, VFXContextDesc.Type.kTypeOutput))
                return false;

            UpdateFlag(attribs, CommonAttrib.Color, VFXContextDesc.Type.kTypeOutput);
            UpdateFlag(attribs, CommonAttrib.Alpha, VFXContextDesc.Type.kTypeOutput);
            m_HasSize = UpdateFlag(attribs, CommonAttrib.Size, VFXContextDesc.Type.kTypeOutput);

            if (m_OrientAlongVelocity)
                m_OrientAlongVelocity = UpdateFlag(attribs, CommonAttrib.Velocity, VFXContextDesc.Type.kTypeOutput);

            return true;
        }

        public override void WriteIndex(StringBuilder builder, ShaderMetaData data) 
        {
            builder.AppendLine("\t\t\t\tuint index = (id >> 2) + instanceID * 16384;");
        }

        public override void WriteAdditionalVertexOutput(StringBuilder builder, ShaderMetaData data)
        {
            builder.AppendLine("\t\t\t\tfloat2 offsets : TEXCOORD0;");
        }

        public override void WritePreBlock(StringBuilder builder, ShaderMetaData data)
        {
            if (m_HasSize)
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
        }

        public override void WritePixelShader(StringBuilder builder, ShaderMetaData data)
        {
            builder.AppendLine("\t\t\t\tfloat lsqr = dot(i.offsets, i.offsets);");
            builder.AppendLine("\t\t\t\tif (lsqr > 1.0)");
            builder.AppendLine("\t\t\t\t\tdiscard;");
            builder.AppendLine();
        }

        private bool m_HasSize;
        private bool m_OrientAlongVelocity;
    }
}
