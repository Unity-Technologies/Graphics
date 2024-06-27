using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using static UnityEditor.VFX.VFXSortingUtility;

namespace UnityEditor.VFX
{
    interface ILayoutProvider
    {
        void GenerateAttributeLayout(uint capacity, Dictionary<VFXAttribute, int> storedAttribute);
        string GetCodeOffset(VFXAttribute attrib, uint capacity, string index, string instanceIndex);
        uint GetBufferSize(uint capacity);

        VFXGPUBufferDesc GetBufferDesc(uint capacity);
    }

    class StructureOfArrayProvider : ILayoutProvider
    {
        private struct AttributeLayout
        {
            public int bucket;
            public int offset;

            public AttributeLayout(int bucket, int offset)
            {
                this.bucket = bucket;
                this.offset = offset;
            }
        }

        // return size
        private int GenerateBucketLayout(List<VFXAttribute> attributes, int bucketId)
        {
            var sortedAttrib = attributes.OrderByDescending(a => VFXValue.TypeToSize(a.type));

            var attribBlocks = new List<List<VFXAttribute>>();
            foreach (var value in sortedAttrib)
            {
                var block = attribBlocks.FirstOrDefault(b => b.Sum(a => VFXValue.TypeToSize(a.type)) + VFXValue.TypeToSize(value.type) <= 4);
                if (block != null)
                    block.Add(value);
                else
                    attribBlocks.Add(new List<VFXAttribute>() { value });
            }

            int currentOffset = 0;
            int minAlignment = 0;
            foreach (var block in attribBlocks)
            {
                foreach (var attrib in block)
                {
                    int size = VFXValue.TypeToSize(attrib.type);
                    int alignment = size > 2 ? 4 : size;
                    minAlignment = Math.Max(alignment, minAlignment);
                    // align offset
                    currentOffset = (currentOffset + alignment - 1) & ~(alignment - 1);
                    m_AttributeLayout.Add(attrib, new AttributeLayout(bucketId, currentOffset));
                    currentOffset += size;
                }
            }

            return (currentOffset + minAlignment - 1) & ~(minAlignment - 1);
        }

        public void GenerateAttributeLayout(uint capacity, Dictionary<VFXAttribute, int> storedAttribute)
        {
            m_BucketSizes.Clear();
            m_AttributeLayout.Clear();
            m_BucketOffsets.Clear();

            var attributeBuckets = new Dictionary<int, List<VFXAttribute>>();
            foreach (var kvp in storedAttribute)
            {
                List<VFXAttribute> attributes;
                if (!attributeBuckets.ContainsKey(kvp.Value))
                {
                    attributes = new List<VFXAttribute>();
                    attributeBuckets[kvp.Value] = attributes;
                }
                else
                    attributes = attributeBuckets[kvp.Value];

                attributes.Add(kvp.Key);
            }

            int bucketId = 0;
            foreach (var bucket in attributeBuckets)
            {
                int bucketOffset = bucketId == 0 ? 0 : m_BucketOffsets[bucketId - 1] + (int)capacity * m_BucketSizes[bucketId - 1];
                m_BucketOffsets.Add((bucketOffset + 3) & ~3); // align on dword;
                m_BucketSizes.Add(GenerateBucketLayout(bucket.Value, bucketId));
                ++bucketId;
            }

            // Debug log
            if (VFXViewPreference.advancedLogs)
            {
                var builder = new StringBuilder();
                builder.AppendLine("ATTRIBUTE LAYOUT");
                builder.Append(string.Format("NbBuckets:{0} ( ", m_BucketSizes.Count));
                foreach (int size in m_BucketSizes)
                    builder.Append(size + " ");
                builder.AppendLine(")");
                foreach (var kvp in m_AttributeLayout)
                    builder.AppendLine(string.Format("Attrib:{0} type:{1} bucket:{2} offset:{3}", kvp.Key.name, kvp.Key.type, kvp.Value.bucket, kvp.Value.offset));
                Debug.Log(builder.ToString());
            }
        }

        public string GetCodeOffset(VFXAttribute attrib, uint capacity, string index, string instanceIndex)
        {
            AttributeLayout layout;
            if (!m_AttributeLayout.TryGetValue(attrib, out layout))
            {
                throw new InvalidOperationException(string.Format("Cannot find attribute {0}", attrib.name));
            }
            return string.Format("(({3} * 0x{4:X}) + ({2} * 0x{0:X} + 0x{1:X})) << 2", m_BucketSizes[layout.bucket], m_BucketOffsets[layout.bucket] + layout.offset, index, instanceIndex, GetBufferSize(capacity));
        }

        public string GetCodeOffset(VFXAttribute attrib, string index, string eventIndex)
        {
            AttributeLayout layout;
            if (!m_AttributeLayout.TryGetValue(attrib, out layout))
            {
                throw new InvalidOperationException(string.Format("Cannot find attribute {0}", attrib.name));
            }
            return string.Format("(({3} * 0x{4:X}) + ({2} * 0x{0:X} + 0x{1:X})) << 2", m_BucketSizes[layout.bucket], m_BucketOffsets[layout.bucket] + layout.offset, index, eventIndex, (uint)m_BucketSizes.LastOrDefault());
        }

        public uint GetBufferSize(uint capacity)
        {
            return (uint)m_BucketOffsets.LastOrDefault() + capacity * (uint)m_BucketSizes.LastOrDefault();
        }

        public VFXGPUBufferDesc GetBufferDesc(uint capacity)
        {
            var layout = m_AttributeLayout.Select(o => new VFXLayoutElementDesc()
            {
                name = o.Key.name,
                type = o.Key.type,
                offset = new VFXLayoutOffset()
                {
                    structure = (uint)m_BucketSizes[o.Value.bucket],
                    bucket = (uint)m_BucketOffsets[o.Value.bucket],
                    element = (uint)o.Value.offset
                }
            });
            return new VFXGPUBufferDesc()
            {
                type = ComputeBufferType.Raw,
                size = GetBufferSize(capacity),
                stride = 4,
                capacity = capacity,
                layout = layout.ToArray()
            };
        }

        public struct BucketInfo
        {
            public int size;
            public int usedSize;
            public VFXAttribute[] attributes;
            public int[] channels;
        }

        public BucketInfo[] GetBucketLayoutInfo()
        {
            int count = m_BucketSizes.Count;
            BucketInfo[] buckets = new BucketInfo[count];
            for (int i = 0; i < count; i++)
            {
                int size = m_BucketSizes[i];
                buckets[i].size = size;
                buckets[i].usedSize = 0;
                buckets[i].attributes = new VFXAttribute[size];
                buckets[i].channels = new int[size];
            }

            foreach (var kvp in m_AttributeLayout)
            {
                var attrib = kvp.Key;
                int size = VFXValue.TypeToSize(attrib.type);
                int offset = kvp.Value.offset;
                for (int i = 0; i < size; i++)
                {
                    buckets[kvp.Value.bucket].attributes[i + offset] = attrib;
                    buckets[kvp.Value.bucket].channels[i + offset] = i;
                    buckets[kvp.Value.bucket].usedSize = Math.Max(buckets[kvp.Value.bucket].usedSize, i + offset + 1);
                }
            }

            return buckets;
        }

        private Dictionary<VFXAttribute, AttributeLayout> m_AttributeLayout = new Dictionary<VFXAttribute, AttributeLayout>();
        private List<int> m_BucketSizes = new List<int>();
        private List<int> m_BucketOffsets = new List<int>();
    }

    internal enum BoundsSettingMode
    {
        Recorded,
        Manual,
        Automatic,
    }

