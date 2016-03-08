using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

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
    }

    public class VFXParticleUpdate : VFXContextDesc
    {
        public VFXParticleUpdate()
            : base(Type.kTypeUpdate,"Particle Update",true)
        {}
    }

    public class VFXPointOutputDesc : VFXContextDesc
    {
        public VFXPointOutputDesc()
            : base(Type.kTypeOutput,"Point Output",true)
        {}
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
    }
}