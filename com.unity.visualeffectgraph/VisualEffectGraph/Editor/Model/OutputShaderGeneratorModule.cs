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
        public VFXBillboardOutputShaderGeneratorModule(VFXParamValue texture, VFXParamValue flipBookDim, bool orientAlongVelocity)
        {
            m_Texture = texture;
            m_FlipBookDim = flipBookDim;
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
            m_HasAngle = UpdateFlag(attribs, CommonAttrib.Angle, VFXContextDesc.Type.kTypeOutput);

            if (m_Texture.GetValue<Texture2D>() != null)
            {
                m_HasTexture = true;
                m_HasFlipBook = UpdateFlag(attribs, CommonAttrib.TexIndex, VFXContextDesc.Type.kTypeOutput);   
            }
            
            if (m_OrientAlongVelocity)
                m_OrientAlongVelocity = UpdateFlag(attribs, CommonAttrib.Velocity, VFXContextDesc.Type.kTypeOutput);
           
            return true;
        }

        public override void UpdateUniforms(HashSet<VFXParamValue> uniforms)
        {
            if (m_HasTexture)
            {
                uniforms.Add(m_Texture);
                if (m_HasFlipBook)
                    uniforms.Add(m_FlipBookDim);
            }
        }

        public override void WriteIndex(StringBuilder builder, ShaderMetaData data) 
        {
            builder.AppendLine("\t\t\t\tuint index = (id >> 2) + instanceID * 16384;");
        }

        public override void WriteAdditionalVertexOutput(StringBuilder builder, ShaderMetaData data)
        {
            if (m_HasFlipBook)
                builder.AppendLine("\t\t\t\tfloat3 offsets : TEXCOORD0; // u,v and index"); 
            else
                builder.AppendLine("\t\t\t\tfloat2 offsets : TEXCOORD0;");
        }

        private void WriteRotation(StringBuilder builder, ShaderMetaData data)
        {
            builder.AppendLine("\t\t\t\t\tfloat2 sincosA;");
            builder.Append("\t\t\t\t\tsincos(radians(");
            builder.WriteAttrib(CommonAttrib.Angle, data);
            builder.Append("), sincosA.x, sincosA.y);");
            builder.AppendLine();
            builder.AppendLine("\t\t\t\t\tconst float c = sincosA.y;");
            builder.AppendLine("\t\t\t\t\tconst float s = sincosA.x;");
            builder.AppendLine("\t\t\t\t\tconst float t = 1.0 - c;");
            builder.AppendLine("\t\t\t\t\tconst float x = front.x;");
            builder.AppendLine("\t\t\t\t\tconst float y = front.y;");
            builder.AppendLine("\t\t\t\t\tconst float z = front.z;");
            builder.AppendLine();
            builder.AppendLine("\t\t\t\t\tfloat3x3 rot = float3x3(t * x * x + c, t * x * y - s * z, t * x * z + s * y,");
            builder.AppendLine("\t\t\t\t\t\t\t\t\t\tt * x * y + s * z, t * y * y + c, t * y * z - s * x,");
            builder.AppendLine("\t\t\t\t\t\t\t\t\t\tt * x * z - s * y, t * y * z + s * x, t * z * z + c);");
            builder.AppendLine();
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
                builder.AppendLine("\t\t\t\t\tfloat3 front = UnityWorldSpaceViewDir(worldPos);");
                builder.Append("\t\t\t\t\tfloat3 up = normalize(");
                builder.WriteAttrib(CommonAttrib.Velocity, data);
                builder.AppendLine(");");
                builder.AppendLine("\t\t\t\t\tfloat3 side = normalize(cross(front,up));"); 
  
                if (m_HasAngle)
                    builder.AppendLine("\t\t\t\t\tfront = cross(up,side);");
            }
            else
            {
                if (m_HasAngle)
                    builder.AppendLine("\t\t\t\t\tfloat3 front = UNITY_MATRIX_MV[2].xyz;");

                builder.AppendLine("\t\t\t\t\tfloat3 side = UNITY_MATRIX_MV[0].xyz;");
                builder.AppendLine("\t\t\t\t\tfloat3 up = UNITY_MATRIX_MV[1].xyz;");
            }

            builder.AppendLine();

            if (m_HasAngle)
            {
                WriteRotation(builder, data);
                builder.AppendLine();
                builder.AppendLine("\t\t\t\t\tworldPos += mul(rot,side) * (o.offsets.x * size.x);");
                builder.AppendLine("\t\t\t\t\tworldPos += mul(rot,up) * (o.offsets.y * size.y);");
            }
            else
            {
                builder.AppendLine("\t\t\t\t\tworldPos += side * (o.offsets.x * size.x);");
                builder.AppendLine("\t\t\t\t\tworldPos += up * (o.offsets.y * size.y);");
            }

            if (m_HasTexture)
            {
                builder.AppendLine("\t\t\t\t\to.offsets.xy = o.offsets.xy * 0.5 + 0.5;");
                if (m_HasFlipBook)
                {
                    builder.Append("\t\t\t\t\to.offsets.z = ");
                    builder.WriteAttrib(CommonAttrib.TexIndex, data);
                    builder.AppendLine(";");
                }
            }

            builder.AppendLine();

            builder.AppendLine("\t\t\t\t\to.pos = mul (UNITY_MATRIX_MVP, float4(worldPos,1.0f));");
        }

        public override void WritePixelShader(StringBuilder builder, ShaderMetaData data)
        {
            if (!m_HasTexture)
            {
                builder.AppendLine("\t\t\t\tfloat lsqr = dot(i.offsets, i.offsets);");
                builder.AppendLine("\t\t\t\tif (lsqr > 1.0)");
                builder.AppendLine("\t\t\t\t\tdiscard;");
                builder.AppendLine();
            }
            else if (m_HasFlipBook)
            {
                builder.Append("\t\t\t\tfloat2 dim = ");
                builder.Append(data.outputParamToName[m_FlipBookDim]);
                builder.AppendLine(";");
                builder.AppendLine("\t\t\t\tfloat2 invDim = 1.0 / dim; // TODO InvDim should be computed on CPU");
                builder.AppendLine("\t\t\t\tfloat index = round(i.offsets.z);");
                builder.AppendLine("\t\t\t\tfloat2 tile = float2(fmod(index,dim.x),dim.y - 1.0 - floor(index * invDim.x));");
                builder.AppendLine("\t\t\t\tfloat2 uv = (tile + i.offsets.xy) * invDim; // TODO InvDim should be computed on CPU");
                builder.Append("\t\t\t\tcolor *= tex2D(");
                builder.Append(data.outputParamToName[m_Texture]);
                builder.AppendLine(",uv);");
            }
            else
            {
                builder.Append("\t\t\t\tcolor *= tex2D(");
                builder.Append(data.outputParamToName[m_Texture]);
                builder.AppendLine(",i.offsets);");
            }
        }

        private VFXParamValue m_Texture;
        private VFXParamValue m_FlipBookDim;

        private bool m_HasSize;
        private bool m_HasAngle;
        private bool m_HasFlipBook;
        private bool m_HasTexture;
        private bool m_OrientAlongVelocity;
    }
}
