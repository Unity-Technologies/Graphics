using System;

namespace UnityEditor.VFX
{
    abstract class VFXContextDesc
    {
        [Flags]
        public enum Type
        {
            kTypeNone = 0,

            kTypeInit = 1 << 0,
            kTypeUpdate = 1 << 1,
            kTypeOutput = 1 << 2,

            kInitAndUpdate = kTypeInit | kTypeUpdate,
            kAll = kTypeInit | kTypeUpdate | kTypeOutput,
        };

        public Type ContextType { get { return m_Type; } }
        public string Name      { get { return m_Name; } }

        protected VFXContextDesc(Type type, string name/*, bool showBlock = false*/)
        {
            // type must not be a combination of flags so test if it's a power of two
            if (type == Type.kTypeNone || (type & (type - 1)) != 0)
                throw new ArgumentException("Illegal context type");

            m_Type = type;
            m_Name = name;
            //m_ShowBlock = showBlock;
        }

        private Type m_Type;
        private string m_Name;
        //private bool m_ShowBlock;
    }
}