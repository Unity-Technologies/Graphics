using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UnityEditor.Experimental
{
    public abstract class VFXContextDesc
    {
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
        public VFXParam[] m_Params;

        public string Name { get { return m_Name; }}
        private string m_Name;

        public bool ShowBlock { get { return m_ShowBlock; }}
        private bool m_ShowBlock = false;

        public virtual VFXShaderGeneratorModule CreateShaderGenerator() { return new VFXShaderGeneratorModule(); }

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
        public override VFXShaderGeneratorModule CreateShaderGenerator() { return new VFXOutputShaderGeneratorModule(); }
    }

    public class VFXPointOutputDesc : VFXContextDesc
    {
        public VFXPointOutputDesc()
            : base(Type.kTypeOutput,"Point Output",true)
        {}

        public override VFXShaderGeneratorModule CreateShaderGenerator() { return new VFXPointOutputShaderGeneratorModule(); }
    }

    public class VFXBillboardOutputDesc : VFXContextDesc
    {
        public VFXBillboardOutputDesc()
            : base(Type.kTypeOutput,"Billboard Output",true)
        {
            VFXParam textureParam = new VFXParam();
            textureParam.m_Name = "texture";
            textureParam.m_Type = VFXParam.Type.kTypeTexture2D;

            m_Params = new VFXParam[1];
            m_Params[0] = textureParam;
        }

        public override VFXShaderGeneratorModule CreateShaderGenerator() { return new VFXBillboardOutputShaderGeneratorModule(false); }
    }

    public class VFXQuadAlongVelocityOutputDesc : VFXContextDesc
    {
        public VFXQuadAlongVelocityOutputDesc()
            : base(Type.kTypeOutput, "Quad Along Velocity Output", true)
        {
            VFXParam textureParam = new VFXParam();
            textureParam.m_Name = "texture";
            textureParam.m_Type = VFXParam.Type.kTypeTexture2D;

            m_Params = new VFXParam[1];
            m_Params[0] = textureParam;
        }

        public override VFXShaderGeneratorModule CreateShaderGenerator() { return new VFXBillboardOutputShaderGeneratorModule(true); }
    }

    public class VFXParticleUpdate : VFXContextDesc
    {
        public VFXParticleUpdate()
            : base(Type.kTypeUpdate, "Particle Update", true)
        {}

        private class ShaderGenerator : VFXShaderGeneratorModule
        {
            public override bool UpdateAttributes(Dictionary<VFXAttrib, int> attribs, ref int flags)
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
                    flags |= (int)VFXBlock.Flag.kHasKill;
                }

                if (m_NeedsReaping || attribs.ContainsKey(CommonAttrib.Age))
                {
                    m_NeedsAging = true;
                    AddOrUpdateFlag(attribs, CommonAttrib.Age, Type.kTypeUpdate, true); // For aging
                }
 
                return true;
            }

            public override void WritePostBlock(StringBuilder builder, ShaderMetaData data)
            {
                if (m_NeedsIntegration)
                {
                    builder.WriteAttrib(CommonAttrib.Position,data);
                    builder.Append(" += ");
                    builder.WriteAttrib(CommonAttrib.Velocity,data);
                    builder.AppendLine(" * deltaTime;");
                    builder.AppendLine();
                }

                if (m_NeedsAging)
                {
                    builder.WriteAttrib(CommonAttrib.Age, data);
                    builder.AppendLine(" += deltaTime;");
                    
                    if (m_NeedsReaping)
                    {
                        builder.Append("if (");
                        builder.WriteAttrib(CommonAttrib.Age, data);
                        builder.Append(" >= ");
                        builder.WriteAttrib(CommonAttrib.Lifetime, data);
                        builder.AppendLine(")");
                        builder.AppendLine("kill = true;");
                    }
                }
            }

            private bool m_NeedsAging;
            private bool m_NeedsReaping;
            private bool m_NeedsIntegration;
        }

        public override VFXShaderGeneratorModule CreateShaderGenerator() { return new ShaderGenerator(); }
    }
}