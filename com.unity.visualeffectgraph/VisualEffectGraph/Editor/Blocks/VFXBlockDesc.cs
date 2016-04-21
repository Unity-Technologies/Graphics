using UnityEngine;
using UnityEditor;
using System;

namespace UnityEngine.Experimental.VFX
{
    public struct VFXAttribute
    {
        public VFXAttribute(string name,VFXValueType type,bool writable = false)
        {
            m_Name = name;
            m_Type = type;
            m_Writable = writable;
        }

        public string m_Name;
        public VFXValueType m_Type;
        public bool m_Writable;
    }

    public abstract class VFXBlockDesc
    {
        [Flags]
        public enum Flag
        {
            kNone = 0,
            kHasRand = 1 << 0,
            kHasKill = 1 << 1,
        }

        public void Process()
        {
            if (IsProcessed)
                return;

            // TODO
            // Compute hash and flags
            IsProcessed = true;
        }

        public abstract string Source               { get; }
        public abstract string IconPath             { get; }
        public abstract string Name                 { get; }
        public abstract string Category             { get; }

        public VFXProperty[] Properties     { get { return m_Properties; } }
        public VFXAttribute[] Attributes    { get { return m_Attributes; } }

        public Flag Flags   { get { return m_Flag; }}
        public Hash128 Hash { get { return m_Hash; }}

        private bool IsProcessed = false;
        protected Flag m_Flag;
        protected Hash128 m_Hash;

        protected VFXProperty[] m_Properties;
        protected VFXAttribute[] m_Attributes;
    }

    // Just a wrapper on old blocks coming from C++
    // TODO To be removed
    class VFXBlockLegacy : VFXBlockDesc
    {
        public VFXBlockLegacy(VFXBlock block)
        {
            m_Block = block;
            
            int nbProperties = m_Block.m_Params.Length;   
            m_Properties = new VFXProperty[nbProperties];
            for (int i = 0; i < nbProperties; ++i)
            {
                VFXParam param = m_Block.m_Params[i];
                m_Properties[i] = new VFXProperty(VFXPropertyConverter.CreateSemantics(param.m_Type),param.m_Name);
            }

            int nbAttributes = m_Block.m_Attribs.Length;
            m_Attributes = new VFXAttribute[nbAttributes];
            for (int i = 0; i < nbAttributes; ++i)
            {
                VFXAttrib attrib = m_Block.m_Attribs[i];
                var attribute = new VFXAttribute();
                attribute.m_Name = attrib.m_Param.m_Name;
                attribute.m_Type = VFXPropertyConverter.ConvertType(attrib.m_Param.m_Type);
                attribute.m_Writable = attrib.m_Writable;
                m_Attributes[i] = attribute;
            }

            // Convert flag
            m_Flag = Flag.kNone;
            if ((m_Block.m_Flags & (int)VFXBlock.Flag.kHasRand) != 0)
                m_Flag |= VFXBlockDesc.Flag.kHasRand;
            if ((m_Block.m_Flags & (int)VFXBlock.Flag.kHasKill) != 0)
                m_Flag |= VFXBlockDesc.Flag.kHasKill;

            m_Hash = m_Block.m_Hash;
        }

        public override string Source               { get { return m_Block.m_Source; }}
        public override string IconPath             { get { return m_Block.m_IconPath; }}
        public override string Name                 { get { return m_Block.m_Name; }}
        public override string Category             { get { return m_Block.m_Category; }}

        private VFXBlock m_Block;
    }

    class VFXSpawnOnSphereBlock : VFXBlockDesc
    {
        public VFXSpawnOnSphereBlock()
        {
            m_Properties = new VFXProperty[1] {
                VFXProperty.Create<VFXSphereType>("sphere"),
            };

            m_Attributes = new VFXAttribute[1] {
                new VFXAttribute("position",VFXValueType.kFloat3,true),
            };       

            // TODO this should be derived automatically
            m_Flag = Flag.kHasRand;
            m_Hash = Hash128.Parse("1"); // dummy but must be unique
        }

        public override string Source { get { return @"
float u1 = 2.0 * RAND - 1.0;
float u2 = UNITY_TWO_PI * RAND;
float2 sincosTheta;
sincos(u2,sincosTheta.x,sincosTheta.y);
sincosTheta *= sqrt(1.0 - u1*u1);
position = (float3(sincosTheta,u1) * radius) + center;"; }}

        public override string IconPath             { get { return "Position"; } }
        public override string Name                 { get { return "Set Position (Sphere Surface) Test"; } }
        public override string Category             { get { return "Test/"; } }
    }
}
