using System;

namespace UnityEditor.VFX
{
    [Flags]
    public enum VFXContextType
    {
        kNone = 0,

        kInit = 1 << 0,
        kUpdate = 1 << 1,
        kOutput = 1 << 2,

        kInitAndUpdate = kInit | kUpdate,
        kAll = kInit | kUpdate | kOutput,
    };

    abstract class VFXContextDesc
    {
        public static VFXContextDesc CreateBasic(VFXContextType type)
        {
            switch (type)
            {
                case VFXContextType.kInit:      return new VFXBasicInitialize();
                case VFXContextType.kUpdate:    return new VFXBasicUpdate();
                case VFXContextType.kOutput:    return new VFXBasicOutput();
            }

            throw new ArgumentException();
        }

        public VFXContextType ContextType   { get { return m_Type; } }
        public string Name                  { get { return m_Name; } }

        protected VFXContextDesc(VFXContextType type, string name, bool showBlock = false)
        {
            // type must not be a combination of flags so test if it's a power of two
            if (type == VFXContextType.kNone || (type & (type - 1)) != 0)
                throw new ArgumentException("Illegal context type");

            m_Type = type;
            m_Name = name;
            //m_ShowBlock = showBlock;
        }

        private VFXContextType m_Type;
        private string m_Name;
        //private bool m_ShowBlock;
    }

    class VFXBasicInitialize : VFXContextDesc
    {
        public VFXBasicInitialize() : base(VFXContextType.kInit, "Initialize", false) { }
    }

    class VFXBasicUpdate : VFXContextDesc
    {
        public VFXBasicUpdate() : base(VFXContextType.kUpdate, "Update", false) { }
    }

    class VFXBasicOutput : VFXContextDesc
    {
        public VFXBasicOutput() : base(VFXContextType.kOutput, "Output", false) { }
        //public override VFXShaderGeneratorModule CreateShaderGenerator(VFXContextModel model) { return new VFXOutputShaderGeneratorModule(); }
    }
}