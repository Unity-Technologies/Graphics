using System;
using System.Collections.Generic;
using UnityEngine;

using Type = System.Type;
using Object = UnityEngine.Object;

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
        kNone =         0,
        kSpawnEvent =   1 << 0,
        kParticle =     1 << 1,
    };

    class VFXContext : VFXSlotContainerModel<VFXSystem, VFXBlock>
    {
        private VFXContext() {} // Used by serialization

        public VFXContext(VFXContextType contextType, VFXDataType inputType, VFXDataType outputType)
        {
            m_ContextType = contextType;
            m_InputType = inputType;
            m_OutputType = outputType;
        }

        public VFXContext(VFXContextType contextType) : this(contextType, VFXDataType.kNone, VFXDataType.kNone)
        {}

        public override void OnEnable()
        {
            base.OnEnable();

            // type must not be a combination of flags so test if it's a power of two
            if (m_ContextType == VFXContextType.kNone || (m_ContextType & (m_ContextType - 1)) != 0)
            {
                var invalidContext = m_ContextType;
                m_ContextType = VFXContextType.kNone;
                throw new ArgumentException(string.Format("Illegal context type: {0}", invalidContext));
            }

            if (m_Inputs == null)
                m_Inputs = new List<VFXContext>();

            if (m_Outputs == null)
                m_Outputs = new List<VFXContext>();
        }

        public virtual VFXContextType contextType   { get { return m_ContextType; } }
        public virtual VFXDataType inputType        { get { return m_InputType; } }
        public virtual VFXDataType outputType       { get { return m_OutputType; } }

        public override void CollectDependencies(HashSet<Object> objs)
        {
            base.CollectDependencies(objs);
            if (m_Data != null)
                m_Data.CollectDependencies(objs);
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            base.OnInvalidate(model, cause);

            if (cause == InvalidationCause.kStructureChanged ||
                cause == InvalidationCause.kConnectionChanged ||
                cause == InvalidationCause.kExpressionInvalidated)
            {
                Invalidate(InvalidationCause.kExpressionGraphChanged);
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

        protected override void OnAdded()
        {
            base.OnAdded();
            Invalidate(InvalidationCause.kExpressionGraphChanged);
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            Invalidate(InvalidationCause.kExpressionGraphChanged);
        }

        public void LinkTo(VFXContext context)
        {
            InnerLink(this, context);
        }

        public void LinkFrom(VFXContext context)
        {
            InnerLink(context, this);
        }

        public void Unlink(VFXContext context)
        {
            if (m_Outputs.Contains(context))
                InnerUnlink(this, context);
            if (m_Inputs.Contains(context))
                InnerUnlink(context, this);
        }

        private static void InnerLink(VFXContext from, VFXContext to)
        {
            if (from == to || from == null || to == null)
                throw new ArgumentException("from and to cannot be the same or null");

            if (from.outputType == VFXDataType.kNone || to.inputType == VFXDataType.kNone || from.outputType != to.inputType)
                throw new ArgumentException(string.Format("Incompatible data type between {0} and {1}", from.outputType, to.inputType));

            if (from.m_Outputs.Contains(to) || to.m_Inputs.Contains(from))
                throw new ArgumentException(string.Format("Contexts {0} and {1} are already linked", from, to));

            from.m_Outputs.Add(to);
            to.m_Inputs.Add(from);

            //from.Invalidate(InvalidationCause.kStructureChanged);
            //to.Invalidate(InvalidationCause.kStructureChanged);
        }

        private static void InnerUnlink(VFXContext from, VFXContext to)
        {
            from.m_Outputs.Remove(to);
            to.m_Inputs.Remove(from);

            //from.Invalidate(InvalidationCause.kStructureChanged);
            //to.Invalidate(InvalidationCause.kStructureChanged);
        }

        private void AddExpressionsToContext(HashSet<VFXExpression> expressions, IVFXSlotContainer slotContainer)
        {
            int nbSlots = slotContainer.GetNbInputSlots();
            for (int i = 0; i < nbSlots; ++i)
            {
                var slot = slotContainer.GetInputSlot(i);
                slot.GetExpressions(expressions);
            }
        }

        public virtual VFXExpressionMapper GetGPUExpressions()
        {
            return VFXExpressionMapper.FromContext(this, null, "uniform");
        }

        public virtual VFXExpressionMapper GetCPUExpressions()
        {
            return null;
        }

        public void SetData(VFXData data)
        {
            if (m_Data != data)
            {
                if (m_Data != null)
                    m_Data.OnContextRemoved(this);
                m_Data = data;
                if (m_Data != null)
                    m_Data.OnContextAdded(this);

                Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        public VFXData GetData()
        {
            return m_Data;
        }

        // Not serialized nor exposed
        private VFXContextType m_ContextType;
        private VFXDataType m_InputType;
        private VFXDataType m_OutputType;

        [SerializeField]
        private VFXData m_Data;

        [SerializeField]
        private CoordinateSpace m_Space;

        [SerializeField]
        private List<VFXContext> m_Inputs;
        [SerializeField]
        private List<VFXContext> m_Outputs;

        public CoordinateSpace space
        {
            get
            {
                return m_Space;
            }
            set
            {
                m_Space = value;
                Invalidate(InvalidationCause.kStructureChanged); // TODO This does not seem correct
            }
        }
    }

    // TODO Do that later!
    /* class VFXSubContext : VFXModel<VFXContext, VFXModel>
     {
         // In and out sub context, if null directly connected to the context input/output
         private VFXSubContext m_In;
         private VFXSubContext m_Out;
     }*/
}
