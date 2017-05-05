using System;
using System.Collections.Generic;
using UnityEngine;

using Type = System.Type;

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

    [Flags]
    public enum VFXDataType
    {
        kNone = 0,
        //kHits =     1 << 0,
        kParticle = 1 << 1,
    };

    class VFXContext : VFXModel<VFXSystem, VFXBlock>
    {
        private VFXContext() {} // Used by serialization

        public VFXContext(VFXContextType contextType, VFXDataType inputType, VFXDataType outputType)
        {
            // type must not be a combination of flags so test if it's a power of two
            if (contextType == VFXContextType.kNone || (contextType & (contextType - 1)) != 0)
                throw new ArgumentException("Illegal context type");

            m_ContextType = contextType;
            m_InputType = inputType;
            m_OutputType = outputType;
        }

        public VFXContext(VFXContextType contextType) : this(contextType, VFXDataType.kNone, VFXDataType.kNone)
        {}

        public virtual VFXContextType contextType   { get { return m_ContextType; } }
        public virtual VFXDataType inputType        { get { return m_InputType; } }
        public virtual VFXDataType outputType       { get { return m_OutputType; } }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            base.OnInvalidate(model, cause);

            if (cause == InvalidationCause.kStructureChanged ||
                cause == InvalidationCause.kConnectionChanged ||
                cause == InvalidationCause.kExpressionInvalidated)
            {
                m_ExpressionContext = new VFXExpression.Context();
                var expressions = new HashSet<VFXExpression>();
                foreach (var child in children)
                {
                    int nbSlots = child.GetNbInputSlots();
                    for (int i = 0; i < nbSlots; ++i)
                    {
                        var slot = child.GetInputSlot(i);
                        slot.GetExpressions(expressions);
                    }
                }

                foreach (var exp in expressions)
                {
                    m_ExpressionContext.RegisterExpression(exp);
                    //Debug.Log("---- Exp: " + exp.GetType() + " " + exp.ValueType);
                }
                m_ExpressionContext.Compile();
                //Debug.Log("************** RECOMPILE EXPRESSION CONTEXT FOR " + this.GetType() + " " + this.name + " nbExpressions:" + expressions.Count);
            }
        }

        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            if (!base.AcceptChild(model, index))
                return false;

            var block = (VFXBlock)model;
            return Accept(block, index);
        }

        public bool Accept(VFXBlock block, int index = -1)
        {
            return (block.compatibleContexts & contextType) != 0;
        }

        // Not serialized nor exposed
        private VFXContextType m_ContextType;
        private VFXDataType m_InputType;
        private VFXDataType m_OutputType;


        [SerializeField]
        private CoordinateSpace m_Space;

        public CoordinateSpace space
        {
            get
            {
                return m_Space;
            }
            set
            {
                m_Space = value;
                Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        public VFXExpression.Context ExpressionContext { get { return m_ExpressionContext; } }
        [NonSerialized]
        private VFXExpression.Context m_ExpressionContext = new VFXExpression.Context();
    }

    // TODO Do that later!
    /* class VFXSubContext : VFXModel<VFXContext, VFXModel>
     {
         // In and out sub context, if null directly connected to the context input/output
         private VFXSubContext m_In;
         private VFXSubContext m_Out;
     }*/
}
