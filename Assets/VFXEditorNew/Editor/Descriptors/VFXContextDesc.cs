using System;

namespace UnityEditor.VFX
{
    [Flags]
    public enum VFXContextType
    {
        kNone =     0,

        kInit =     1 << 0,
        kUpdate =   1 << 1,
        kOutput =   1 << 2,

        kInitAndUpdate = kInit | kUpdate,
        kAll = kInit | kUpdate | kOutput,
    };

    [Flags]
    public enum VFXDataType
    {
        kNone =     0,
        //kHits =     1 << 0,
        kParticle = 1 << 1,
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
        public VFXDataType InputType        { get { return m_InputType; } }
        public VFXDataType OutputType       { get { return m_OutputType; } }
        public string Name                  { get { return m_Name; } }

        protected VFXContextDesc(VFXContextType type, VFXDataType inputType, VFXDataType outputType, string name, bool showBlock = false)
        {
            // type must not be a combination of flags so test if it's a power of two
            if (type == VFXContextType.kNone || (type & (type - 1)) != 0)
                throw new ArgumentException("Illegal context type");

            m_Type = type;
            m_InputType = inputType;
            m_OutputType = outputType;
            m_Name = name;
            //m_ShowBlock = showBlock;
        }

        protected VFXContextDesc(VFXContextType type, string name, bool showBlock = false)
            : this(type, VFXDataType.kNone, VFXDataType.kNone, name, showBlock)
        {

        }

        private VFXContextType m_Type;

        private VFXDataType m_InputType;
        private VFXDataType m_OutputType;

        private string m_Name;
        //private bool m_ShowBlock;
    }

    [VFXInfo]
    class VFXBasicInitialize : VFXContextDesc
    {
        public VFXBasicInitialize() : base(VFXContextType.kInit, VFXDataType.kNone, VFXDataType.kParticle, "Initialize", false) { }
    }

    [VFXInfo]
    class VFXBasicUpdate : VFXContextDesc
    {
        public VFXBasicUpdate() : base(VFXContextType.kUpdate, VFXDataType.kParticle, VFXDataType.kParticle, "Update", false) { }
    }

    [VFXInfo]
    class VFXBasicOutput : VFXContextDesc
    {
        public VFXBasicOutput() : base(VFXContextType.kOutput, VFXDataType.kParticle, VFXDataType.kNone, "Output", false) { }
        //public override VFXShaderGeneratorModule CreateShaderGenerator(VFXContextModel model) { return new VFXOutputShaderGeneratorModule(); }
    }
}