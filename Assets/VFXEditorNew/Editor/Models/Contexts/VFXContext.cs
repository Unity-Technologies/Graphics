using System;using UnityEngine;using Type = System.Type;namespace UnityEditor.VFX{
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

    [Flags]
    public enum VFXDataType
    {
        kNone = 0,
        //kHits =     1 << 0,
        kParticle = 1 << 1,
    };    class VFXContext : VFXModel<VFXSystem, VFXBlock>    {        private VFXContext() {} // Used by serialization

        public VFXContext(VFXContextType contextType, VFXDataType inputType, VFXDataType outputType)        {
            // type must not be a combination of flags so test if it's a power of two
            if (contextType == VFXContextType.kNone || (contextType & (contextType - 1)) != 0)
                throw new ArgumentException("Illegal context type");

            m_ContextType = contextType;
            m_InputType = inputType;
            m_OutputType = outputType;        }

        public VFXContext(VFXContextType contextType) : this(contextType,VFXDataType.kNone,VFXDataType.kNone)
        {}

        public VFXContextType contextType       { get { return m_ContextType; } }        public VFXDataType inputType            { get { return m_InputType; } }        public VFXDataType outputType           { get { return m_OutputType; } }            public Vector2 position        {            get { return m_UIPosition; }            set { m_UIPosition = value; }        }        public override bool AcceptChild(VFXModel model, int index = -1)        {            if (!base.AcceptChild(model, index))                return false;            var block = (VFXBlock)model;            return Accept(block,index);        }        public bool Accept(VFXBlock block, int index = -1)        {
            return (block.compatibleContexts & contextType) != 0;        }

        [SerializeField]
        private VFXContextType m_ContextType;        [SerializeField]        private VFXDataType m_InputType;

        [SerializeField]        private VFXDataType m_OutputType;         [SerializeField]        private Vector2 m_UIPosition;    }    // TODO Do that later!   /* class VFXSubContext : VFXModel<VFXContext, VFXModel>    {        // In and out sub context, if null directly connected to the context input/output        private VFXSubContext m_In;        private VFXSubContext m_Out;    }*/}