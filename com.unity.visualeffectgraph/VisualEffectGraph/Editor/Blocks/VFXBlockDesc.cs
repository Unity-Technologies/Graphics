using UnityEngine;
using UnityEditor;
using System;

namespace UnityEngine.Experimental.VFX
{
    struct VFXAttribute
    {
        public string m_Name;
        public VFXValueType m_Type;
        public bool m_Writable;
    }

    abstract class VFXBlockDesc
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

        public abstract string GetSource();
        public abstract string GetIconPath();
        public abstract string GetName();
        public abstract string GetCategory();
        public abstract VFXProperty[] GetProperties();
        public abstract VFXAttribute[] GetAttributes();

        public Flag GetFlag() { return m_Flag; }

        private bool IsProcessed = false;
        protected Flag m_Flag;
    }

    // Just a wrapper on old blocks coming from C++
    // TODO To be removed
    class VFXBlockLegacy : VFXBlockDesc
    {
        VFXBlockLegacy(VFXBlock block)
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
            for (int i = 0; i < nbProperties; ++i)
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
        }

        public override string GetSource()              { return m_Block.m_Source; }
        public override string GetIconPath()            { return m_Block.m_IconPath; }
        public override string GetName()                { return m_Block.m_Name; }
        public override string GetCategory()            { return m_Block.m_Category; }
        public override VFXProperty[] GetProperties()   { return m_Properties; }
        public override VFXAttribute[] GetAttributes()  { return m_Attributes; } 

        private VFXBlock m_Block;
        private VFXProperty[] m_Properties;
        private VFXAttribute[] m_Attributes;
    }
}
