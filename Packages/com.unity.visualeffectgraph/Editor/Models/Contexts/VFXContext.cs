using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.VFX;
using UnityEngine.VFX;

using Type = System.Type;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    [Flags]
    enum VFXContextType
    {
        None = 0,

        Spawner = 1 << 0,
        Init = 1 << 1,
        OutputEvent = 1 << 2,
        Update = 1 << 3,
        Output = 1 << 4,
        Event = 1 << 5,
        SpawnerGPU = 1 << 6,
        Subgraph = 1 << 7,
        Filter = 1 << 8,
        BlockSubgraph = 1 << 9,

        InitAndUpdate = Init | Update,
        InitAndUpdateAndOutput = Init | Update | Output,
        UpdateAndOutput = Update | Output,
        All = Init | Update | Output | Spawner | Subgraph,

        CanHaveBlocks = ~(OutputEvent | Event | SpawnerGPU | Subgraph),
    };

    [Flags]
    enum VFXDataType
    {
        None = 0,
        SpawnEvent = 1 << 0,
        OutputEvent = 1 << 1,
        Particle = 1 << 2,
        Mesh = 1 << 3,
        ParticleStrip = 1 << 4 | Particle, // strips
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

    internal class VFXContext : VFXSlotContainerModel<VFXGraph, VFXBlock>, IVFXDataGetter
    {
        public const int kMaxFlowCount = 10;

        protected static string RenderPipeTemplate(string fileName)
        {
            return VFXLibrary.currentSRPBinder.templatePath + "/Templates/" + fileName;
        }

        [SerializeField]
        private string m_Label;

        public string label
        {
            get { return m_Label; }
            set
            {
                if (m_Label != value)
                {
                    m_Label = value;
                    Invalidate(InvalidationCause.kSettingChanged);
                }
            }
        }

        private VFXContext() { m_UICollapsed = false; } // Used by serialization

        public VFXContext(VFXContextType contextType, VFXDataType inputType, VFXDataType outputType)
        {
            m_ContextType = contextType;
            m_InputType = inputType;
            m_OutputType = outputType;
        }

        public VFXContext(VFXContextType contextType) : this(contextType, VFXDataType.None, VFXDataType.None)
        { }

        // Called by VFXData
        public static T CreateImplicitContext<T>(VFXData data) where T : VFXContext
        {
            var context = ScriptableObject.CreateInstance<T>();
            context.m_Data = data;
            return context;
        }

        public override void OnEnable()
        {
            int nbRemoved = 0;
            if (m_InputFlowSlot == null)
                m_InputFlowSlot = Enumerable.Range(0, inputFlowCount).Select(_ => new VFXContextSlot()).ToArray();
            else
                for (int i = 0; i < m_InputFlowSlot.Length; ++i)
                    nbRemoved += m_InputFlowSlot[i].link.RemoveAll(s => s.context == null);

            if (m_OutputFlowSlot == null)
                m_OutputFlowSlot = Enumerable.Range(0, outputFlowCount).Select(_ => new VFXContextSlot()).ToArray();
            else
                for (int i = 0; i < m_OutputFlowSlot.Length; ++i)
                    nbRemoved += m_OutputFlowSlot[i].link.RemoveAll(s => s.context == null);

            if (nbRemoved > 0)
                Debug.LogWarningFormat("Remove {0} linked context(s) that could not be deserialized from {1} of type {2}", nbRemoved, name, GetType());

            if (m_Data == null)
                SetDefaultData(false);

            m_UICollapsed = false;

            base.OnEnable();
        }

        public virtual bool doesGenerateShader { get { return codeGeneratorTemplate != null; } }
        public virtual string codeGeneratorTemplate { get { return null; } }
        public virtual bool codeGeneratorCompute { get { return true; } }
        public virtual bool doesIncludeCommonCompute { get { return codeGeneratorCompute; } }
        public virtual VFXContextType contextType { get { return m_ContextType; } }

        public virtual VFXContextType compatibleContextType { get { return contextType; } } 
        public virtual VFXDataType inputType { get { return m_InputType; } }
        public virtual VFXDataType outputType { get { return m_OutputType; } }
        public virtual VFXDataType ownedType { get { return contextType == VFXContextType.Output ? inputType : outputType; } }
        public virtual VFXTaskType taskType { get { return VFXTaskType.None; } }
        public virtual IEnumerable<VFXAttributeInfo> attributes { get { return Enumerable.Empty<VFXAttributeInfo>(); } }
        public virtual IEnumerable<VFXMapping> additionalMappings { get { return Enumerable.Empty<VFXMapping>(); } }
        public virtual IEnumerable<string> additionalDataHeaders { get { return GetData().additionalHeaders; } }
        public virtual IEnumerable<string> additionalDefines { get { return Enumerable.Empty<string>(); } }
        public virtual IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionalReplacements { get { return Enumerable.Empty<KeyValuePair<string, VFXShaderWriter>>(); } }
        public virtual IEnumerable<string> fragmentParameters { get { return Enumerable.Empty<string>(); } }
        public virtual bool usesMaterialVariantInEditMode { get { return false; } }

        public virtual VFXContextCompiledData PrepareCompiledData()
        {
            var compiledData = new VFXContextCompiledData
            {
                tasks = new()
                {
                    new VFXTask
                    {
                        doesGenerateShader = doesGenerateShader,
                        templatePath = codeGeneratorTemplate,
                        type = taskType,
                        shaderType = codeGeneratorCompute ? VFXTaskShaderType.ComputeShader : VFXTaskShaderType.Shader,
                    }
                },
                buffers = new()
            };

            return compiledData;
        }

        public virtual bool CanBeCompiled()
        {
            return m_Data != null && m_Data.CanBeCompiled();
        }

        public void MarkAsCompiled(bool compiled)
        {
            hasBeenCompiled = compiled;
        }

        public override void CollectDependencies(HashSet<ScriptableObject> objs, bool ownedOnly = true)
        {
            base.CollectDependencies(objs, ownedOnly);
            if (m_Data != null)
            {
                objs.Add(m_Data);
                m_Data.CollectDependencies(objs, ownedOnly);
            }
        }

        private static bool IsDisabledBlock(VFXModel model)
        {
            VFXBlock block = null;
            if (model is VFXBlock)
                block = (VFXBlock)model;
            else if (model is VFXSlot)
                block = ((VFXSlot)model).owner as VFXBlock;

            return block != null && !block.enabled;
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            base.OnInvalidate(model, cause);

            if (cause == InvalidationCause.kStructureChanged ||
                cause == InvalidationCause.kConnectionChanged ||
                cause == InvalidationCause.kExpressionInvalidated ||
                cause == InvalidationCause.kSettingChanged ||
                cause == InvalidationCause.kEnableChanged)
            {
                if (hasBeenCompiled || CanBeCompiled())
                {
                    bool skip = false;

                    // Check if the invalidation comes from a disable block and in that case don't recompile
                    if (cause != InvalidationCause.kEnableChanged)
                    {
                        skip = IsDisabledBlock(model);
                    }

                    if (!skip)
                        Invalidate(InvalidationCause.kExpressionGraphChanged);
                }
            }

            if (cause == InvalidationCause.kParamChanged ||
                cause == InvalidationCause.kExpressionValueInvalidated)
            {
                if (contextType == VFXContextType.Spawner ||
                    contextType == VFXContextType.Init)
                {
                    bool isBounds = model is VFXSlot slot && (
                        slot.name.Equals(nameof(VFXBasicInitialize.InputPropertiesBounds.bounds)) ||
                        slot.name.Equals(nameof(VFXBasicInitialize.InputPropertiesPadding.boundsPadding))); // Skip bounds so no reinit occurs when authoring the bounds

                    if ((hasBeenCompiled || CanBeCompiled()) && !IsDisabledBlock(model) && !isBounds)
                        Invalidate(InvalidationCause.kInitValueChanged);
                }
            }
        }

        public virtual bool SetupCompilation() { return true; }
        public virtual void EndCompilation() { }


        public void DetachAllInputFlowSlots(bool notify = true)
        {
            //Unlink all existing links. It is up to the user of this method to backup and restore links.
            if (m_InputFlowSlot != null)
            {
                for (int slot = 0; slot < m_InputFlowSlot.Length; slot++)
                {
                    while (m_InputFlowSlot[slot].link.Count > 0)
                    {
                        var clean = m_InputFlowSlot[slot].link.Last();
                        InnerUnlink(clean.context, this, clean.slotIndex, slot, notify);
                    }
                }
            }

            m_InputFlowSlot = Enumerable.Range(0, inputFlowCount).Select(_ => new VFXContextSlot()).ToArray();
        }

        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            if (!base.AcceptChild(model, index))
                return false;

            var block = (VFXBlock)model;
            return Accept(block, index);
        }

        public bool Accept(VFXBlock block, int index = -1) => Accept(block.compatibleContexts, block.compatibleData);
        public bool Accept(VFXContextType blockContexts, VFXDataType blockData) => (blockContexts & compatibleContextType) == compatibleContextType && (blockData & ownedType) != 0;

        public bool CanHaveBlocks()
        {
            return (contextType & VFXContextType.CanHaveBlocks) != 0;
        }

        protected override void OnAdded()
        {
            base.OnAdded();
            if (hasBeenCompiled || CanBeCompiled())
                Invalidate(InvalidationCause.kExpressionGraphChanged);
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (hasBeenCompiled || CanBeCompiled())
                Invalidate(InvalidationCause.kExpressionGraphChanged);
        }

        public static bool CanLink(VFXContext from, VFXContext to, int fromIndex = 0, int toIndex = 0)
        {
            if (from == to || from == null || to == null)
                return false;

            if (from.outputType == VFXDataType.None || to.inputType == VFXDataType.None || (from.outputType & to.inputType) != to.inputType)
                return false;

            if (fromIndex >= from.outputFlowSlot.Length || toIndex >= to.inputFlowSlot.Length)
                return false;

            //If link already present, returns false
            if (from.m_OutputFlowSlot[fromIndex].link.Any(o => o.context == to && o.slotIndex == toIndex) ||
                to.m_InputFlowSlot[toIndex].link.Any(o => o.context == from && o.slotIndex == fromIndex))
                return false;

            //Special incorrect case, GPUEvent use the same type than Spawner which leads to an unexpected allowed link on output & spawner.
            if (from.m_ContextType == VFXContextType.SpawnerGPU && to.m_ContextType != VFXContextType.Init)
                return false;

            //If we want to prevent no mixing of GPUEvent & Spawn Context on Initialize. (allowed but disconnect invalid link)
            /*if (to.m_ContextType == VFXContextType.Init)
            {
                var currentSlot = to.m_InputFlowSlot.SelectMany(o => o.link).Select(o => o.context.m_ContextType).FirstOrDefault();
                if (currentSlot != VFXContextType.None && currentSlot != from.m_ContextType)
                    return false;
            }*/

            //Can't connect directly event to context to OutputEvent
            if (from.m_ContextType == VFXContextType.Event && to.contextType == VFXContextType.OutputEvent)
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
            return contextType == VFXContextType.Spawner
                || contextType == VFXContextType.Event;
        }

        private bool CanLinkFromMany()
        {
            return contextType == VFXContextType.Output
                || contextType == VFXContextType.OutputEvent
                || contextType == VFXContextType.Spawner
                || contextType == VFXContextType.Subgraph
                || contextType == VFXContextType.Init;
        }

        private static bool CanMixingFrom(VFXContextType from, VFXContextType to, VFXContextType lastFavoriteTo)
        {
            if (from == VFXContextType.Init || from == VFXContextType.Update)
            {
                if (lastFavoriteTo == VFXContextType.Update)
                    return to == VFXContextType.Update;
                if (lastFavoriteTo == VFXContextType.Output)
                    return to == VFXContextType.Output;
            }
            //No special case outside init output which can't be mixed with output & update
            return true;
        }

        private static bool CanMixingTo(VFXContextType from, VFXContextType to, VFXContextType lastFavoriteFrom)
        {
            if (to == VFXContextType.Init)
            {
                //Init is exclusive either {event, spawner} xor {spawnerGPU}, not both
                if (lastFavoriteFrom == VFXContextType.Event || lastFavoriteFrom == VFXContextType.Spawner)
                    return from == VFXContextType.Event || from == VFXContextType.Spawner;
                if (lastFavoriteFrom == VFXContextType.SpawnerGPU)
                    return from == VFXContextType.SpawnerGPU;
            }
            else if (to == VFXContextType.Spawner || to == VFXContextType.OutputEvent)
            {
                //No special constraint on spawner or output event (gpuEvent isn't allowed anyway)
                return true;
            }

            //Default case, type transfer aren't expected
            return from == to && to == lastFavoriteFrom;
        }

        protected static void InnerLink(VFXContext from, VFXContext to, int fromIndex, int toIndex, bool notify = true)
        {
            if (!CanLink(from, to, fromIndex, toIndex))
                throw new ArgumentException(string.Format("Cannot link contexts {0} and {1}", from, to));

            // Handle constraints on connections
            foreach (var link in from.m_OutputFlowSlot[fromIndex].link.ToArray())
            {
                if (!link.context.CanLinkFromMany()
                    || !CanMixingFrom(from.contextType, link.context.contextType, to.contextType))
                {
                    if (link.context.inputFlowCount > toIndex) //Special case from SubGraph, not sure how this test could be false
                        InnerUnlink(from, link.context, fromIndex, toIndex, notify);
                }
            }

            foreach (var link in to.m_InputFlowSlot[toIndex].link.ToArray())
            {
                if (!link.context.CanLinkToMany()
                    || !CanMixingTo(link.context.contextType, to.contextType, from.contextType))
                {
                    InnerUnlink(link.context, to, fromIndex, toIndex, notify);
                }
            }

            if ((from.ownedType & to.ownedType) == to.ownedType && from.ownedType.HasFlag(VFXDataType.Particle))
                to.InnerSetData(from.GetData(), false);

            from.m_OutputFlowSlot[fromIndex].link.Add(new VFXContextLink() { context = to, slotIndex = toIndex });
            to.m_InputFlowSlot[toIndex].link.Add(new VFXContextLink() { context = from, slotIndex = fromIndex });

            if (notify)
            {
                from.Invalidate(InvalidationCause.kConnectionChanged);
                to.Invalidate(InvalidationCause.kConnectionChanged);
            }
        }

        private static void InnerUnlink(VFXContext from, VFXContext to, int fromIndex = 0, int toIndex = 0, bool notify = true)
        {
            if (from.GetData() == to.GetData() && from.GetData() != null)
                to.SetDefaultData(false);

            int count = from.m_OutputFlowSlot[fromIndex].link.RemoveAll(o => o.context == to && o.slotIndex == toIndex);
            count += to.m_InputFlowSlot[toIndex].link.RemoveAll(o => o.context == from && o.slotIndex == fromIndex);

            if (count > 0 && notify)
            {
                from.Invalidate(InvalidationCause.kConnectionChanged);
                to.Invalidate(InvalidationCause.kConnectionChanged);
            }
        }

        public VFXContextSlot[] inputFlowSlot { get { return m_InputFlowSlot == null ? new VFXContextSlot[] { } : m_InputFlowSlot; } }
        public VFXContextSlot[] outputFlowSlot { get { return m_OutputFlowSlot == null ? new VFXContextSlot[] { } : m_OutputFlowSlot; } }
        protected virtual int inputFlowCount { get { return 1; } }
        protected virtual int outputFlowCount { get { return 1; } }

        public IEnumerable<VFXContext> inputContexts { get { return m_InputFlowSlot.SelectMany(l => l.link.Select(o => o.context)); } }
        public IEnumerable<VFXContext> outputContexts { get { return m_OutputFlowSlot.SelectMany(l => l.link.Select(o => o.context)); } }

        public virtual VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            if (target == VFXDeviceTarget.GPU)
                return VFXExpressionMapper.FromContext(this);

            return null;
        }

        public void SetDefaultData(bool notify)
        {
            InnerSetData(VFXData.CreateDataType(ownedType), notify);
        }

        public virtual void OnDataChanges(VFXData oldData, VFXData newData)
        {
        }

        private void InnerSetData(VFXData data, bool notify)
        {
            if (m_Data != data)
            {
                if (m_Data != null)
                {
                    m_Data.OnContextRemoved(this);
                }
                OnDataChanges(m_Data, data);
                m_Data = data;

                if (m_Data != null)
                    m_Data.OnContextAdded(this);

                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);

                // Propagate data downwards
                if (ownedType.HasFlag(VFXDataType.Particle)) // Only propagate for particle type atm
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

        public IEnumerable<VFXBlock> activeFlattenedChildrenWithImplicit
        {
            get
            {
                List<VFXBlock> blocks = new List<VFXBlock>();

                foreach (var ctxblk in implicitPreBlock)
                {
                    if (ctxblk is VFXSubgraphBlock subgraphBlk)
                        foreach (var blk in subgraphBlk.recursiveSubBlocks)
                        {
                            if (blk.enabled)
                                blocks.Add(blk);
                        }
                    else
                    {
                        if (ctxblk.enabled)
                            blocks.Add(ctxblk);
                    }
                }

                foreach (var ctxblk in children)
                {
                    if (ctxblk is VFXSubgraphBlock subgraphBlk)
                        foreach (var blk in subgraphBlk.recursiveSubBlocks)
                        {
                            if (blk.enabled)
                                blocks.Add(blk);
                        }
                    else
                    {
                        if (ctxblk.enabled)
                            blocks.Add(ctxblk);
                    }
                }

                foreach (var ctxblk in implicitPostBlock)
                {
                    if (ctxblk is VFXSubgraphBlock subgraphBlk)
                        foreach (var blk in subgraphBlk.recursiveSubBlocks)
                        {
                            if (blk.enabled)
                                blocks.Add(blk);
                        }
                    else
                    {
                        if (ctxblk.enabled)
                            blocks.Add(ctxblk);
                    }
                }
                return blocks;
            }
        }

        private IEnumerable<IVFXSlotContainer> allSlotContainer
        {
            get
            {
                return activeFlattenedChildrenWithImplicit.OfType<IVFXSlotContainer>().Concat(Enumerable.Repeat(this as IVFXSlotContainer, 1));
            }
        }

        public IEnumerable<VFXSlot> allLinkedOutputSlot
        {
            get
            {
                return allSlotContainer.SelectMany(o => o.outputSlots.SelectMany(s => s.LinkedSlots));
            }
        }

        public IEnumerable<VFXSlot> allLinkedInputSlot
        {
            get
            {
                return allSlotContainer.SelectMany(o => o.inputSlots.SelectMany(s => s.LinkedSlots));
            }
        }

        // Not serialized nor exposed
        private VFXContextType m_ContextType;
        private VFXDataType m_InputType;
        private VFXDataType m_OutputType;

        [NonSerialized]
        private bool hasBeenCompiled = false;

        [SerializeField]
        private VFXData m_Data;

        [SerializeField]
        private VFXContextSlot[] m_InputFlowSlot;
        [SerializeField]
        private VFXContextSlot[] m_OutputFlowSlot;

        public char letter { get; set; }

        public override VFXSpace GetOutputSpaceFromSlot(VFXSlot slot)
        {
            return space;
        }

        public virtual bool spaceable
        {
            get
            {
                return m_Data is ISpaceable;
            }
        }

        public VFXSpace space
        {
            get
            {
                if (spaceable)
                {
                    return (m_Data as ISpaceable).space;
                }
                return VFXSpace.None;
            }

            set
            {
                if (spaceable)
                {
                    (m_Data as ISpaceable).space = value;
                    foreach (var owner in m_Data.owners)
                        Invalidate(InvalidationCause.kSettingChanged);

                    var allSlots = m_Data.owners.SelectMany(c => c.inputSlots.Concat(c.activeFlattenedChildrenWithImplicit.SelectMany(o => o.inputSlots)));
                    foreach (var slot in allSlots.Where(s => s.spaceable))
                        slot.Invalidate(InvalidationCause.kSpaceChanged);
                }
            }
        }

        public override void CheckGraphBeforeImport()
        {
            base.CheckGraphBeforeImport();
            // If the graph is reimported it can be because one of its depedency such as the subgraphs, has been changed.
            // blocs could be subgraph blocks.

            foreach (var block in children)
                block.CheckGraphBeforeImport();
        }

        //TODO: Register all the contexts that have issues when transfering settings (in ConvertContext() )
        protected virtual IEnumerable<string> untransferableSettings
        {
            get
            {
                return Enumerable.Empty<string>();
            }
        }

        public bool CanTransferSetting(string settingName)
        {
            return !untransferableSettings.Contains(settingName);
        }

        public bool CanTransferSetting(VFXSetting setting)
        {
            return CanTransferSetting(setting.field.Name);
        }

        public IEnumerable<VFXAttributeInfo> GetAttributesInfos()
        {
            var attributesInfos = Enumerable.Empty<VFXAttributeInfo>();
            attributesInfos = attributesInfos.Concat(attributes);
            foreach (var block in activeFlattenedChildrenWithImplicit)
                attributesInfos = attributesInfos.Concat(block.attributes);
            return attributesInfos;
        }

        public List<VFXData.TaskProfilingData> GetContextTaskIndices()
        {
            return GetData().GetContextTaskIndices(this);
        }

        public List<uint> CreateInstancingSplitValues(VFXExpressionGraph expressionGraph)
        {
            List<uint> instancingSplitValues = new List<uint>();
            foreach (var exp in instancingSplitCPUExpressions)
            {
                int index = expressionGraph.GetFlattenedIndex(exp);
                if (index >= 0)
                {
                    instancingSplitValues.Add((uint)index);
                }
            }
            return instancingSplitValues;
        }

        public virtual IEnumerable<VFXExpression> instancingSplitCPUExpressions { get { return Enumerable.Empty<VFXExpression>(); } }

        protected Dictionary<Type, VFXBlock> implicitBlockCache;

        private void CreateImplicitBlockCacheIfNeeded()
        {
            implicitBlockCache ??= new Dictionary<Type, VFXBlock>();
        }

        protected T GetOrCreateImplicitBlock<T>(VFXData data) where T : VFXBlock
        {
            CreateImplicitBlockCacheIfNeeded();
            if (implicitBlockCache.TryGetValue(typeof(T), out var block))
            {
                T typedBlock = block as T;
                typedBlock.SetTransientData(data);
                return typedBlock;
            }
            else
            {
                var newBlock = VFXBlock.CreateImplicitBlock<T>(data);
                implicitBlockCache.Add(typeof(T), newBlock);
                return newBlock;
            }
        }
    }
}