    class VFXDataParticle : VFXData, ISpaceable
    {
        public VFXDataParticle()
        {
            m_GraphValuesLayout.uniformBlocks = new List<List<VFXExpression>>();
        }

        public override VFXDataType type { get { return hasStrip ? VFXDataType.ParticleStrip : VFXDataType.Particle; } }

        internal enum DataType
        {
            Particle,
            ParticleStrip
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected DataType dataType = DataType.Particle;
        [VFXSetting, Delayed, SerializeField, FormerlySerializedAs("m_Capacity")]
        [Tooltip("Sets the maximum particle capacity of this system. Particles spawned after the capacity has been reached are discarded.")]
        protected uint capacity = 128;
        [VFXSetting, Delayed, SerializeField]
        protected uint stripCapacity = 1;
        [VFXSetting, Delayed, SerializeField]
        protected uint particlePerStripCount = 128;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), SerializeField]
        protected bool needsComputeBounds = false;

        public bool NeedsComputeBounds() => needsComputeBounds;

        public bool NeedsComputeBounds(VFXContext context)
        {
            return needsComputeBounds && context == m_Owners.Where(ctx => ctx is VFXBasicUpdate).Last();
        }

        [FormerlySerializedAs("boundsSettingMode")]
        [VFXSetting(VFXSettingAttribute.VisibleFlags.All),
         Tooltip("Specifies how the bounds are set. They can be set manually, recorded in the Target GameObject window, or computed automatically at a small performance cost."),
         SerializeField]
        public BoundsSettingMode boundsMode = BoundsSettingMode.Recorded;

        public bool hasStrip { get { return dataType == DataType.ParticleStrip; } }

        public override void OnSettingModified(VFXSetting setting)
        {
            base.OnSettingModified(setting);

            if (setting.name == "capacity" && capacity == 0)
                capacity = 1;
            else if (setting.name == "stripCapacity" && stripCapacity == 0)
                stripCapacity = 1;
            else if (setting.name == "particlePerStripCount" && particlePerStripCount == 0)
                particlePerStripCount = 1;
            else if (setting.name == "boundsMode")
            {
                //Refresh errors on Output contexts
                var allSystemOutputContexts = owners.Where(ctx => ctx is VFXAbstractParticleOutput);
                foreach (var ctx in allSystemOutputContexts)
                {
                    ctx.RefreshErrors();
                }

                if (boundsMode == BoundsSettingMode.Automatic)
                {
                    needsComputeBounds = true;
                    var graph = GetGraph();
                    graph.visualEffectResource.cullingFlags = VFXCullingFlags.CullNone;
                }
                else
                {
                    needsComputeBounds = false;
                }
            }
            if (hasStrip)
            {
                if (setting.name == "dataType") // strip has just been set
                {
                    stripCapacity = 1;
                    particlePerStripCount = capacity;
                }
                capacity = stripCapacity * particlePerStripCount;
            }
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            base.OnInvalidate(model, cause);
            if (cause == InvalidationCause.kSettingChanged)
                UpdateValidOutputs();
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var s in base.filteredOutSettings)
                    yield return s;

                if (!VFXViewPreference.displayExperimentalOperator) // TODO Name is bad!
                    yield return "dataType";

