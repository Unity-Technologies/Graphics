using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

using Type = System.Type;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    [Flags]
    public enum VFXContextType
    {
        kNone = 0,

        kSpawner = 1 << 0,
        kInit = 1 << 1,
        kUpdate = 1 << 2,
        kOutput = 1 << 3,
        kEvent = 1 << 4,
        kSpawnerGPU = 1 << 5,

        kInitAndUpdate = kInit | kUpdate,
        kInitAndUpdateAndOutput = kInit | kUpdate | kOutput,
        kUpdateAndOutput = kUpdate | kOutput,
        kAll = kInit | kUpdate | kOutput | kSpawner | kSpawnerGPU,
    };

    [Flags]
    public enum VFXDataType
    {
        kNone =         0,
        kSpawnEvent =   1 << 0,
        kParticle =     1 << 1,
        kEvent =        1 << 2
    };

    [Serializable]
    struct VFXContextLink
    {
        public VFXContext context;
        public int slotIndex;
    }

    [Serializable]
    class VFXContextSlot
    {
        public List<VFXContextLink> link = new List<VFXContextLink>();
    }

    class VFXContext : VFXSlotContainerModel<VFXGraph, VFXBlock>
    {
        private VFXContext() { m_UICollapsed = false; } // Used by serialization

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

            if (m_InputFlowSlot == null)
                m_InputFlowSlot = Enumerable.Range(0, inputFlowCount).Select(_ => new VFXContextSlot()).ToArray();

            if (m_OutputFlowSlot == null)
                m_OutputFlowSlot = Enumerable.Range(0, outputFlowCount).Select(_ => new VFXContextSlot()).ToArray();

            if (m_Data == null)
                SetDefaultData(false);

            m_UICollapsed = false;
        }

        public override T Clone<T>()
        {
            var clone = base.Clone<T>() as VFXContext;
            clone.m_InputFlowSlot = Enumerable.Range(0, m_InputFlowSlot.Count()).Select(_ => new VFXContextSlot()).ToArray();
            clone.m_OutputFlowSlot = Enumerable.Range(0, m_OutputFlowSlot.Count()).Select(_ => new VFXContextSlot()).ToArray();
            return clone as T;
        }

        public virtual string codeGeneratorTemplate                     { get { return null; } }
        public virtual bool codeGeneratorCompute                        { get { return true; } }
        public virtual VFXContextType contextType                       { get { return m_ContextType; } }
        public virtual VFXDataType inputType                            { get { return m_InputType; } }
        public virtual VFXDataType outputType                           { get { return m_OutputType; } }
        public virtual VFXDataType ownedType                            { get { return contextType == VFXContextType.kOutput ? inputType : outputType; } }
        public virtual VFXTaskType taskType                             { get { return VFXTaskType.kNone; } }
        public virtual IEnumerable<VFXAttributeInfo> attributes         { get { return Enumerable.Empty<VFXAttributeInfo>(); } }
        public virtual IEnumerable<VFXAttributeInfo> optionalAttributes { get { return Enumerable.Empty<VFXAttributeInfo>(); } }
        public virtual IEnumerable<VFXMapping> additionalMappings       { get { return Enumerable.Empty<VFXMapping>(); } }
        public virtual IEnumerable<string> additionalDefines            { get { return Enumerable.Empty<string>(); } }
        public virtual string renderLoopCommonInclude                   { get { throw new NotImplementedException(); } }
        public virtual IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionnalReplacements { get { return Enumerable.Empty<KeyValuePair<string, VFXShaderWriter>>(); } }

        public virtual bool CanBeCompiled()
        {
            return m_Data != null && m_Data.CanBeCompiled();
        }

        public override void CollectDependencies(HashSet<Object> objs)
        {
            base.CollectDependencies(objs);
            if (m_Data != null)
            {
                objs.Add(m_Data);
                m_Data.CollectDependencies(objs);
            }
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            base.OnInvalidate(model, cause);

            if (cause == InvalidationCause.kStructureChanged ||
                cause == InvalidationCause.kConnectionChanged ||
                cause == InvalidationCause.kExpressionInvalidated ||
                cause == InvalidationCause.kSettingChanged)
            {
                if (CanBeCompiled())
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
            var testedType = contextType == VFXContextType.kOutput ? inputType : outputType;
            return ((block.compatibleContexts & contextType) != 0) && ((block.compatibleData & testedType) != 0);
        }

        protected override void OnAdded()
        {
            base.OnAdded();
            if (CanBeCompiled())
                Invalidate(InvalidationCause.kExpressionGraphChanged);
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (CanBeCompiled())
                Invalidate(InvalidationCause.kExpressionGraphChanged);
        }

        public static bool CanLink(VFXContext from, VFXContext to, int fromIndex = 0, int toIndex = 0)
        {
            if (from == to || from == null || to == null)
                return false;

            if (from.outputType == VFXDataType.kNone || to.inputType == VFXDataType.kNone || from.outputType != to.inputType)
                return false;

            if (fromIndex >= from.outputFlowSlot.Length || toIndex >= to.inputFlowSlot.Length)
                return false;

            if (from.m_OutputFlowSlot[fromIndex].link.Any(o => o.context == to) || to.m_InputFlowSlot[toIndex].link.Any(o => o.context == from))
                return false;

            return true;
        }

        public void LinkTo(VFXContext context, int fromIndex = 0, int toIndex = 0)
        {
            InnerLink(this, context, fromIndex, toIndex);
        }

        public void LinkFrom(VFXContext context, int fromIndex = 0, int toIndex = 0)
        {
            InnerLink(context, this, fromIndex, toIndex);
        }

        public void UnlinkTo(VFXContext context, int fromIndex = 0, int toIndex = 0)
        {
            InnerUnlink(this, context, fromIndex, toIndex);
        }

        public void UnlinkFrom(VFXContext context, int fromIndex = 0, int toIndex = 0)
        {
            InnerUnlink(context, this, fromIndex, toIndex);
        }

        public void UnlinkAll()
        {
            for (int slot = 0; slot < m_OutputFlowSlot.Length; slot++)
            {
                while (m_OutputFlowSlot[slot].link.Count > 0)
                {
                    var clean = m_OutputFlowSlot[slot].link.Last();
                    InnerUnlink(this, clean.context, slot, clean.slotIndex);
                }
            }

            for (int slot = 0; slot < m_InputFlowSlot.Length; slot++)
            {
                while (m_InputFlowSlot[slot].link.Count > 0)
                {
                    var clean = m_InputFlowSlot[slot].link.Last();
                    InnerUnlink(clean.context, this, clean.slotIndex, slot);
                }
            }
        }

        private bool CanLinkToMany()
        {
            return contextType == VFXContextType.kSpawner
                || contextType == VFXContextType.kEvent;
        }

        private bool CanLinkFromMany()
        {
            return contextType == VFXContextType.kOutput
                ||  contextType == VFXContextType.kSpawner
                ||  contextType == VFXContextType.kInit;
        }

        private static void InnerLink(VFXContext from, VFXContext to, int fromIndex, int toIndex, bool notify = true)
        {
            if (!CanLink(from, to, fromIndex, toIndex))
                throw new ArgumentException(string.Format("Cannot link contexts {0} and {1}", from, to));

            // Handle constraints on connections
            foreach (var link in from.m_OutputFlowSlot[fromIndex].link.ToArray())
            {
                if (!link.context.CanLinkFromMany() || link.context.contextType != to.contextType)
                {
                    InnerUnlink(from, link.context, fromIndex, toIndex, notify);
                }
            }

            foreach (var link in to.m_InputFlowSlot[toIndex].link.ToArray())
            {
                if (!link.context.CanLinkToMany() || link.context.contextType != from.contextType)
                {
                    InnerUnlink(link.context, to, fromIndex, toIndex, notify);
                }
            }

            if (from.ownedType == to.ownedType)
                to.InnerSetData(from.GetData(), false);

            from.m_OutputFlowSlot[fromIndex].link.Add(new VFXContextLink() { context = to, slotIndex = toIndex });
            to.m_InputFlowSlot[toIndex].link.Add(new VFXContextLink() { context = from, slotIndex = fromIndex });

            if (notify)
            {
                // TODO Might need a specific event ?
                from.Invalidate(InvalidationCause.kStructureChanged);
                to.Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        private static void InnerUnlink(VFXContext from, VFXContext to, int fromIndex = 0, int toIndex = 0, bool notify = true)
        {
            // We need to force recompilation of contexts that where compilable before unlink and might not be after
            if (from.CanBeCompiled())
                from.Invalidate(InvalidationCause.kExpressionGraphChanged);
            if (to.CanBeCompiled())
                to.Invalidate(InvalidationCause.kExpressionGraphChanged);

            if (from.ownedType == to.ownedType)
                to.SetDefaultData(false);

            from.m_OutputFlowSlot[fromIndex].link.RemoveAll(o => o.context == to && o.slotIndex == toIndex);
            to.m_InputFlowSlot[toIndex].link.RemoveAll(o => o.context == from && o.slotIndex == fromIndex);

            // TODO Might need a specific event ?
            if (notify)
            {
                from.Invalidate(InvalidationCause.kStructureChanged);
                to.Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        public VFXContextSlot[] inputFlowSlot { get { return m_InputFlowSlot == null ? new VFXContextSlot[] {} : m_InputFlowSlot; } }
        public VFXContextSlot[] outputFlowSlot { get { return m_OutputFlowSlot == null ? new VFXContextSlot[] {} : m_OutputFlowSlot; } }
        protected virtual int inputFlowCount { get { return 1; } }
        protected virtual int outputFlowCount { get { return 1; } }

        public IEnumerable<VFXContext> inputContexts    { get { return m_InputFlowSlot.SelectMany(l => l.link.Select(o => o.context)); } }
        public IEnumerable<VFXContext> outputContexts   { get { return m_OutputFlowSlot.SelectMany(l => l.link.Select(o => o.context)); } }

        public virtual VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            if (target == VFXDeviceTarget.GPU)
                return VFXExpressionMapper.FromContext(this);

            return null;
        }

        private void SetDefaultData(bool notify)
        {
            InnerSetData(VFXData.CreateDataType(ownedType), notify);
        }

        private void InnerSetData(VFXData data, bool notify)
        {
            if (m_Data != data)
            {
                if (m_Data != null)
                {
                    m_Data.OnContextRemoved(this);
                    if (m_Data.owners.Count() == 0)
                        m_Data.Detach();
                }
                m_Data = data;
                if (m_Data != null)
                    m_Data.OnContextAdded(this);

                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);

                // Propagate data downwards
                foreach (var output in m_OutputFlowSlot.SelectMany(o => o.link.Select(l => l.context)))
                    if (output.ownedType == ownedType)
                        output.InnerSetData(data, notify);
            }
        }

        public VFXData GetData()
        {
            return m_Data;
        }

        protected virtual IEnumerable<VFXBlock> implicitPreBlock
        {
            get
            {
                return Enumerable.Empty<VFXBlock>();
            }
        }

        protected virtual IEnumerable<VFXBlock> implicitPostBlock
        {
            get
            {
                return Enumerable.Empty<VFXBlock>();
            }
        }

        public IEnumerable<VFXBlock> activeChildrenWithImplicit
        {
            get
            {
                return implicitPreBlock.Concat(children).Concat(implicitPostBlock).Where(o => o.enabled);
            }
        }

        public IEnumerable<VFXSlot> allLinkedOutputSlot
        {
            get
            {
                var linkedOutCount =    activeChildrenWithImplicit.OfType<IVFXSlotContainer>()
                    .Concat(Enumerable.Repeat(this as IVFXSlotContainer, 1))
                    .SelectMany(o => o.outputSlots.SelectMany(s => s.LinkedSlots));
                return linkedOutCount;
            }
        }

        public static List<KeyValuePair<VFXContext, VFXContext>> BuildAssociativeContext(VFXModel[] fromArray, VFXModel[] toArray)
        {
            var associativeContext = new List<KeyValuePair<VFXContext, VFXContext>>();
            for (int i = 0; i < fromArray.Length; ++i)
            {
                var from = fromArray[i] as VFXContext;
                var to = toArray[i] as VFXContext;
                if (from != null)
                {
                    if (to == null)
                        throw new NullReferenceException("BuildAssociativeContext : Inconsistent hierarchy");
                    associativeContext.Add(new KeyValuePair<VFXContext, VFXContext>(from, to));
                }
            }
            return associativeContext;
        }

        public static IEnumerable<VFXData> ReproduceDataSettings(List<KeyValuePair<VFXContext, VFXContext>> associativeContext)
        {
            var allDstData = associativeContext.Select(o => o.Value.GetData()).Where(o => o != null).Distinct();

            foreach (var data in allDstData)
            {
                var srcData = associativeContext.First(c => c.Value == data.owners.First()).Key.GetData();
                srcData.CopySettings(data);
            }

            return allDstData;
        }

        public static void ReproduceLinkedFlowFromHiearchy(List<KeyValuePair<VFXContext, VFXContext>> associativeContext)
        {
            for (int i = 0; i < associativeContext.Count(); ++i)
            {
                var from = associativeContext[i].Key;
                var to = associativeContext[i].Value;
                for (int slotIndex = 0; slotIndex < from.m_InputFlowSlot.Length; ++slotIndex)
                {
                    var slotInputFrom = from.m_InputFlowSlot[slotIndex];
                    foreach (var link in slotInputFrom.link)
                    {
                        var refContext = associativeContext.FirstOrDefault(o => o.Key == link.context);
                        if (refContext.Value == null)
                            throw new NullReferenceException("ReproduceLinkedFlowFromHiearchy : Unable to retrieve reference for " + link);
                        InnerLink(refContext.Value, to, link.slotIndex, slotIndex, false);
                    }
                }
            }
        }

        // Not serialized nor exposed
        private VFXContextType m_ContextType;
        private VFXDataType m_InputType;
        private VFXDataType m_OutputType;

        [SerializeField]
        private VFXData m_Data;

        [SerializeField]
        private VFXContextSlot[] m_InputFlowSlot;
        [SerializeField]
        private VFXContextSlot[] m_OutputFlowSlot;

        [NonSerialized]
        private bool m_Compiled = false;

        public CoordinateSpace space
        {
            get
            {
                if (m_Data is ISpaceable)
                {
                    return (m_Data as ISpaceable).space;
                }
                return CoordinateSpace.Local;
            }
            set
            {
                if (m_Data is ISpaceable)
                {
                    (m_Data as ISpaceable).space = value;
                    Invalidate(InvalidationCause.kSettingChanged);
                }
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
