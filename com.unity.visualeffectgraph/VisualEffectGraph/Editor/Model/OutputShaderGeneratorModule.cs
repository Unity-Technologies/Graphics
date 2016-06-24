using UnityEngine;
using UnityEngine.Experimental.VFX;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental
{
    public class VFXPointOutputShaderGeneratorModule : VFXOutputShaderGeneratorModule
    {
        public override bool UpdateAttributes(Dictionary<VFXAttribute, int> attribs, ref VFXBlockDesc.Flag flags)
        {
            if (!UpdateFlag(attribs, CommonAttrib.Position, VFXContextDesc.Type.kTypeOutput))
            {
                Debug.LogError("Position attribute is needed for point output context");
                return false;
            }

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
        public enum OrientMode
        {
            kFaceCamera,
            kVelocity,
            kRotateAxis,
            kFixed
        }

        public const int TextureIndex = 0;
        public const int FlipbookDimIndex = 1;
        public const int MorphTextureIndex = 2;
        public const int MorphIntensityIndex = 3;

        public const int FirstLockedAxisIndex = 2;
        public const int SecondLockedAxisIndex = 3;

        public VFXBillboardOutputShaderGeneratorModule(VFXPropertySlot[] slots, OrientMode orientmode)
        {
            for (int i = 0; i < Math.Min(slots.Length, 4); ++i)
                m_Values[i] = slots[i].ValueRef.Reduce() as VFXValue; // TODO Refactor
            m_OrientMode = orientmode;
        }

        public override int[] GetSingleIndexBuffer(ShaderMetaData data) { return new int[0]; } // tmp

        private bool CanHaveMotionVectors()
        {
            return m_OrientMode != OrientMode.kRotateAxis && m_OrientMode != OrientMode.kFixed;
        }


        public override bool UpdateAttributes(Dictionary<VFXAttribute, int> attribs, ref VFXBlockDesc.Flag flags)
        {
            if (!UpdateFlag(attribs, CommonAttrib.Position, VFXContextDesc.Type.kTypeOutput))
            {
                Debug.LogError("Position attribute is needed for billboard output context");
                return false;
            }

            UpdateFlag(attribs, CommonAttrib.Color, VFXContextDesc.Type.kTypeOutput);
            UpdateFlag(attribs, CommonAttrib.Alpha, VFXContextDesc.Type.kTypeOutput);
            m_HasSize = UpdateFlag(attribs, CommonAttrib.Size, VFXContextDesc.Type.kTypeOutput);
            m_HasAngle = UpdateFlag(attribs, CommonAttrib.Angle, VFXContextDesc.Type.kTypeOutput);
            m_HasPivot = UpdateFlag(attribs, CommonAttrib.Pivot, VFXContextDesc.Type.kTypeOutput);

            if (m_Values[TextureIndex] != null)
            {
                m_HasTexture = true;
                if (m_HasFlipBook = m_Values[FlipbookDimIndex] != null && UpdateFlag(attribs, CommonAttrib.TexIndex, VFXContextDesc.Type.kTypeOutput))
                    m_HasMotionVectors = CanHaveMotionVectors() && m_Values[MorphTextureIndex] != null && m_Values[MorphIntensityIndex] != null && m_Values[MorphTextureIndex].Get<Texture2D>() != null;
            }

            if (m_OrientMode == OrientMode.kVelocity)
            {
                if(!UpdateFlag(attribs, CommonAttrib.Velocity, VFXContextDesc.Type.kTypeOutput))
                    m_OrientMode = OrientMode.kFaceCamera;
            } 

            return true;
        }

        public override void UpdateUniforms(HashSet<VFXExpression> uniforms)
        {
            if (m_HasTexture)
            {
                uniforms.Add(m_Values[TextureIndex]);
                if (m_HasFlipBook)
                {
                    uniforms.Add(m_Values[FlipbookDimIndex]);
                    if (m_HasMotionVectors)
                    {
                        uniforms.Add(m_Values[MorphTextureIndex]);
                        uniforms.Add(m_Values[MorphIntensityIndex]);
                    }
                }
            }

            switch(m_OrientMode)
            {
                case OrientMode.kRotateAxis:
                    uniforms.Add(m_Values[FirstLockedAxisIndex]);
                    break;
                case OrientMode.kFixed:
                    uniforms.Add(m_Values[FirstLockedAxisIndex]);
                    uniforms.Add(m_Values[SecondLockedAxisIndex]);
                    break;
                default: break;
            }
        }

        public override void UpdateExpressions(HashSet<VFXExpression> expressions)
        {
            if (m_HasTexture)
            {
                expressions.Add(m_Values[TextureIndex]);
                if (m_HasFlipBook)
                {
                    expressions.Add(m_Values[FlipbookDimIndex]);
                    if (m_HasMotionVectors)
                    {
                        expressions.Add(m_Values[MorphTextureIndex]);
                        expressions.Add(m_Values[MorphIntensityIndex]);
                    }
                }
            }

            switch(m_OrientMode)
            {
                case OrientMode.kRotateAxis:
                    expressions.Add(m_Values[FirstLockedAxisIndex]);
                    break;
                case OrientMode.kFixed:
                    expressions.Add(m_Values[FirstLockedAxisIndex]);
                    expressions.Add(m_Values[SecondLockedAxisIndex]);
                    break;
                default: break;
            }
        }

        public override void WriteIndex(ShaderSourceBuilder builder, ShaderMetaData data)
        {
            builder.WriteLine("uint index = (id >> 2) + instanceID * 16384;");
        }

        public override void WriteAdditionalVertexOutput(ShaderSourceBuilder builder, ShaderMetaData data)
        {
            builder.WriteLine("float2 offsets : TEXCOORD0;");
            if (m_HasFlipBook)
                builder.WriteLine("nointerpolation float flipbookIndex : TEXCOORD1;");
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
            const bool CLAMP_SIZE = false; // false atm

            if (m_HasSize)
            {
                builder.Write("float2 size = ");
                builder.WriteAttrib(CommonAttrib.Size, data);
                builder.WriteLine(" * 0.5f;");
            }
            else
                builder.WriteLine("float2 size = float2(0.005,0.005);");

            builder.WriteLine("o.offsets.x = 2.0 * float(id & 1) - 1.0;");
            builder.WriteLine("o.offsets.y = 2.0 * float((id & 2) >> 1) - 1.0;");
            builder.WriteLine();

            builder.Write("float3 worldPos = ");
            builder.WriteAttrib(CommonAttrib.Position, data);
            builder.WriteLine(";");
            builder.WriteLine();

            if (CLAMP_SIZE)
            {
                builder.WriteLine("// Clamp size so that billboards are never less than one pixel size");
                builder.WriteLine("const float PIXEL_SIZE = 0.003f; // This should be a uniform depending on fov and viewport dimension");
                builder.WriteLine("float minSize = dot(UNITY_MATRIX_VP[3],float4(worldPos,1.0f)) * PIXEL_SIZE; // w * pixel size");
                builder.WriteLine("size = max(size,minSize);");
                builder.WriteLine();
            }

            if (m_HasPivot)
            {
                builder.WriteLine("float2 posOffsets = o.offsets.xy - ");
                builder.WriteAttrib(CommonAttrib.Pivot, data);
                builder.WriteLine(".xy;");
                builder.WriteLine();
            }
            else
            {
                builder.WriteLine("float2 posOffsets = o.offsets.xy;");
            }

            switch (m_OrientMode)
            {
                case OrientMode.kVelocity:

                    builder.WriteLine("float3 front = UnityWorldSpaceViewDir(worldPos);");
                    builder.Write("float3 up = normalize(");
                    builder.WriteAttrib(CommonAttrib.Velocity, data);
                    builder.WriteLine(");");
                    builder.WriteLine("float3 side = normalize(cross(front,up));");

                    if (m_HasAngle)
                        builder.WriteLine("front = cross(up,side);");

                    break;

                case OrientMode.kFaceCamera:

                    if (m_HasAngle || m_HasPivot)
                        builder.WriteLine("float3 front = UNITY_MATRIX_MV[2].xyz;");

                    builder.WriteLine("float3 side = UNITY_MATRIX_IT_MV[0].xyz;");
                    builder.WriteLine("float3 up = UNITY_MATRIX_IT_MV[1].xyz;");

                    break;

                case OrientMode.kRotateAxis:

                    builder.WriteLine("float3 front = UnityWorldSpaceViewDir(worldPos);");
                    builder.Write("float3 up = normalize(");
                    builder.Write(data.outputParamToName[m_Values[FirstLockedAxisIndex]]);
                    builder.WriteLine(");");
                    builder.WriteLine("float3 side = normalize(cross(front,up));");

                    if (m_HasAngle)
                        builder.WriteLine("front = cross(up,side);");
                    break;

                case OrientMode.kFixed:

                    builder.Write("float3 front = ");
                    builder.Write(data.outputParamToName[m_Values[SecondLockedAxisIndex]]);
                    builder.Write(";");
                    builder.Write("float3 up = normalize(");
                    builder.Write(data.outputParamToName[m_Values[FirstLockedAxisIndex]]);
                    builder.WriteLine(");");
                    builder.WriteLine("float3 side = normalize(cross(front,up));");

                    if (m_HasAngle)
                        builder.WriteLine("front = cross(up,side);");
                    break;

                default: break;
            }


            builder.WriteLine();

            if (m_HasAngle)
            {
                WriteRotation(builder, data);
                builder.WriteLine();
                builder.WriteLine("worldPos += mul(rot,side * posOffsets.x * size.x);");
                builder.WriteLine("worldPos += mul(rot,up * posOffsets.y * size.y);");
            }
            else
            {
                builder.WriteLine("worldPos += side * (posOffsets.x * size.x);");
                builder.WriteLine("worldPos += up * (posOffsets.y * size.y);");
            }

            if (m_HasPivot)
            {
                builder.WriteLine("worldPos -= front * ");
                builder.WriteAttrib(CommonAttrib.Pivot, data);
                builder.WriteLine(".z;");
            }

            // local space (tmp)
            //builder.WriteLine("worldPos = mul(unity_ObjectToWorld,float4(worldPos,1.0f)).xyz;");

            if (m_HasTexture)
            {
                builder.WriteLine("o.offsets.xy = o.offsets.xy * 0.5 + 0.5;");
                if (m_HasFlipBook)
                {
                    builder.Write("o.flipbookIndex = ");
                    builder.WriteAttrib(CommonAttrib.TexIndex, data);
                    builder.WriteLine(";");
                }
            }

            builder.WriteLine();
            builder.WriteLine("o.pos = mul (UNITY_MATRIX_MVP, float4(worldPos,1.0f));");
        }

        public override void WriteFunctions(ShaderSourceBuilder builder, ShaderMetaData data)
        {
            if (m_HasFlipBook)
            {
                builder.WriteLine("float2 GetSubUV(int flipBookIndex,float2 uv,float2 dim,float2 invDim)");
                builder.EnterScope();
                builder.WriteLine("float2 tile = float2(fmod(flipBookIndex,dim.x),dim.y - 1.0 - floor(flipBookIndex * invDim.x));");
                builder.WriteLine("return (tile + uv) * invDim;");
                builder.ExitScope();
                builder.WriteLine();
            }
        }

        private static void WriteTex2DFetch(ShaderSourceBuilder builder, ShaderMetaData data, VFXValue texture, string uv, bool endLine)
        {
            builder.Write("tex2D(");
            builder.Write(data.outputParamToName[texture]);
            builder.Write(",");
            builder.Write(uv);
            builder.Write(")");
            if (endLine)
                builder.WriteLine(";");
        }

        public override void WritePixelShader(VFXSystemModel system, ShaderSourceBuilder builder, ShaderMetaData data)
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
                builder.Write(data.outputParamToName[m_Values[FlipbookDimIndex]]);
                builder.WriteLine(";");
                builder.WriteLine("float2 invDim = 1.0 / dim; // TODO InvDim should be computed on CPU");

                if (!m_HasMotionVectors)
                {
                    const bool INTERPOLATE = true; // TODO Add a toggle on block

                    if (!INTERPOLATE)
                    {
                        builder.WriteLine("float2 uv = GetSubUV(i.flipbookIndex,i.offsets.xy,dim,invDim);");
                        builder.Write("color *= ");
                        WriteTex2DFetch(builder, data, m_Values[TextureIndex], "uv", true);
                    }
                    else
                    {
                        builder.WriteLine("float ratio = frac(i.flipbookIndex);");
                        builder.WriteLine("float index = i.flipbookIndex - ratio;");
                        builder.WriteLine();

                        builder.WriteLine("float2 uv1 = GetSubUV(index,i.offsets.xy,dim,invDim);");
                        builder.Write("float4 col1 = ");
                        WriteTex2DFetch(builder, data, m_Values[TextureIndex], "uv1", true);
                        builder.WriteLine();

                        builder.WriteLine("float2 uv2 = GetSubUV(index + 1.0,i.offsets.xy,dim,invDim);");
                        builder.Write("float4 col2 = ");
                        WriteTex2DFetch(builder, data, m_Values[TextureIndex], "uv2", true);
                        builder.WriteLine();

                        builder.WriteLine("color *= lerp(col1,col2,ratio);");
                    }
                }
                else
                {
                    builder.WriteLine("float ratio = frac(i.flipbookIndex);");
                    builder.WriteLine("float index = i.flipbookIndex - ratio;");
                    builder.WriteLine();

                    builder.WriteLine("float2 uv1 = GetSubUV(index,i.offsets.xy,dim,invDim);");
                    builder.Write("float2 duv1 = ");
                    WriteTex2DFetch(builder, data, m_Values[MorphTextureIndex], "uv1", false);
                    builder.WriteLine(".rg - 0.5;");
                    builder.WriteLine();

                    builder.WriteLine("float2 uv2 = GetSubUV(index + 1.0,i.offsets.xy,dim,invDim);");
                    builder.Write("float2 duv2 = ");
                    WriteTex2DFetch(builder, data, m_Values[MorphTextureIndex], "uv2", false);
                    builder.WriteLine(".rg - 0.5;");
                    builder.WriteLine();

                    builder.Write("float morphIntensity = ");
                    builder.Write(data.outputParamToName[m_Values[MorphIntensityIndex]]);
                    builder.WriteLine(";");
                    builder.WriteLine("duv1 *= morphIntensity * ratio;");
                    builder.WriteLine("duv2 *= morphIntensity * (ratio - 1.0);");
                    builder.WriteLine();

                    builder.Write("float4 col1 = ");
                    WriteTex2DFetch(builder, data, m_Values[TextureIndex], "uv1 - duv1", true);
                    builder.Write("float4 col2 = ");
                    WriteTex2DFetch(builder, data, m_Values[TextureIndex], "uv2 - duv2", true);
                    builder.WriteLine();

                    builder.WriteLine("color *= lerp(col1,col2,ratio);");
                }

            }
            else
            {
                builder.Write("color *= ");
                WriteTex2DFetch(builder, data, m_Values[TextureIndex], "i.offsets", true);
            }

            if (system.BlendingMode == BlendMode.kMasked)
                builder.WriteLine("if (color.a < 0.33333) discard;");
        }

        private VFXValue[] m_Values = new VFXValue[4];

        private bool m_HasSize;
        private bool m_HasAngle;
        private bool m_HasFlipBook;
        private bool m_HasTexture;
        private OrientMode m_OrientMode;
        private bool m_HasMotionVectors;
        private bool m_HasPivot;
        private bool m_UseSoftParticles;
    }
}

