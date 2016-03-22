using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

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

        public override void WritePreBlock(ShaderSourceBuilder builder, ShaderMetaData data)
        {
            builder.Write("float3 worldPos = ");
            builder.WriteAttrib(CommonAttrib.Position, data);
            builder.WriteLine(";");
            builder.WriteLine("o.pos = mul (UNITY_MATRIX_MVP, float4(worldPos,1.0f));");
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

        public override void WriteIndex(ShaderSourceBuilder builder, ShaderMetaData data) 
        {
            builder.WriteLine("uint index = (id >> 2) + instanceID * 16384;");
        }

        public override void WriteAdditionalVertexOutput(ShaderSourceBuilder builder, ShaderMetaData data)
        {
            if (m_HasFlipBook)
                builder.WriteLine("float3 offsets : TEXCOORD0; // u,v and index"); 
            else
                builder.WriteLine("float2 offsets : TEXCOORD0;");
        }

        private void WriteRotation(ShaderSourceBuilder builder, ShaderMetaData data)
        {
            builder.WriteLine("float2 sincosA;");
            builder.Write("sincos(radians(");
            builder.WriteAttrib(CommonAttrib.Angle, data);
            builder.Write("), sincosA.x, sincosA.y);");
            builder.WriteLine();
            builder.WriteLine("const float c = sincosA.y;");
            builder.WriteLine("const float s = sincosA.x;");
            builder.WriteLine("const float t = 1.0 - c;");
            builder.WriteLine("const float x = front.x;");
            builder.WriteLine("const float y = front.y;");
            builder.WriteLine("const float z = front.z;");
            builder.WriteLine();
            builder.WriteLine("float3x3 rot = float3x3(t * x * x + c, t * x * y - s * z, t * x * z + s * y,");
            builder.WriteLine("\t\t\t\t\tt * x * y + s * z, t * y * y + c, t * y * z - s * x,");
            builder.WriteLine("\t\t\t\t\tt * x * z - s * y, t * y * z + s * x, t * z * z + c);");
            builder.WriteLine();
        }

        public override void WritePreBlock(ShaderSourceBuilder builder, ShaderMetaData data)
        {
            if (m_HasSize)
            {
                builder.Write("float2 size = ");
                builder.WriteAttrib(CommonAttrib.Size, data);
                builder.WriteLine(" * 0.5f;");
            }
            else
                builder.WriteLine("const float2 size = float2(0.005,0.005);");

            builder.WriteLine("o.offsets.x = 2.0 * float(id & 1) - 1.0;");
            builder.WriteLine("o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;");
            builder.WriteLine();

            builder.Write("float3 worldPos = ");
            builder.WriteAttrib(CommonAttrib.Position, data);
            builder.WriteLine(";");
            builder.WriteLine();

            if (m_OrientAlongVelocity)
            {
                builder.WriteLine("float3 front = UnityWorldSpaceViewDir(worldPos);");
                builder.Write("float3 up = normalize(");
                builder.WriteAttrib(CommonAttrib.Velocity, data);
                builder.WriteLine(");");
                builder.WriteLine("float3 side = normalize(cross(front,up));"); 
  
                if (m_HasAngle)
                    builder.WriteLine("front = cross(up,side);");
            }
            else
            {
                if (m_HasAngle)
                    builder.WriteLine("float3 front = UNITY_MATRIX_MV[2].xyz;");

                builder.WriteLine("float3 side = UNITY_MATRIX_MV[0].xyz;");
                builder.WriteLine("float3 up = UNITY_MATRIX_MV[1].xyz;");
            }

            builder.WriteLine();

            if (m_HasAngle)
            {
                WriteRotation(builder, data);
                builder.WriteLine();
                builder.WriteLine("worldPos += mul(rot,side) * (o.offsets.x * size.x);");
                builder.WriteLine("worldPos += mul(rot,up) * (o.offsets.y * size.y);");
            }
            else
            {
                builder.WriteLine("worldPos += side * (o.offsets.x * size.x);");
                builder.WriteLine("worldPos += up * (o.offsets.y * size.y);");
            }

            if (m_HasTexture)
            {
                builder.WriteLine("o.offsets.xy = o.offsets.xy * 0.5 + 0.5;");
                if (m_HasFlipBook)
                {
                    builder.Write("o.offsets.z = ");
                    builder.WriteAttrib(CommonAttrib.TexIndex, data);
                    builder.WriteLine(";");
                }
            }

            builder.WriteLine();

            builder.WriteLine("o.pos = mul (UNITY_MATRIX_MVP, float4(worldPos,1.0f));");
        }

        public override void WritePixelShader(ShaderSourceBuilder builder, ShaderMetaData data)
        {
            if (!m_HasTexture)
            {
                builder.WriteLine("float lsqr = dot(i.offsets, i.offsets);");
                builder.WriteLine("if (lsqr > 1.0)");
                builder.WriteLine("\tdiscard;");
                builder.WriteLine();
            }
            else if (m_HasFlipBook)
            {
                builder.Write("float2 dim = ");
                builder.Write(data.outputParamToName[m_FlipBookDim]);
                builder.WriteLine(";");
                builder.WriteLine("float2 invDim = 1.0 / dim; // TODO InvDim should be computed on CPU");
                builder.WriteLine("float index = round(i.offsets.z);");
                builder.WriteLine("float2 tile = float2(fmod(index,dim.x),dim.y - 1.0 - floor(index * invDim.x));");
                builder.WriteLine("float2 uv = (tile + i.offsets.xy) * invDim; // TODO InvDim should be computed on CPU");
                builder.Write("color *= tex2D(");
                builder.Write(data.outputParamToName[m_Texture]);
                builder.WriteLine(",uv);");
            }
            else
            {
                builder.Write("color *= tex2D(");
                builder.Write(data.outputParamToName[m_Texture]);
                builder.WriteLine(",i.offsets);");
            }

            if (VFXEditor.AssetModel.BlendingMode == BlendMode.kMasked)
                builder.WriteLine("if (color.a < 0.33333) discard;");
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
