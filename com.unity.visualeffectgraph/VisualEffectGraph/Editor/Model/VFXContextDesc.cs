using UnityEngine;
using UnityEngine.Experimental.VFX;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental
{
    public abstract class VFXContextDesc
    {
        public static string GetFriendlyName(Type t)
        {
            switch(t)
            {
                case Type.kTypeNone: return "None";
                case Type.kTypeInit: return "Initialize";
                case Type.kTypeUpdate: return "Update";
                case Type.kTypeOutput: return "Output";
                default: return t.ToString();
            }
        }

        public enum Type
        {
            kTypeNone,
            kTypeInit,
            kTypeUpdate,
            kTypeOutput,
        };

        public static VFXContextDesc CreateBasic(Type type)
        {
            switch (type)
            {
                case Type.kTypeInit: return new VFXBasicInitialize();
                case Type.kTypeUpdate: return new VFXBasicUpdate();
                case Type.kTypeOutput: return new VFXBasicOutput();
            }

            throw new ArgumentException();
        }

        public VFXContextDesc(Type type,string name, bool showBlock = false)
        {
            m_Type = type;
            m_Name = name;
            m_ShowBlock = showBlock;
        }

        public const uint s_NbTypes = (uint)Type.kTypeOutput + 1;

        public Type m_Type;
        public VFXProperty[] m_Properties;

        public string Name { get { return m_Name; }}
        private string m_Name;

        public bool ShowBlock { get { return m_ShowBlock; }}
        private bool m_ShowBlock = false;

        public virtual VFXShaderGeneratorModule CreateShaderGenerator(VFXContextModel model) { return new VFXShaderGeneratorModule(); }

        public static string GetTypeName(Type type)
        {
            switch(type)
            {
                case Type.kTypeInit: return "Initialize";
                case Type.kTypeNone: return "None";
                case Type.kTypeOutput: return "Output";
                case Type.kTypeUpdate: return "Update";
                default: return "INVALID";
            }
        }

    }

    public class VFXBasicInitialize : VFXContextDesc
    {
        public VFXBasicInitialize() : base(Type.kTypeInit,"Initialize",false) {}
    }

    public class VFXBasicUpdate : VFXContextDesc
    {
        public VFXBasicUpdate() : base(Type.kTypeUpdate,"Update",false) {}
    }

    public class VFXBasicOutput : VFXContextDesc
    {
        public VFXBasicOutput() : base(Type.kTypeOutput,"Output",false) {}
        public override VFXShaderGeneratorModule CreateShaderGenerator(VFXContextModel model) { return new VFXOutputShaderGeneratorModule(); }
    }

    public class VFXPointOutputDesc : VFXContextDesc
    {
        public VFXPointOutputDesc()
            : base(Type.kTypeOutput,"Point Output",true)
        {}

        public override VFXShaderGeneratorModule CreateShaderGenerator(VFXContextModel model) { return new VFXPointOutputShaderGeneratorModule(); }
    }

    public class VFXBillboardOutputDesc : VFXContextDesc
    {
        private const int TextureSlot = 0;
        private const int FlipBookDimSlot = 1;

        public VFXBillboardOutputDesc()
            : base(Type.kTypeOutput, "Billboard Output", true)
        {
            m_Properties = new VFXProperty[2];
            m_Properties[TextureSlot] = VFXProperty.Create<VFXTexture2DType>("texture");
            m_Properties[FlipBookDimSlot] = VFXProperty.Create<VFXFloat2Type>("flipBook");
        }

        public override VFXShaderGeneratorModule CreateShaderGenerator(VFXContextModel model) 
        {
            VFXPropertySlot[] values = new VFXPropertySlot[2];
            values[VFXBillboardOutputShaderGeneratorModule.TextureIndex] = model.GetSlot(TextureSlot);
            values[VFXBillboardOutputShaderGeneratorModule.FlipbookDimIndex] = model.GetSlot(FlipBookDimSlot);
            return new VFXBillboardOutputShaderGeneratorModule(values, false); 
        }
    }

    public class VFXQuadAlongVelocityOutputDesc : VFXContextDesc
    {
        private const int TextureSlot = 0;
        private const int FlipBookDimSlot = 1;

        public VFXQuadAlongVelocityOutputDesc()
            : base(Type.kTypeOutput, "Quad Along Velocity Output", true)
        {
            m_Properties = new VFXProperty[2];
            m_Properties[TextureSlot] = VFXProperty.Create<VFXTexture2DType>("texture");
            m_Properties[FlipBookDimSlot] = VFXProperty.Create<VFXFloat2Type>("flipBook");
        }

        public override VFXShaderGeneratorModule CreateShaderGenerator(VFXContextModel model) 
        {
            VFXPropertySlot[] values = new VFXPropertySlot[2];
            values[VFXBillboardOutputShaderGeneratorModule.TextureIndex] = model.GetSlot(TextureSlot);
            values[VFXBillboardOutputShaderGeneratorModule.FlipbookDimIndex] = model.GetSlot(FlipBookDimSlot);
            return new VFXBillboardOutputShaderGeneratorModule(values, true); 
        }
    }

    public class VFXMorphSubUVBillboardOutputDesc : VFXContextDesc
    {
        private const int TextureSlot = 0;
        private const int FlipBookDimSlot = 1;
        private const int MorphTextureSlot = 2;
        private const int MorphIntensitySlot = 3;

        public VFXMorphSubUVBillboardOutputDesc()
            : base(Type.kTypeOutput, "SubUV Morph Billboard", true)
        {
            m_Properties = new VFXProperty[4];
            m_Properties[TextureSlot] = VFXProperty.Create<VFXTexture2DType>("texture");
            m_Properties[FlipBookDimSlot] = VFXProperty.Create<VFXFloat2Type>("flipBook");
            m_Properties[MorphTextureSlot] = VFXProperty.Create<VFXTexture2DType>("MotionVectors2D");
            m_Properties[MorphIntensitySlot] = VFXProperty.Create<VFXFloatType>("MorphIntensity");

        }

        public override VFXShaderGeneratorModule CreateShaderGenerator(VFXContextModel model)
        {
            VFXPropertySlot[] values = new VFXPropertySlot[4];
            values[VFXBillboardOutputShaderGeneratorModule.TextureIndex] = model.GetSlot(TextureSlot);
            values[VFXBillboardOutputShaderGeneratorModule.FlipbookDimIndex] = model.GetSlot(FlipBookDimSlot);
            values[VFXBillboardOutputShaderGeneratorModule.MorphTextureIndex] = model.GetSlot(MorphTextureSlot);
            values[VFXBillboardOutputShaderGeneratorModule.MorphIntensityIndex] = model.GetSlot(MorphIntensitySlot);
            return new VFXBillboardOutputShaderGeneratorModule(values, false); 
        }
    }

    public class VFXParticleUpdate : VFXContextDesc
    {
        public VFXParticleUpdate()
            : base(Type.kTypeUpdate, "Particle Update", true)
        {}

        private class ShaderGenerator : VFXShaderGeneratorModule
        {
            public override bool UpdateAttributes(Dictionary<VFXAttribute, int> attribs, ref VFXBlockDesc.Flag flags)
            {
                if (attribs.ContainsKey(CommonAttrib.Velocity))
                {
                    m_NeedsIntegration = true;
                    AddOrUpdateFlag(attribs, CommonAttrib.Position, Type.kTypeUpdate, true);
                    UpdateFlag(attribs, CommonAttrib.Velocity, Type.kTypeUpdate, false);
                }

                if (attribs.ContainsKey(CommonAttrib.Lifetime))
                {
                    m_NeedsReaping = true;
                    UpdateFlag(attribs, CommonAttrib.Lifetime, Type.kTypeUpdate, false);
                    flags |= VFXBlockDesc.Flag.kHasKill;
                }

                if (m_NeedsReaping || attribs.ContainsKey(CommonAttrib.Age))
                {
                    m_NeedsAging = true;
                    AddOrUpdateFlag(attribs, CommonAttrib.Age, Type.kTypeUpdate, true); // For aging
                }

                if (attribs.ContainsKey(CommonAttrib.AngularVelocity))
                {
                    m_NeedsAngularIntegration = true;
                    UpdateFlag(attribs, CommonAttrib.AngularVelocity, Type.kTypeUpdate, false);
                    AddOrUpdateFlag(attribs, CommonAttrib.Angle, Type.kTypeUpdate, true);
                }
 
                return true;
            }

            public override void WritePostBlock(ShaderSourceBuilder builder, ShaderMetaData data)
            {
                if (m_NeedsAngularIntegration)
                {
                    builder.WriteAttrib(CommonAttrib.Angle, data);
                    builder.Write(" += ");
                    builder.WriteAttrib(CommonAttrib.AngularVelocity, data);
                    builder.WriteLine(" * deltaTime;");
                    builder.WriteLine();
                }

                if (m_NeedsIntegration)
                {
                    builder.WriteAttrib(CommonAttrib.Position,data);
                    builder.Write(" += ");
                    builder.WriteAttrib(CommonAttrib.Velocity,data);
                    builder.WriteLine(" * deltaTime;");
                    builder.WriteLine();
                }

                if (m_NeedsAging)
                {
                    builder.WriteAttrib(CommonAttrib.Age, data);
                    builder.WriteLine(" += deltaTime;");
                    
                    if (m_NeedsReaping)
                    {
                        builder.Write("if (");
                        builder.WriteAttrib(CommonAttrib.Age, data);
                        builder.Write(" >= ");
                        builder.WriteAttrib(CommonAttrib.Lifetime, data);
                        builder.WriteLine(")");
                        builder.WriteLine("\tkill = true;");
                        builder.WriteLine();
                    }
                }
            }

            private bool m_NeedsAging;
            private bool m_NeedsReaping;
            private bool m_NeedsIntegration;
            private bool m_NeedsAngularIntegration;
        }

        public override VFXShaderGeneratorModule CreateShaderGenerator(VFXContextModel model) { return new ShaderGenerator(); }
    }
}
