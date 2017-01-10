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

        public Type ContextType { get { return m_Type; } }
        public string Name      { get { return m_Name; } }

        protected VFXContextDesc(Type type, string name, bool showBlock = false)
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

    class VFXBasicInitialize : VFXContextDesc
    {
        public VFXBasicInitialize() : base(Type.kTypeInit, "Initialize", false) { }
    }

    class VFXBasicUpdate : VFXContextDesc
    {
        public VFXBasicUpdate() : base(Type.kTypeUpdate, "Update", false) { }
    }

    class VFXBasicOutput : VFXContextDesc
    {
        public VFXBasicOutput() : base(Type.kTypeOutput, "Output", false) { }
        //public override VFXShaderGeneratorModule CreateShaderGenerator(VFXContextModel model) { return new VFXOutputShaderGeneratorModule(); }
    }
}