                if (hasStrip)
                {
                    yield return "capacity";
                }
                else
                {
                    yield return "stripCapacity";
                    yield return "particlePerStripCount";
                }
            }
        }

        public override IEnumerable<string> additionalHeaders
        {
            get
            {
                if (hasStrip)
                {
                    yield return "#define STRIP_COUNT " + stripCapacity + "u";
                    yield return "#define PARTICLE_PER_STRIP_COUNT " + particlePerStripCount + "u";
                }
                yield return "#define RAW_CAPACITY " + capacity + "u";
            }
        }

        private void UpdateValidOutputs()
        {
            var toUnlink = new List<VFXContext>();

            foreach (var context in owners)
                if (context.contextType == VFXContextType.Output) // Consider only outputs
                {
                    var input = context.inputContexts.FirstOrDefault(); // Consider only one input at the moment because this is ensure by the data type (even if it may change in the future)
                    if (input != null && (input.outputType & context.inputType) != context.inputType)
                        toUnlink.Add(context);
                }

            foreach (var context in toUnlink)
                context.UnlinkFrom(context.inputContexts.FirstOrDefault());
        }

        public uint alignedCapacity
        {
            get
            {
                uint paddedCapacity = capacity;
                const uint kThreadPerGroup = 64;
                if (paddedCapacity > kThreadPerGroup)
                    paddedCapacity = (uint)((paddedCapacity + kThreadPerGroup - 1) & ~(kThreadPerGroup - 1)); // multiple of kThreadPerGroup

                return (paddedCapacity + 3u) & ~3u; // Align on 4 boundary
            }
        }

        public uint ComputeSourceCount(Dictionary<VFXContext, List<VFXContextLink>[]> effectiveFlowInputLinks)
        {
            var init = owners.FirstOrDefault(o => o.contextType == VFXContextType.Init);

            if (init == null)
                return 0u;

            var cpuCount = effectiveFlowInputLinks[init].SelectMany(t => t.Select(u => u.context)).Where(o => o.contextType == VFXContextType.Spawner).Count();
            var gpuCount = effectiveFlowInputLinks[init].SelectMany(t => t.Select(u => u.context)).Where(o => o.contextType == VFXContextType.SpawnerGPU).Count();

            if (cpuCount != 0 && gpuCount != 0)
            {
                throw new InvalidOperationException("Cannot mix GPU & CPU spawners in init");
            }

            if (cpuCount > 0)
            {
                return (uint)cpuCount;
            }
            else if (gpuCount > 0)
            {
                if (gpuCount > 1)
                {
                    throw new InvalidOperationException("Don't support multiple GPU event (for now)");
                }
                var parent = m_DependenciesIn.OfType<VFXDataParticle>().FirstOrDefault();
                return parent != null ? parent.capacity : 0u;
            }
            return init != null ? (uint)effectiveFlowInputLinks[init].SelectMany(t => t.Select(u => u.context)).Where(o => o.contextType == VFXContextType.Spawner /* Explicitly ignore spawner gpu */).Count() : 0u;
        }

        public uint attributeBufferSize
        {
            get
            {
                return m_layoutAttributeCurrent.GetBufferSize(alignedCapacity);
            }
        }

        public VFXGPUBufferDesc attributeBufferDesc
        {
            get
            {
                return m_layoutAttributeCurrent.GetBufferDesc(alignedCapacity);
            }
        }

        public VFXCoordinateSpace space
        {
            get { return m_Space; }
            set { m_Space = value; Modified(false); }
        }

        public override bool CanBeCompiled()
        {
            // Has enough contexts and capacity
            if (m_Owners.Count < 1 || capacity <= 0)
                return false;

            // Has a initialize
            if (m_Owners[0].contextType != VFXContextType.Init)
                return false;

            // Has a spawner
            if (m_Owners[0].inputContexts.FirstOrDefault() == null)
                return false;

            // Has an output
            if (m_Owners.Last().contextType == VFXContextType.Output)
                return true;

            // Has a least one dependent compilable system
            if (m_Owners.SelectMany(c => c.allLinkedOutputSlot)
                .Select(s => ((VFXModel)s.owner).GetFirstOfType<VFXContext>())
                .Any(c => c.CanBeCompiled()))
                return true;

            return false;
        }

        public override VFXDeviceTarget GetCompilationTarget(VFXContext context)
        {
            return VFXDeviceTarget.GPU;
        }

        uint m_SourceCount = 0xFFFFFFFFu;

        public override uint staticSourceCount
        {
            get
            {
                return m_SourceCount;
            }
        }

        public bool hasDynamicSourceCount
        {
            get
            {
                return m_Contexts.Any(
                    o => o.contextType == VFXContextType.Init
                    && o.inputFlowSlot.Any(flow => flow.link.Any(link => link.context.contextType == VFXContextType.Event)));
            }
        }

        public override void GenerateAttributeLayout(Dictionary<VFXContext, List<VFXContextLink>[]> effectiveFlowInputLinks)
        {
            m_layoutAttributeCurrent.GenerateAttributeLayout(alignedCapacity, m_StoredCurrentAttributes);
            m_SourceCount = ComputeSourceCount(effectiveFlowInputLinks);

            var parent = m_DependenciesIn.OfType<VFXDataParticle>().FirstOrDefault();
            if (parent != null)
            {
                m_layoutAttributeSource.GenerateAttributeLayout(parent.alignedCapacity, parent.m_StoredCurrentAttributes);
                m_ownAttributeSourceBuffer = false;
            }
            else
            {
                var readSourceAttribute = m_ReadSourceAttributes.ToDictionary(o => o, _ => (int)VFXAttributeMode.ReadSource);
                m_layoutAttributeSource.GenerateAttributeLayout(m_SourceCount, readSourceAttribute);
                m_ownAttributeSourceBuffer = true;
            }
        }

        public override string GetAttributeDataDeclaration(VFXAttributeMode mode)
        {
            if (m_StoredCurrentAttributes.Count == 0)
                return string.Empty;
            else if ((mode & VFXAttributeMode.Write) != 0)
                return "RWByteAddressBuffer attributeData;";
            else
                return "ByteAddressBuffer attributeData;";
        }

        private string GetCastAttributePrefix(VFXAttribute attrib)
        {
            if (VFXExpression.IsFloatValueType(attrib.type))
                return "asfloat";
            return "";
        }

        private string GetByteAddressBufferMethodSuffix(VFXAttribute attrib)
        {
            int size = VFXExpression.TypeToSize(attrib.type);
            if (size == 1)
                return string.Empty;
            else if (size <= 4)
                return size.ToString();
            else
                throw new ArgumentException(string.Format("Attribute {0} of type {1} cannot be handled in ByteAddressBuffer due to its size of {2}", attrib.name, attrib.type, size));
        }

        public override string GetLoadAttributeCode(VFXAttribute attrib, VFXAttributeLocation location)
        {
            var attributeStore = location == VFXAttributeLocation.Current ? m_layoutAttributeCurrent : m_layoutAttributeSource;
            var attributeBuffer = location == VFXAttributeLocation.Current ? "attributeBuffer" : "sourceAttributeBuffer";
            var parent = m_DependenciesIn.OfType<VFXDataParticle>().FirstOrDefault();

            uint attributeCapacity;
            if (location == VFXAttributeLocation.Current)
                attributeCapacity = alignedCapacity;
            else
                attributeCapacity = (parent != null) ? parent.capacity : staticSourceCount;

            var index = location == VFXAttributeLocation.Current ? "index" : "sourceIndex";

            if (location == VFXAttributeLocation.Current && !m_StoredCurrentAttributes.ContainsKey(attrib))
                throw new ArgumentException(string.Format("Attribute {0} does not exist in data layout", attrib.name));

            if (location == VFXAttributeLocation.Source && !m_ReadSourceAttributes.Any(a => a.name == attrib.name))
                throw new ArgumentException(string.Format("Attribute {0} does not exist in data layout", attrib.name));

            string codeOffset = location == VFXAttributeLocation.Current
                ? attributeStore.GetCodeOffset(attrib, attributeCapacity, index, "instanceIndex")
                : attributeStore.GetCodeOffset(attrib, index, "startEventIndex");
            return string.Format("{0}({3}.Load{1}({2}))", GetCastAttributePrefix(attrib), GetByteAddressBufferMethodSuffix(attrib), codeOffset, attributeBuffer);
        }

        public override string GetStoreAttributeCode(VFXAttribute attrib, string value)
        {
            if (!m_StoredCurrentAttributes.ContainsKey(attrib))
                throw new ArgumentException(string.Format("Attribute {0} does not exist in data layout", attrib.name));

            return string.Format("attributeBuffer.Store{0}({1},{3}({2}))",
                GetByteAddressBufferMethodSuffix(attrib),
                m_layoutAttributeCurrent.GetCodeOffset(attrib, alignedCapacity, "index", "instanceIndex"),
                value, attrib.type == VFXValueType.Boolean ? "uint" : "asuint");
        }

        public override IEnumerable<VFXContext> InitImplicitContexts()
        {
            var contexts = compilableOwners.ToList();

            m_Contexts = new List<VFXContext>(contexts.Count + 2); // Allocate max number
            int index = 0;

            bool hasMainUpdate = false;
            // First add init and updates
            for (index = 0; index < contexts.Count; ++index)
            {
                if (contexts[index].contextType == VFXContextType.Update)
                    hasMainUpdate = true;
                if ((contexts[index].contextType == VFXContextType.Output))
                    break;
                m_Contexts.Add(contexts[index]);
            }
            //Reset needsOwnSort flags
            for (int outputIndex = index; outputIndex < contexts.Count; ++outputIndex)
            {
                var currentOutputContext = contexts[outputIndex];
                var abstractParticleOutput = currentOutputContext as VFXAbstractParticleOutput;
                if (abstractParticleOutput == null)
                    continue;
                abstractParticleOutput.needsOwnSort = false;
            }

            var implicitContext = new List<VFXContext>();

            bool needsGlobalSort = NeedsGlobalSort(out var globalSortCriterion);
            //Issues a global sort when at least two outputs have the same criterion.
            //If others don't match the criterion, or have a compute cull pass, they need a per output sort.
            if (needsGlobalSort)
            {
                // Then the camera sort
                var globalSort = VFXContext.CreateImplicitContext<VFXGlobalSort>(this);
                SetContextSortCriteria(ref globalSort, globalSortCriterion);
                implicitContext.Add(globalSort);
                m_Contexts.Add(globalSort);
            }
            //additional update
            for (int outputIndex = index; outputIndex < contexts.Count; ++outputIndex)
            {
                var currentOutputContext = contexts[outputIndex];
                var abstractParticleOutput = currentOutputContext as VFXAbstractParticleOutput;
                if (abstractParticleOutput == null)
                    continue;

                abstractParticleOutput.needsOwnSort = OutputNeedsOwnSort(abstractParticleOutput, globalSortCriterion, hasMainUpdate);
                if (abstractParticleOutput.NeedsOutputUpdate())
                {
                    var update = VFXContext.CreateImplicitContext<VFXOutputUpdate>(this);
                    update.SetOutput(abstractParticleOutput);
                    implicitContext.Add(update);
                    m_Contexts.Add(update);
                }
            }

            // And finally output
            for (; index < contexts.Count; ++index)
                m_Contexts.Add(contexts[index]);

            return implicitContext;
        }

        public bool NeedsIndirectBuffer()
        {
            return compilableOwners.OfType<VFXAbstractParticleOutput>().Any(o => o.HasIndirectDraw());
        }

        public bool NeedsGlobalIndirectBuffer()
        {
            return compilableOwners.OfType<VFXAbstractParticleOutput>().Any(o => o.HasIndirectDraw() && !VFXOutputUpdate.HasFeature(o.outputUpdateFeatures, VFXOutputUpdate.Features.IndirectDraw));
        }

        public bool NeedsGlobalSort()
        {
            bool hasMainUpdate = compilableOwners.OfType<VFXBasicUpdate>().Any();
            int sharedCriterionCount = GetGlobalSortingCriterionAndVoteCount(out var globalSortCriterion);
            return hasMainUpdate && sharedCriterionCount >= 2;
        }

        public bool NeedsGlobalSort(out SortingCriterion globalSortCriterion)
        {
            bool hasMainUpdate = compilableOwners.OfType<VFXBasicUpdate>().Any();
            if (!hasMainUpdate)
            {
                globalSortCriterion = null;
                return false;
            }
            int sharedCriterionCount = GetGlobalSortingCriterionAndVoteCount(out globalSortCriterion);
            return sharedCriterionCount >= 2;
        }

        int GetGlobalSortingCriterionAndVoteCount(out SortingCriterion globalSortCriterion)
        {
            var globalSortedCandidates = compilableOwners.OfType<VFXAbstractParticleOutput>()
                .Where(o => o.CanBeCompiled() && o.HasSorting() && !VFXOutputUpdate.HasFeature(o.outputUpdateFeatures, VFXOutputUpdate.Features.IndirectDraw)).ToArray();
            if (!globalSortedCandidates.Any())
            {
                globalSortCriterion = null;
                return 0;
            }

            Func<VFXAbstractParticleOutput, SortingCriterion> getVoteFunc = VFXSortingUtility.GetVoteFunc;
            var voteResult = MajorityVote(globalSortedCandidates, getVoteFunc, new SortingCriteriaComparer());
            globalSortCriterion = voteResult.Value >= 2 ? voteResult.Key : null;
            return voteResult.Value;
        }

        public override void FillDescs(
            VFXCompileErrorReporter reporter,
            List<VFXGPUBufferDesc> outBufferDescs,
            List<VFXTemporaryGPUBufferDesc> outTemporaryBufferDescs,
            List<VFXEditorSystemDesc> outSystemDescs,
            VFXExpressionGraph expressionGraph,
            Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData,
            Dictionary<VFXContext, int> contextSpawnToBufferIndex,
            VFXDependentBuffersData dependentBuffers,
            Dictionary<VFXContext, List<VFXContextLink>[]> effectiveFlowInputLinks,
            Dictionary<VFXData, uint> dataToSystemIndex,
            VFXSystemNames systemNames = null)
        {
            bool hasKill = IsAttributeStored(VFXAttribute.Alive);

            var deadListBufferIndex = -1;
            var deadListCountIndex = -1;
            var deadListCountCopyIndex = -1;

            var systemBufferMappings = new List<VFXMapping>();
            var systemValueMappings = new List<VFXMapping>();

            var attributeBufferIndex = dependentBuffers.attributeBuffers[this];

            int attributeSourceBufferIndex = -1;
            int eventGPUFrom = -1;

            var stripDataIndex = -1;

            int contextDataBufferIndex = -1;

            int instancingIndirectBufferIndex = -1;
            int instancingActiveIndirectBufferIndex = -1;

            if (m_DependenciesIn.Any())
            {
                if (m_DependenciesIn.Count != 1)
                {
                    throw new InvalidOperationException("Unexpected multiple input dependency for GPU event");
                }

                var dependency = m_DependenciesIn.First();

                attributeSourceBufferIndex = dependentBuffers.attributeBuffers[dependency];
                eventGPUFrom = dependentBuffers.eventBuffers[this];
               
                systemValueMappings.Add(new VFXMapping("parentSystemIndex", (int)dataToSystemIndex[dependency]));
            }
            var systemFlag = VFXSystemFlag.SystemDefault;
            if (attributeBufferIndex != -1)
            {
                systemFlag |= VFXSystemFlag.SystemHasAttributeBuffer;
                systemBufferMappings.Add(new VFXMapping("attributeBuffer", attributeBufferIndex));
            }

            contextDataBufferIndex = outBufferDescs.Count;
            outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Structured, size = 1, stride = 8 });
            systemBufferMappings.Add(new VFXMapping("instancingContextData", contextDataBufferIndex));

            if (m_ownAttributeSourceBuffer)
            {
                if (attributeSourceBufferIndex != -1)
                {
                    throw new InvalidOperationException("Unexpected source while filling description of data particle");
                }

                attributeSourceBufferIndex = outBufferDescs.Count;
                outBufferDescs.Add(m_layoutAttributeSource.GetBufferDesc(staticSourceCount));
            }

            if (attributeSourceBufferIndex != -1)
            {
                systemBufferMappings.Add(new VFXMapping("sourceAttributeBuffer", attributeSourceBufferIndex));
            }

            if (eventGPUFrom != -1)
            {
                systemFlag |= VFXSystemFlag.SystemReceivedEventGPU;
                systemBufferMappings.Add(new VFXMapping("eventList", eventGPUFrom));
            }

            if (hasKill)
            {
                systemFlag |= VFXSystemFlag.SystemHasKill;

                if (!hasStrip) // No dead list for strips
                {
                    deadListBufferIndex = outBufferDescs.Count;
                    outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Structured, size = capacity, stride = 4 });
                    systemBufferMappings.Add(new VFXMapping("deadList", deadListBufferIndex));

                    deadListCountIndex = outBufferDescs.Count;
                    outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Structured, size = 1, stride = 4 });
                    systemBufferMappings.Add(new VFXMapping("deadListCount", deadListCountIndex));

                    deadListCountCopyIndex = outBufferDescs.Count;
                    outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Structured, size = 1, stride = 4 });
                    systemBufferMappings.Add(new VFXMapping("deadListCountCopy", deadListCountCopyIndex));
                }
            }

            if (hasStrip)
            {
                systemFlag |= VFXSystemFlag.SystemHasStrips;

                systemValueMappings.Add(new VFXMapping("stripCount", (int)stripCapacity));
                systemValueMappings.Add(new VFXMapping("particlePerStripCount", (int)particlePerStripCount));

                stripDataIndex = dependentBuffers.stripBuffers[this];
                systemBufferMappings.Add(new VFXMapping("stripDataBuffer", stripDataIndex));
            }

            bool hasInstancing = true;
            if (hasInstancing)
            {
                // for custom instancing indirect, like rendering one particular instance of the batch
                instancingIndirectBufferIndex = outBufferDescs.Count;
                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Structured, size = 1, stride = 4 });
                systemBufferMappings.Add(new VFXMapping("instancingIndirect", instancingIndirectBufferIndex));

                instancingActiveIndirectBufferIndex = outBufferDescs.Count;
                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Structured, size = 1, stride = 4 });
                systemBufferMappings.Add(new VFXMapping("instancingActiveIndirect", instancingActiveIndirectBufferIndex));
            }

            if (hasDynamicSourceCount)
            {
                systemFlag |= VFXSystemFlag.SystemHasDirectLink;
            }

            if (needsComputeBounds || boundsMode == BoundsSettingMode.Automatic)
            {
                systemFlag |= VFXSystemFlag.SystemNeedsComputeBounds;

                var boundsBufferIndex = dependentBuffers.boundsBuffers[this];
                systemBufferMappings.Add(new VFXMapping("boundsBuffer", boundsBufferIndex));
            }

            if (boundsMode == BoundsSettingMode.Automatic)
            {
                systemFlag |= VFXSystemFlag.SystemAutomaticBounds;
            }

            if (space == VFXCoordinateSpace.World)
            {
                systemFlag |= VFXSystemFlag.SystemInWorldSpace;
            }

            //Particle systems allow use of instanced rendering
            systemFlag |= VFXSystemFlag.SystemUsesInstancedRendering;

            var initContext = m_Contexts.FirstOrDefault(o => o.contextType == VFXContextType.Init);
            if (initContext != null)
                systemBufferMappings.AddRange(effectiveFlowInputLinks[initContext]
                    .SelectMany(t => t.Select(u => u.context))
                    .Where(o => o.contextType == VFXContextType.Spawner)
                    .Select(o => new VFXMapping("spawner_input", contextSpawnToBufferIndex[o])));
            if (m_Contexts.Count() > 0 && m_Contexts.First().contextType == VFXContextType.Init) // TODO This test can be removed once we ensure priorly the system is valid
            {
                var mapper = contextToCompiledData[m_Contexts.First()].cpuMapper;

                var boundsCenterExp = mapper.FromNameAndId("bounds_center", -1);
                var boundsSizeExp = mapper.FromNameAndId("bounds_size", -1);
                var boundsPaddingExp = mapper.FromNameAndId("boundsPadding", -1);

                int boundsCenterIndex = boundsCenterExp != null ? expressionGraph.GetFlattenedIndex(boundsCenterExp) : -1;
                int boundsSizeIndex = boundsSizeExp != null ? expressionGraph.GetFlattenedIndex(boundsSizeExp) : -1;
                int boundsPaddingIndex = boundsPaddingExp != null ? expressionGraph.GetFlattenedIndex(boundsPaddingExp) : -1;

                if (boundsCenterIndex != -1 && boundsSizeIndex != -1)
                {
                    systemValueMappings.Add(new VFXMapping("bounds_center", boundsCenterIndex));
                    systemValueMappings.Add(new VFXMapping("bounds_size", boundsSizeIndex));
                }
                if (boundsPaddingIndex != -1)
                {
                    systemValueMappings.Add(new VFXMapping("boundsPadding", boundsPaddingIndex));
                }
            }

            systemValueMappings.Add(new VFXMapping("graphValuesOffset", systemValueMappings.Count + 1));
            foreach (var uniform in m_GraphValuesLayout.uniformBlocks.SelectMany(o => o))
                systemValueMappings.Add(new VFXMapping(m_SystemUniformMapper.GetName(uniform), expressionGraph.GetFlattenedIndex(uniform)));

            Dictionary<VFXContext, VFXOutputUpdate> indirectOutputToCuller = null;
            bool needsIndirectBuffer = NeedsIndirectBuffer();
            int globalIndirectBufferIndex = -1;
            bool needsGlobalIndirectBuffer = false;
            if (needsIndirectBuffer)
            {
                indirectOutputToCuller = new Dictionary<VFXContext, VFXOutputUpdate>();
                foreach (var cullCompute in m_Contexts.OfType<VFXOutputUpdate>())
                    if (cullCompute.HasFeature(VFXOutputUpdate.Features.IndirectDraw))
                        indirectOutputToCuller.Add(cullCompute.output, cullCompute);

                var allIndirectOutputs = owners.OfType<VFXAbstractParticleOutput>().Where(o => o.HasIndirectDraw());

                needsGlobalIndirectBuffer = NeedsGlobalIndirectBuffer();
                if (needsGlobalIndirectBuffer)
                {
                    globalIndirectBufferIndex = outBufferDescs.Count;
                    systemBufferMappings.Add(new VFXMapping("indirectBuffer0", outBufferDescs.Count));
                    outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Structured, size = capacity + 1, stride = 4 });
                }

                int currentIndirectBufferIndex = globalIndirectBufferIndex == -1 ? 0 : 1;
                foreach (var indirectOutput in allIndirectOutputs)
                {
                    if (indirectOutputToCuller.ContainsKey(indirectOutput))
                    {
                        VFXOutputUpdate culler = indirectOutputToCuller[indirectOutput];
                        uint bufferCount = culler.bufferCount;
                        culler.bufferIndex = outBufferDescs.Count;
                        bool perCamera = culler.isPerCamera;
                        uint bufferStride = culler.HasFeature(VFXOutputUpdate.Features.Sort) ? 8u : 4u;
                        for (uint i = 0; i < bufferCount; ++i)
                        {
                            string bufferName = "indirectBuffer" + currentIndirectBufferIndex++;
                            if (perCamera)
                                bufferName += "PerCamera";
                            systemBufferMappings.Add(new VFXMapping(bufferName, outBufferDescs.Count));
                            outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Structured, size = capacity + 1, stride = bufferStride });
                        }

                        if (culler.HasFeature(VFXOutputUpdate.Features.Sort))
                        {
                            culler.sortedBufferIndex = outBufferDescs.Count;
                            for (uint i = 0; i < bufferCount; ++i)
                                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Default, size = capacity + 1, stride = 4 });
                        }
                        else
                            culler.sortedBufferIndex = culler.bufferIndex;
                    }
                }
            }

            int batchedInitParamsIndex = -1;
            int graphValuesBufferIndex = -1;
            int instancesPrefixSumBufferIndex = -1;
            int eventsPrefixSumBufferIndex = -1;
            int spawnCountPrefixSumBufferIndex = -1;
            if (hasInstancing)
            {
                if (eventGPUFrom == -1) //GPUEVent doesn't have any uniform buffer
                    FillBatchedUniformsBuffers(outBufferDescs, systemBufferMappings, out batchedInitParamsIndex);

                FillGraphValuesBuffers(outBufferDescs, systemBufferMappings, m_GraphValuesLayout, out graphValuesBufferIndex);

                FillPrefixSumBuffers(outBufferDescs, systemBufferMappings, staticSourceCount,
                    out instancesPrefixSumBufferIndex,
                    out eventsPrefixSumBufferIndex,
                    out spawnCountPrefixSumBufferIndex);
            }

            // sort buffers
            int sortBufferAIndex = -1;
            int sortBufferBIndex = -1;
            bool needsGlobalSort = NeedsGlobalSort();
            if (needsGlobalSort)
            {
                sortBufferAIndex = outBufferDescs.Count;
                sortBufferBIndex = sortBufferAIndex + 1;

                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Default, size = capacity + 1, stride = 8 });
                systemBufferMappings.Add(new VFXMapping("sortBufferA", sortBufferAIndex));

                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Default, size = capacity + 1, stride = 8 });
                systemBufferMappings.Add(new VFXMapping("sortBufferB", sortBufferBIndex));
            }

            var elementToVFXBufferMotionVector = new Dictionary<VFXContext, int>();
            foreach (VFXOutputUpdate context in m_Contexts.OfType<VFXOutputUpdate>())
            {
                if (context.HasFeature(VFXOutputUpdate.Features.MotionVector))
                {
                    uint sizePerElement = 12U * 4U;
                    if (context.output.SupportsMotionVectorPerVertex(out uint vertsCount))
                    {
                        // 2 floats per vertex
                        sizePerElement = vertsCount * 2U * 4U;
                    }
                    // add previous frame index
                    sizePerElement += 4U;
                    int currentElementToVFXBufferMotionVector = outTemporaryBufferDescs.Count;
                    outTemporaryBufferDescs.Add(new VFXTemporaryGPUBufferDesc() { frameCount = 2u, desc = new VFXGPUBufferDesc { type = ComputeBufferType.Raw, size = capacity * sizePerElement, stride = 4 } });
                    elementToVFXBufferMotionVector.Add(context.output, currentElementToVFXBufferMotionVector);
                }
            }

            var taskDescs = new List<VFXEditorTaskDesc>();
            var bufferMappings = new List<VFXMapping>();
            var uniformMappings = new List<VFXMapping>();
            var additionalParameters = new List<VFXMapping>();
            var instanceSplitDescs = new List<VFXInstanceSplitDesc>();

            AddInstanceSplitDesc(instanceSplitDescs, new List<uint>());

            for (int i = 0; i < m_Contexts.Count; ++i)
            {
                var temporaryBufferMappings = new List<VFXMappingTemporary>();

                var context = m_Contexts[i];
                if (!contextToCompiledData.TryGetValue(context, out var contextData))
                    throw new InvalidOperationException("Unexpected context which hasn't been compiled : " + context);

                var taskDesc = new VFXEditorTaskDesc();
                taskDesc.type = (UnityEngine.VFX.VFXTaskType)context.taskType;

                bufferMappings.Clear();
                additionalParameters.Clear();

                if (context is VFXOutputUpdate)
                {
                    var update = (VFXOutputUpdate)context;
                    if (update.HasFeature(VFXOutputUpdate.Features.MotionVector))
                    {
                        var currentIndex = elementToVFXBufferMotionVector[update.output];
                        temporaryBufferMappings.Add(new VFXMappingTemporary() { pastFrameIndex = 0u, perCameraBuffer = true, mapping = new VFXMapping("elementToVFXBuffer", currentIndex) });
                    }
                }
                else if (context.contextType == VFXContextType.Output && (context is IVFXSubRenderer) && (context as IVFXSubRenderer).hasMotionVector)
                {
                    var currentIndex = elementToVFXBufferMotionVector[context];
                    temporaryBufferMappings.Add(new VFXMappingTemporary() { pastFrameIndex = 1u, perCameraBuffer = true, mapping = new VFXMapping("elementToVFXBufferPrevious", currentIndex) });
                }

                if (attributeBufferIndex != -1)
                    bufferMappings.Add(new VFXMapping("attributeBuffer", attributeBufferIndex));

                if (graphValuesBufferIndex != -1)
                    bufferMappings.Add(new VFXMapping("graphValuesBuffer", graphValuesBufferIndex));

                if (eventGPUFrom != -1 && context.contextType == VFXContextType.Init)
                    bufferMappings.Add(new VFXMapping("eventList", eventGPUFrom));

                if (deadListBufferIndex != -1 && (context.taskType == VFXTaskType.Initialize || context.taskType == VFXTaskType.Update))
                    bufferMappings.Add(new VFXMapping(context.contextType == VFXContextType.Update ? "deadListOut" : "deadListIn", deadListBufferIndex));

                if (deadListCountIndex != -1 && (context.contextType == VFXContextType.Init || context.contextType == VFXContextType.Update))
                    bufferMappings.Add(new VFXMapping("deadListCount", deadListCountIndex));

                if(deadListCountCopyIndex != -1 && context.contextType == VFXContextType.Init)
                    bufferMappings.Add(new VFXMapping("deadListCountCopy", deadListCountCopyIndex));

                if (attributeSourceBufferIndex != -1 && context.contextType == VFXContextType.Init)
                    bufferMappings.Add(new VFXMapping("sourceAttributeBuffer", attributeSourceBufferIndex));

                if (stripDataIndex != -1 && context.ownedType == VFXDataType.ParticleStrip)
                    bufferMappings.Add(new VFXMapping("stripDataBuffer", stripDataIndex));

                if (contextDataBufferIndex != -1)
                {
                    switch (context.contextType)
                    {
                        case VFXContextType.Init:
                        case VFXContextType.Update:
                        case VFXContextType.Filter:
                        case VFXContextType.Output:
                            bufferMappings.Add(new VFXMapping("instancingContextData", contextDataBufferIndex));
                            break;
                    }
                }

                if (context.contextType == VFXContextType.Init)
                {
                    if (batchedInitParamsIndex != -1)
                        bufferMappings.Add(new VFXMapping("batchedInitParams", batchedInitParamsIndex));
                    if(eventsPrefixSumBufferIndex != -1)
                        bufferMappings.Add(new VFXMapping("eventCountPrefixSum", eventsPrefixSumBufferIndex));
                    if(spawnCountPrefixSumBufferIndex != -1)
                        bufferMappings.Add(new VFXMapping("spawnCountPrefixSum", spawnCountPrefixSumBufferIndex));
                }

                if (hasInstancing)
                {
                    bool needsInstancePrefixSum = context.contextType == VFXContextType.Init;
                    needsInstancePrefixSum |= !hasKill && (context.contextType == VFXContextType.Update || context.contextType == VFXContextType.Filter);
                    if (instancesPrefixSumBufferIndex != -1 && needsInstancePrefixSum)
                        bufferMappings.Add(new VFXMapping("instancingPrefixSum", instancesPrefixSumBufferIndex));

                    switch (context.contextType)
                    {
                        case VFXContextType.Init:
                        case VFXContextType.Update:
                        case VFXContextType.Filter:
                            if (instancingIndirectBufferIndex != -1)
                                bufferMappings.Add(new VFXMapping("instancingIndirect", instancingIndirectBufferIndex));

                            if (instancingActiveIndirectBufferIndex != -1)
                                bufferMappings.Add(new VFXMapping("instancingActiveIndirect", instancingActiveIndirectBufferIndex));
                            break;
                    }
                }

                bool hasAttachedStrip = IsAttributeStored(VFXAttribute.StripAlive);
                if (hasAttachedStrip)
                {
                    var stripData = dependenciesOut.First(d => ((VFXDataParticle)d).hasStrip); // TODO Handle several strip attached
                    bufferMappings.Add(new VFXMapping("attachedStripDataBuffer", dependentBuffers.stripBuffers[stripData]));
                }

                if (needsIndirectBuffer)
                {
                    systemFlag |= VFXSystemFlag.SystemHasIndirectBuffer;

                    if (context.contextType == VFXContextType.Output && (context as VFXAbstractParticleOutput).HasIndirectDraw())
                    {
                        bool hasCuller = indirectOutputToCuller.ContainsKey(context);
                        additionalParameters.Add(new VFXMapping("indirectIndex", hasCuller ? indirectOutputToCuller[context].bufferIndex : globalIndirectBufferIndex));
                        bufferMappings.Add(new VFXMapping("indirectBuffer", hasCuller ? indirectOutputToCuller[context].sortedBufferIndex : globalIndirectBufferIndex));
                    }

                    if (context.contextType == VFXContextType.Update)
                    {
                        if (context.taskType == VFXTaskType.Update && needsGlobalIndirectBuffer)
                            bufferMappings.Add(new VFXMapping("indirectBuffer", globalIndirectBufferIndex));
                    }

                    if (context.contextType == VFXContextType.Filter)
                    {
                        if (context.taskType == VFXTaskType.GlobalSort && needsGlobalIndirectBuffer)
                            bufferMappings.Add(new VFXMapping("inputBuffer", globalIndirectBufferIndex));
                        else if (context is VFXOutputUpdate)
                        {
                            var outputUpdate = (VFXOutputUpdate)context;
                            int startIndex = outputUpdate.bufferIndex;
                            uint bufferCount = outputUpdate.bufferCount;
                            for (int j = 0; j < bufferCount; ++j)
                                bufferMappings.Add(new VFXMapping("outputBuffer" + j, startIndex + j));
                        }
                    }
                }

                if (deadListBufferIndex != -1 && context.contextType == VFXContextType.Output && (context as VFXAbstractParticleOutput).NeedsDeadListCount())
                    bufferMappings.Add(new VFXMapping("deadListCount", deadListCountIndex));

                if (context.taskType == VFXTaskType.GlobalSort)
                {
                    bufferMappings.Add(new VFXMapping("outputBuffer", sortBufferAIndex));
                    if (deadListCountIndex != -1)
                        bufferMappings.Add(new VFXMapping("deadListCount", deadListCountIndex));
                }

                for (uint indexTarget = 0; indexTarget < (uint)contextData.linkedEventOut.Length; ++indexTarget)
                {
                    var gpuTarget = dependentBuffers.eventBuffers[contextData.linkedEventOut[indexTarget].data];
                    var prefix = VFXCodeGeneratorHelper.GeneratePrefix(indexTarget);
                    bufferMappings.Add(new VFXMapping($"eventListOut_{prefix}", gpuTarget));
                }

                var instancingSplitDescValues = contextData.instancingSplitValues;
                uniformMappings.Clear();

                foreach (var buffer in contextData.uniformMapper.buffers)
                {
                    int index = expressionGraph.GetFlattenedIndex(buffer);
                    if (!buffer.IsAny(VFXExpression.Flags.Constant | VFXExpression.Flags.Foldable))
                    {
                        instancingSplitDescValues.Add((uint)index);
                    }
                    var name = contextData.uniformMapper.GetName(buffer);
                    uniformMappings.Add(new VFXMapping(name, index));
                }
                foreach (var texture in contextData.uniformMapper.textures)
                {
                    int index = expressionGraph.GetFlattenedIndex(texture);
                    if (!texture.IsAny(VFXExpression.Flags.Constant | VFXExpression.Flags.Foldable))
                    {
                        instancingSplitDescValues.Add((uint)index);
                    }
                    // TODO At the moment issue all names sharing the same texture as different texture slots. This is not optimized as it required more texture binding than necessary
                    foreach (var name in contextData.uniformMapper.GetNames(texture))
                        uniformMappings.Add(new VFXMapping(name, index));
                }

                // Retrieve all cpu mappings at context level (-1)
                var cpuMappings = contextData.cpuMapper.CollectExpression(-1).Select(exp => new VFXMapping(exp.name, expressionGraph.GetFlattenedIndex(exp.exp))).ToArray();

                //Check potential issue with invalid operation on CPU
                foreach (var mapping in cpuMappings)
                {
                    if (mapping.index < 0)
                    {
                        reporter?.RegisterError(context.GetSlotByPath(true, mapping.name), "GPUNodeLinkedTOCPUSlot", VFXErrorType.Error, "Can not link a GPU operator to a system wide (CPU) input.");
                        throw new InvalidOperationException("Unable to compute CPU expression for mapping : " + mapping.name);
                    }
                }

                taskDesc.buffers = bufferMappings.ToArray();
                taskDesc.temporaryBuffers = temporaryBufferMappings.ToArray();
                taskDesc.values = uniformMappings.OrderBy(mapping => mapping.index).ToArray();
                taskDesc.parameters = cpuMappings.Concat(contextData.parameters).Concat(additionalParameters).ToArray();
                taskDesc.shaderSourceIndex = contextToCompiledData[context].indexInShaderSource;
                taskDesc.instanceSplitIndex = AddInstanceSplitDesc(instanceSplitDescs, instancingSplitDescValues);
                taskDesc.model = context;

                if (context is IVFXMultiMeshOutput multiMeshOutput && multiMeshOutput.meshCount > 0) // If the context is a multi mesh output, split and patch task desc into several tasks
                {
                    for (int j = (int)multiMeshOutput.meshCount - 1; j >= 0; --j) // Back to front to be consistent with LOD and alpha
                    {
                        VFXEditorTaskDesc singleMeshTaskDesc = taskDesc;
                        singleMeshTaskDesc.parameters = VFXMultiMeshHelper.PatchCPUMapping(taskDesc.parameters, multiMeshOutput.meshCount, j).ToArray();
                        singleMeshTaskDesc.buffers = VFXMultiMeshHelper.PatchBufferMapping(taskDesc.buffers, j).ToArray();
                        var instancingSplitDescValuesMesh = new List<uint>(instancingSplitDescValues);
                        VFXMultiMeshHelper.PatchInstancingSplitValues(instancingSplitDescValuesMesh, expressionGraph, context.inputSlots, multiMeshOutput.meshCount, j);
                        singleMeshTaskDesc.instanceSplitIndex = AddInstanceSplitDesc(instanceSplitDescs, instancingSplitDescValuesMesh);
                        taskDescs.Add(singleMeshTaskDesc);
                    }
                }
                else
                    taskDescs.Add(taskDesc);

                // if task is a per output update with sorting, add sort tasks
                if (context is VFXOutputUpdate)
                {
                    var update = (VFXOutputUpdate)context;

                    if (update.HasFeature(VFXOutputUpdate.Features.CameraSort) || update.HasFeature(VFXOutputUpdate.Features.Sort))
                    {
                        for (int j = 0; j < update.bufferCount; ++j)
                        {
                            VFXEditorTaskDesc sortTaskDesc = new VFXEditorTaskDesc();
                            sortTaskDesc.type = UnityEngine.VFX.VFXTaskType.PerOutputSort;
                            sortTaskDesc.externalProcessor = null;
                            sortTaskDesc.model = context;

                            sortTaskDesc.buffers = new VFXMapping[3];
                            sortTaskDesc.buffers[0] = new VFXMapping("srcBuffer", update.bufferIndex + j);
                            if (capacity > 4096) // Add scratch buffer
                            {
                                sortTaskDesc.buffers[1] = new VFXMapping("scratchBuffer", outBufferDescs.Count);
                                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Default, size = capacity + 1, stride = 8 });
                            }
                            else
                                sortTaskDesc.buffers[1] = new VFXMapping("scratchBuffer", -1); // No scratchBuffer needed
                            sortTaskDesc.buffers[2] = new VFXMapping("dstBuffer", update.sortedBufferIndex + j);

                            sortTaskDesc.parameters = new VFXMapping[2];
                            sortTaskDesc.parameters[0] = new VFXMapping("globalSort", 0);
                            sortTaskDesc.parameters[1] = new VFXMapping("isPerCameraSort", update.isPerCamera ? 1 : 0);

                            taskDescs.Add(sortTaskDesc);
                        }
                    }
                }
            }

            string nativeName = string.Empty;
            if (systemNames != null)
                nativeName = systemNames.GetUniqueSystemName(this);
            else
                throw new InvalidOperationException("system names manager cannot be null");

            outSystemDescs.Add(new VFXEditorSystemDesc()
            {
                flags = systemFlag,
                tasks = taskDescs.ToArray(),
                capacity = capacity,
                name = nativeName,
                buffers = systemBufferMappings.ToArray(),
                values = systemValueMappings.ToArray(),
                instanceSplitDescs = instanceSplitDescs.ToArray(),
                type = VFXSystemType.Particle,
                layer = m_Layer
            });
        }

        private void FillGraphValuesBuffers(List<VFXGPUBufferDesc> outBufferDescs, List<VFXMapping> systemBufferMappings, GraphValuesLayout graphValuesLayout, out int graphValuesIndex)
        {
            var graphValuesSize = graphValuesLayout.paddedSizeInBytes / 4;
            if (graphValuesSize == 0)
            {
                graphValuesIndex = -1;
                return;
            }
            graphValuesIndex = outBufferDescs.Count;
            outBufferDescs.Add(new VFXGPUBufferDesc()
            {
                type = ComputeBufferType.Raw, size = graphValuesSize, stride = 4u
            });
            systemBufferMappings.Add(new VFXMapping("graphValuesBuffer", graphValuesIndex));
        }

        private static void FillBatchedUniformsBuffers(List<VFXGPUBufferDesc> outBufferDescs, List<VFXMapping> systemBufferMappings,
            out int batchedInitParamsIndex)
        {
            uint initParamsStride = 16u;
            batchedInitParamsIndex = outBufferDescs.Count;
            outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Default, size = 1u, stride = initParamsStride });
            systemBufferMappings.Add(new VFXMapping("batchedInitParams", batchedInitParamsIndex));
        }
        private static void FillPrefixSumBuffers(List<VFXGPUBufferDesc> outBufferDescs, List<VFXMapping> systemBufferMappings, uint staticSourceCount,
            out int instancesPrefixSumBufferIndex,
            out int eventsPrefixSumBufferIndex,
            out int spawnCountPrefixSumBufferIndex)
        {
            instancesPrefixSumBufferIndex = outBufferDescs.Count;
            outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Default, size = 1u, stride = 4 });
            systemBufferMappings.Add(new VFXMapping("instancingPrefixSum", instancesPrefixSumBufferIndex));

            eventsPrefixSumBufferIndex = outBufferDescs.Count;
            outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Default, size = 1u, stride = 4 });
            systemBufferMappings.Add(new VFXMapping("eventCountPrefixSum", eventsPrefixSumBufferIndex));

            spawnCountPrefixSumBufferIndex = outBufferDescs.Count;
            uint spawnCountSize = Math.Max(staticSourceCount, 1u);
            outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Default, size = spawnCountSize, stride = 4 });
            systemBufferMappings.Add(new VFXMapping("spawnCountPrefixSum", spawnCountPrefixSumBufferIndex));
        }


        private static uint AddInstanceSplitDesc(List<VFXInstanceSplitDesc> instanceSplitDescs, List<uint> instanceSplitDescValues)
        {
            int index = -1;

            instanceSplitDescValues.Sort();

            for (int i = 0; i < instanceSplitDescs.Count; ++i)
            {
                if (instanceSplitDescValues.SequenceEqual(instanceSplitDescs[i].values))
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                index = instanceSplitDescs.Count;
                var newEntry = new VFXInstanceSplitDesc();
                newEntry.values = instanceSplitDescValues.ToArray();
                instanceSplitDescs.Add(newEntry);
            }
            return (uint)index;
        }

        public override void Sanitize(int version)
        {
            if (version < 8)
            {
                SetSettingValue("boundsMode", BoundsSettingMode.Manual);
            }

            if(boundsMode != BoundsSettingMode.Automatic && needsComputeBounds)
                SetSettingValue(nameof(needsComputeBounds), false);


            base.Sanitize(version);
        }

        public override void CopySettings<T>(T dst)
        {
            var instance = dst as VFXDataParticle;
            instance.m_Space = m_Space;
        }

        public StructureOfArrayProvider.BucketInfo[] GetCurrentAttributeLayout()
        {
            return m_layoutAttributeCurrent.GetBucketLayoutInfo();
        }

        public StructureOfArrayProvider.BucketInfo[] GetSourceAttributeLayout()
        {
            return m_layoutAttributeSource.GetBucketLayoutInfo();
        }

        [SerializeField]
        private VFXCoordinateSpace m_Space; // TODO Should be an actual setting
        [NonSerialized]
        private StructureOfArrayProvider m_layoutAttributeCurrent = new StructureOfArrayProvider();
        [NonSerialized]
        private StructureOfArrayProvider m_layoutAttributeSource = new StructureOfArrayProvider();
        [NonSerialized]
        private bool m_ownAttributeSourceBuffer;

        [NonSerialized]
        private VFXUniformMapper m_SystemUniformMapper;
        [NonSerialized]
        private GraphValuesLayout m_GraphValuesLayout;

        public VFXUniformMapper systemUniformMapper => m_SystemUniformMapper;

        public struct GraphValuesLayout
        {
            public List<List<VFXExpression>> uniformBlocks;
            public Dictionary<string, int> nameToOffset;
            public uint paddedSizeInBytes;

            private static readonly int kAlignement = 4;

            public void SetUniformBlocks(List<VFXExpression> orderedUniforms)
            {
                if (uniformBlocks == null)
                {
                    uniformBlocks = new List<List<VFXExpression>>();
                }
                else
                {
                    uniformBlocks.Clear();
                }
                foreach (var value in orderedUniforms)
                {
                    var block = uniformBlocks.FirstOrDefault(b =>
                        b.Sum(e => VFXValue.TypeToSize(e.valueType)) + VFXValue.TypeToSize(value.valueType) <= kAlignement);
                    if (block != null)
                        block.Add(value);
                    else
                        uniformBlocks.Add(new List<VFXExpression>() { value });
                }
            }

            private static int ComputePadding(int offset)
            {
                return (kAlignement - (offset % kAlignement)) % kAlignement;
            }

            public void GenerateOffsetMap(VFXUniformMapper systemUniformMapper)
            {
                int mapSize = uniformBlocks.Sum(o => o.Count);
                nameToOffset = new Dictionary<string, int>(mapSize);
                int currentOffset = 0;
                foreach (var block in uniformBlocks)
                {
                    int currentBlockSize = 0;
                    foreach (var value in block)
                    {
                        string name = systemUniformMapper.GetName(value);

                        if (nameToOffset.ContainsKey(name))
                        {
                            throw new ArgumentException(
                                "Uniform name should not appear twice in the graph values offset map");
                        }
                        nameToOffset.Add(name, currentOffset);
                        int typeSize = VFXExpression.TypeToSize(value.valueType);
                        currentOffset += sizeof(uint) * typeSize;
                        currentBlockSize += typeSize;
                    }
                    currentOffset += sizeof(uint) * ComputePadding(currentBlockSize);
                }
                paddedSizeInBytes = (uint)currentOffset;
            }
        }

        public GraphValuesLayout graphValuesLayout
        {
            get { return m_GraphValuesLayout; }
            set { m_GraphValuesLayout = value; }
        }

        public void GenerateSystemUniformMapper(VFXExpressionGraph graph, Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData)
        {
            VFXUniformMapper uniformMapper = null;
            foreach (var context in m_Contexts)
            {
                var gpuMapper = graph.BuildGPUMapper(context);
                var contextUniformMapper = new VFXUniformMapper(gpuMapper, context.doesGenerateShader, true);

                // SG inputs if needed
                var fragInputNames = context.fragmentParameters;
                var vertInputNames = context.vertexParameters;
                var contextSGInputs = fragInputNames.Any() || vertInputNames.Any() ? new VFXSGInputs(gpuMapper, contextUniformMapper, vertInputNames, fragInputNames) : null;

                // Add gpu and uniform mapper
                var contextData = contextToCompiledData[context];
                contextData.gpuMapper = gpuMapper;
                contextData.uniformMapper = contextUniformMapper;
                contextData.SGInputs = contextSGInputs;
                contextToCompiledData[context] = contextData;

                if (uniformMapper == null)
                    uniformMapper = new VFXUniformMapper(gpuMapper, true, true);
                else
                {
                    uniformMapper.AppendMapper(gpuMapper);
                }
            }

            m_SystemUniformMapper = uniformMapper;
            m_GraphValuesLayout = new GraphValuesLayout();
            var orderedUniforms = new List<VFXExpression>(m_SystemUniformMapper?.uniforms
                .Where(e => !e.IsAny(VFXExpression.Flags.Constant |
                                     VFXExpression.Flags.InvalidOnCPU)) // Filter out constant expressions
                .OrderByDescending(e => VFXValue.TypeToSize(e.valueType)));

            m_GraphValuesLayout.SetUniformBlocks(orderedUniforms);
            m_GraphValuesLayout.GenerateOffsetMap(m_SystemUniformMapper);
        }

        internal override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            base.GenerateErrors(manager);

            if (boundsMode == BoundsSettingMode.Automatic)
            {
                if (CanBeCompiled())
                    manager.RegisterError("WarningAutomaticBoundsFlagChange", VFXErrorType.Warning,
                        $"Changing the bounds mode to Automatic modifies the Culling Flags on the Visual Effect Asset to Always recompute bounds and simulate.");
            }
        }
    }
}
