using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Serialization;
using static UnityEditor.VFX.VFXSortingUtility;

namespace UnityEditor.VFX
{
    interface ILayoutProvider
    {
        void GenerateAttributeLayout(uint capacity, Dictionary<VFXAttribute, int> storedAttribute, bool splitBuckets);
        string GetCodeOffset(VFXAttribute attrib, uint capacity, string index, string instanceIndex);
        uint GetBufferSize(uint capacity);

        VFXGPUBufferDesc GetBufferDesc(uint capacity, ComputeBufferMode mode = ComputeBufferMode.Immutable);
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

        private void AddBucket(int capacity, int bucketId, int currentOffset)
        {
            int bucketOffset = bucketId == 0 ? 0 : m_BucketOffsets[bucketId - 1] + capacity * m_BucketSizes[bucketId - 1];
            m_BucketOffsets.Add((bucketOffset + 3) & ~3); // align on dword;
            m_BucketSizes.Add(currentOffset);
        }

        private void AddAttributeBuckets(List<VFXAttribute> attributes, int capacity, ref int bucketId, bool splitBuckets)
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
            foreach (var block in attribBlocks)
            {
                if (splitBuckets)
                    currentOffset = 0;

                foreach (var attrib in block)
                {
                    int size = VFXValue.TypeToSize(attrib.type);
                    m_AttributeLayout.Add(attrib, new AttributeLayout(bucketId, currentOffset));
                    currentOffset += size;
                }
                if (splitBuckets)
                {
                    AddBucket(capacity, bucketId, currentOffset);
                    bucketId++;
                }
            }
            if (!splitBuckets)
            {
                AddBucket(capacity, bucketId, currentOffset);
                bucketId++;
            }
        }

        public void GenerateAttributeLayout(uint capacity, Dictionary<VFXAttribute, int> storedAttribute, bool splitBuckets = true)
        {
            m_BucketSizes.Clear();
            m_AttributeLayout.Clear();
            m_BucketOffsets.Clear();

            var attributeGroups = new Dictionary<int, List<VFXAttribute>>();
            foreach (var kvp in storedAttribute)
            {
                List<VFXAttribute> attributes;
                if (!attributeGroups.ContainsKey(kvp.Value))
                {
                    attributes = new List<VFXAttribute>();
                    attributeGroups[kvp.Value] = attributes;
                }
                else
                    attributes = attributeGroups[kvp.Value];

                attributes.Add(kvp.Key);
            }

            int bucketId = 0;
            foreach (var group in attributeGroups)
            {
                AddAttributeBuckets(group.Value, (int)capacity, ref bucketId, splitBuckets);
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

        public VFXGPUBufferDesc GetBufferDesc(uint capacity, ComputeBufferMode mode = ComputeBufferMode.Immutable)
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
                debugName = "VFXAttributeBuffer",
                target = GraphicsBuffer.Target.Raw,
                size = GetBufferSize(capacity),
                stride = 4,
                capacity = capacity,
                layout = layout.ToArray(),
                mode = mode
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
        public static readonly string k_IndirectBufferName = "indirectBuffer";
        public static readonly string k_SortedIndirectBufferName = "sortedIndirectBuffer";

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
        [VFXSetting(VFXSettingAttribute.VisibleFlags.Default),
         Tooltip("Specifies how the bounds are set. They can be set manually, recorded in the Target GameObject window, or computed automatically at a small performance cost."),
         SerializeField]
        public BoundsSettingMode boundsMode = BoundsSettingMode.Recorded;

        public bool hasStrip => dataType == DataType.ParticleStrip;
        public bool hasAttachedStrip => IsAttributeStored(VFXAttribute.StripAlive);
        public VFXDataParticle attachedStripData => (VFXDataParticle)dependenciesOut.FirstOrDefault(d => ((VFXDataParticle)d).hasStrip); // TODO Handle several strip attached

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
                if (hasAttachedStrip)
                {
                    var stripData = attachedStripData;
                    yield return "#define ATTACHED_STRIP_COUNT " + stripData.stripCapacity + "u";
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

        public VFXSpace space
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
                m_layoutAttributeSource.GenerateAttributeLayout(m_SourceCount, readSourceAttribute, false);
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
            bool attribFound = false;
            string attributeBuffer = null;
            string codeOffset = null;

            if (location == VFXAttributeLocation.Current)
            {
                attribFound = m_StoredCurrentAttributes.ContainsKey(attrib);
                attributeBuffer = "attributeBuffer";
                codeOffset = m_layoutAttributeCurrent.GetCodeOffset(attrib, alignedCapacity, "index", "instanceIndex");
            }
            else // source attributes
            {
                attribFound = m_ReadSourceAttributes.Any(a => a.name == attrib.name);
                attributeBuffer = "sourceAttributeBuffer";
                var parent = m_DependenciesIn.OfType<VFXDataParticle>().FirstOrDefault();
                if (parent != null)
                    codeOffset = m_layoutAttributeSource.GetCodeOffset(attrib, parent.alignedCapacity, "sourceIndex", "instanceIndex");
                else
                    codeOffset = m_layoutAttributeSource.GetCodeOffset(attrib, "sourceIndex", "startEventIndex");
            }

            if (!attribFound)
                throw new ArgumentException(string.Format("Attribute {0} does not exist in data layout", attrib.name));

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

            int updateIndex = Int32.MaxValue;
            // First add init and updates
            for (index = 0; index < contexts.Count; ++index)
            {
                if (contexts[index].contextType == VFXContextType.Update)
                    updateIndex = index;

                if (contexts[index].contextType == VFXContextType.Output)
                    break;
                m_Contexts.Add(contexts[index]);
            }
            bool hasMainUpdate = updateIndex != Int32.MaxValue;

            //Reset needsOwnSort and needsOwnAabbBuffer flags
            for (int outputIndex = index; outputIndex < contexts.Count; ++outputIndex)
            {
                var currentOutputContext = contexts[outputIndex];
                var abstractParticleOutput = currentOutputContext as VFXAbstractParticleOutput;
                if (abstractParticleOutput == null)
                    continue;
                abstractParticleOutput.needsOwnSort = false;
                abstractParticleOutput.needsOwnAabbBuffer = false;
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

            //AABB Buffer rules :
            var rayTracedOutputs = compilableOwners.OfType<VFXAbstractParticleOutput>().Where(o => o.isRayTraced).ToArray();
            var outputsWithoutAabbModifs = new List<VFXAbstractParticleOutput>();
            if (rayTracedOutputs.Length > 0)
            {
                foreach (var output in rayTracedOutputs)
                {
                    if (output.ModifiesAabbAttributes())
                    {
                        output.needsOwnAabbBuffer = true;
                    }
                    else
                        outputsWithoutAabbModifs.Add(output);
                }
            }

            if (outputsWithoutAabbModifs.Count > 0 && hasMainUpdate)
            {
                var updateContext = (VFXBasicUpdate)contexts[updateIndex];
                var firstEligibleOutput = outputsWithoutAabbModifs[0];
                uint sharedDecimationFactor = firstEligibleOutput.GetRaytracingDecimationFactor();
                updateContext.rayTracingDefines = firstEligibleOutput.rayTracingDefines;
                for (var i = 1; i < outputsWithoutAabbModifs.Count; i++)
                {
                    if (outputsWithoutAabbModifs[i].GetRaytracingDecimationFactor() != sharedDecimationFactor
                        || !firstEligibleOutput.HasSameRayTracingScalingMode(outputsWithoutAabbModifs[i]) )
                    {
                        outputsWithoutAabbModifs[i].needsOwnAabbBuffer = true;
                    }
                }
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

        public bool NeedsSharedAabbBuffer()
        {
            return compilableOwners.OfType<VFXAbstractParticleOutput>().Any(o => !o.NeedsOwnAabbBuffer() && o.isRayTraced);
        }

        private void PrepareAABBBuffers(out List<VFXAbstractParticleOutput> outputsSharingAABB,
            out Dictionary<VFXAbstractParticleOutput, int> outputsOwningAABB,
            out Dictionary<VFXAbstractParticleOutput, uint> outputAabbSize,
            out int sharedAabbBufferIndex,
            out uint sharedAabbCount,
            ref List<VFXMapping> systemBufferMappings,
            ref List<VFXGPUBufferDesc> outBufferDescs)
        {
            outputsSharingAABB = new List<VFXAbstractParticleOutput>();
            outputsOwningAABB = new Dictionary<VFXAbstractParticleOutput, int>();
            outputAabbSize = new Dictionary<VFXAbstractParticleOutput, uint>();
            List<VFXAbstractParticleOutput> listOutputsOwningAABB = new List<VFXAbstractParticleOutput>();
            sharedAabbCount = 0u;


            sharedAabbBufferIndex = -1;
            var rayTracedOutputs = compilableOwners.OfType<VFXAbstractParticleOutput>().Where(o => o.isRayTraced).ToArray();
            if (rayTracedOutputs.Length == 0)
                return;
            foreach (var output in rayTracedOutputs)
            {
                if (output.NeedsOwnAabbBuffer())
                    listOutputsOwningAABB.Add(output);
                else
                    outputsSharingAABB.Add(output);
            }

            int sharedAABBCount = outputsSharingAABB.Count;
            if (sharedAABBCount > 0)
            {
                uint sharedDecimationFactor = outputsSharingAABB[0].GetRaytracingDecimationFactor();
                uint aabbBufferCount = (capacity + sharedDecimationFactor - 1) / sharedDecimationFactor;
                sharedAabbBufferIndex = outBufferDescs.Count;
                outBufferDescs.Add(new VFXGPUBufferDesc() { debugName = "VFXAabbBuffer", target = GraphicsBuffer.Target.Structured, size = 0u, stride = 24 });
                systemBufferMappings.Add(new VFXMapping("aabbBuffer", sharedAabbBufferIndex));
                sharedAabbCount = aabbBufferCount;
            }

            int outputId = 0;
            foreach (var output in listOutputsOwningAABB)
            {
                uint aabbBufferCount = (capacity + output.GetRaytracingDecimationFactor() - 1) / output.GetRaytracingDecimationFactor();
                int bufferIndex = outBufferDescs.Count;
                outputsOwningAABB.Add(output, bufferIndex);
                outputAabbSize.Add(output, aabbBufferCount);
                outBufferDescs.Add(new VFXGPUBufferDesc() { debugName = "VFXAabbBuffer" + outputId, target = GraphicsBuffer.Target.Structured, size = 0u, stride = 24 });
                systemBufferMappings.Add(new VFXMapping("aabbBuffer" + outputId++, bufferIndex));
            }
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

        private bool NeedsStripData(VFXContext context)
        {
            bool needsStripData = false;

            if (context.ownedType == VFXDataType.ParticleStrip)
            {
                needsStripData = true;
            }
            else if (context is VFXAbstractParticleOutput output)
            {
                needsStripData = output.HasStripsData();
            }

            return needsStripData;
        }

        public override void FillDescs(
            IVFXErrorReporter reporter,
            VFXCompilationMode compilationMode,
            List<VFXGPUBufferDesc> outBufferDescs,
            List<VFXTemporaryGPUBufferDesc> outTemporaryBufferDescs,
            List<VFXEditorSystemDesc> outSystemDescs,
            VFXExpressionGraph expressionGraph,
            VFXCompiledData compiledData,
            IEnumerable<VFXContext> compilableContexts,
            Dictionary<VFXContext, int> contextSpawnToBufferIndex,
            VFXDependentBuffersData dependentBuffers,
            Dictionary<VFXContext, List<VFXContextLink>[]> effectiveFlowInputLinks,
            Dictionary<VFXData, uint> dataToSystemIndex,
            VFXSystemNames systemNames = null)
        {
            bool hasKill = IsAttributeStored(VFXAttribute.Alive);

            var deadListBufferIndex = -1;

            var systemBufferMappings = new List<VFXMapping>();
            var systemValueMappings = new List<VFXMapping>();

            var attributeBufferIndex = dependentBuffers.attributeBuffers[this];

            int attributeSourceBufferIndex = -1;
            int eventGPUFrom = -1;

            var stripDataIndex = -1;

            int instancingIndirectAndActiveIndirectBufferIndex = -1;

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
            if (m_ownAttributeSourceBuffer)
            {
                if (attributeSourceBufferIndex != -1)
                {
                    throw new InvalidOperationException("Unexpected source while filling description of data particle");
                }

                attributeSourceBufferIndex = outBufferDescs.Count;
                outBufferDescs.Add(m_layoutAttributeSource.GetBufferDesc(staticSourceCount, ComputeBufferMode.Dynamic));
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
                    outBufferDescs.Add(new VFXGPUBufferDesc() { debugName = "VFXDeadList", target = GraphicsBuffer.Target.Structured, size = capacity + 2, stride = 4 }); //capacity + 2 for the two counters
                    systemBufferMappings.Add(new VFXMapping("deadList", deadListBufferIndex));
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
                instancingIndirectAndActiveIndirectBufferIndex = outBufferDescs.Count;
                outBufferDescs.Add(new VFXGPUBufferDesc()); // description will be filled at the end when knowning split descs size.
                systemBufferMappings.Add(new VFXMapping("instancingIndirectAndActiveIndirect", instancingIndirectAndActiveIndirectBufferIndex));
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

            if (space == VFXSpace.World)
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
                var firstTaskOfContext = compiledData.contextToCompiledData[m_Contexts.First()].tasks.First();
                var mapper = compiledData.taskToCompiledData[firstTaskOfContext].cpuMapper;

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

            HashSet<VFXContext> indirectOutputHasTaskDependency = null;

            bool needsIndirectBuffer = NeedsIndirectBuffer();
            int globalIndirectBufferIndex = -1;
            bool needsGlobalIndirectBuffer = false;
            if (needsIndirectBuffer)
            {
                indirectOutputHasTaskDependency = new();

                // First link the tasks that output an indirect buffer to the particle output
                foreach (var output in m_Contexts.OfType<VFXAbstractParticleOutput>())
                {
                    var outputTasks = compiledData.contextToCompiledData[output].tasks;

                    // Find the VFXTask providing the indirect buffer to the output.
                    // In case the Output has multiple tasks and one of them uses an indirect buffer, it has the priority over the OutputUpate

                    foreach (var task in Enumerable.Reverse(outputTasks))
                    {
                        if ((task.type & VFXTaskType.Update) != 0)
                        {
                            if (task.needsIndirectBuffer)
                            {
                                indirectOutputHasTaskDependency.Add(output);
                                break;
                            }
                        }
                    }
                }

                foreach (var cullCompute in m_Contexts.OfType<VFXOutputUpdate>())
                    if (cullCompute.HasFeature(VFXOutputUpdate.Features.IndirectDraw))
                    {
                        if (cullCompute.output.contextType != VFXContextType.Output)
                            throw new Exception("Context types expect to be an output.");

                        // If the has only one output task, then we find the last task using the indirect buffer in the OutputUpdate
                        if (!indirectOutputHasTaskDependency.Contains(cullCompute.output))
                        {
                            var indirectOutputUpdateTask = Enumerable.Reverse(compiledData.contextToCompiledData[cullCompute].tasks).FirstOrDefault(t => t.needsIndirectBuffer);
                            if (indirectOutputUpdateTask == null)
                                throw new InvalidOperationException("The type " + cullCompute + " did not return a task using indirect buffer even though the output requires it.");
                            indirectOutputHasTaskDependency.Add(cullCompute.output);
                        }
                    }

                needsGlobalIndirectBuffer = NeedsGlobalIndirectBuffer();
                if (needsGlobalIndirectBuffer)
                {
                    globalIndirectBufferIndex = outBufferDescs.Count;
                    systemBufferMappings.Add(new VFXMapping("indirectBuffer0", outBufferDescs.Count));
                    outBufferDescs.Add(new VFXGPUBufferDesc() { debugName = "VFXIndirectBuffer0", target = GraphicsBuffer.Target.Structured, size = capacity + 1, stride = 4 });
                }
            }

            // Flatten all the buffer descriptors
            List<VFXContextBufferDescriptor> bufferDescriptorList = new();
            foreach (var context in m_Contexts)
            {
                foreach (var buffer in compiledData.contextToCompiledData[context].buffers)
                    bufferDescriptorList.Add(buffer);
            }

            Dictionary<string, int> bufferNameToIndex = new();
            Dictionary<VFXTask, int> taskGroups = new(); // Identify groups of tasks that shares the same buffers
            int groupIndex = 0;
            foreach (var cullCompute in m_Contexts.OfType<VFXOutputUpdate>())
                if (cullCompute.HasFeature(VFXOutputUpdate.Features.IndirectDraw))
                {
                    // Gather all the tasks in the update + output and assign them to the same group so they don't share buffers with another pair.
                    foreach (var task in compiledData.contextToCompiledData[cullCompute.output].tasks)
                        taskGroups[task] = groupIndex;
                    foreach (var task in compiledData.contextToCompiledData[cullCompute].tasks)
                        taskGroups[task] = groupIndex;

                    groupIndex++;
                }

            if (needsIndirectBuffer)
            {
                // Assign a task group to tasks coming from an output without VFXOutputUpdate but that have an indirect buffer
                foreach (var output in indirectOutputHasTaskDependency)
                {
                    bool incrementGroupIndex = false;
                    foreach (var task in compiledData.contextToCompiledData[output].tasks)
                    {
                        if (taskGroups.ContainsKey(task)) //output already handled through its VFXOutputUpdate above.
                            break;
                        incrementGroupIndex = true;
                        taskGroups[task] = groupIndex;
                    }

                    if (incrementGroupIndex)
                        groupIndex++;
                }
            }

            // Allocate the buffers based on their binding order in the tasks
            uint prefixIndex = 0;
            foreach (var context in m_Contexts)
            {
                foreach (var task in compiledData.contextToCompiledData[context].tasks)
                {
                    foreach (var bufferMapping in task.bufferMappings)
                    {
                        var bufferDescriptors = compiledData.contextToCompiledData[context].buffers;
                        if (context is VFXAbstractParticleOutput particleOutput)
                            foreach (var outputUpdate in m_Contexts.OfType<VFXOutputUpdate>().Where(o => o.output == particleOutput))
                                bufferDescriptors.AddRange(compiledData.contextToCompiledData[outputUpdate].buffers);

                        // Add fallback descriptors at the end of the list in case a pass needs a buffer not declared in output update or output
                        bufferDescriptors.AddRange(bufferDescriptorList);

                        // Find the buffer descriptor from it's name:
                        var bufferDescriptor = bufferDescriptors.FirstOrDefault(b => b.baseName == bufferMapping.bufferName);
                        if (bufferDescriptor.baseName == null)
                            continue;

                        string name = bufferDescriptor.baseName;
                        if (taskGroups.TryGetValue(task, out var taskGroupIndex))
                            name += taskGroupIndex;

                        if (bufferNameToIndex.ContainsKey(name))
                            continue;

                        bufferNameToIndex[name] = outBufferDescs.Count;
                        for (int i = 0; i < bufferDescriptor.bufferCount; i++)
                        {
                            string bufferName = $"{bufferDescriptor.baseName + i}_{VFXCodeGeneratorHelper.GeneratePrefix(prefixIndex++)}";
                            if (bufferDescriptor.isPerCamera)
                                bufferName += "PerCamera";
                            if (bufferDescriptor.includeInSystemMappings)
                                systemBufferMappings.Add(new VFXMapping(bufferName, outBufferDescs.Count));

                            uint size;
                            switch (bufferDescriptor.bufferSizeMode)
                            {
                                default:
                                case VFXContextBufferSizeMode.ScaleWithCapacity:
                                    size = (uint)Math.Ceiling(capacity * (double)bufferDescriptor.capacityScaleMultiplier);
                                    break;
                                case VFXContextBufferSizeMode.FixedSize:
                                    size = bufferDescriptor.size;
                                    break;
                                case VFXContextBufferSizeMode.FixedSizePlusScaleWithCapacity:
                                    size = (uint)Math.Ceiling(capacity * (double)bufferDescriptor.capacityScaleMultiplier) + bufferDescriptor.size;
                                    break;
                            }

                            outBufferDescs.Add(new VFXGPUBufferDesc
                            {
                                debugName = bufferName,
                                target = bufferDescriptor.bufferTarget,
                                size = size,
                                stride = bufferDescriptor.stride
                            });
                        }
                    }
                }
            }

            int GetBufferIndex(VFXTask task, string baseName)
            {
                if (taskGroups.TryGetValue(task, out var taskGroupIndex))
                    baseName += taskGroupIndex;

                bufferNameToIndex.TryGetValue(baseName, out int index);
                return index;
            }

            // Duplicate indirect buffers in case there are mulitple outputs


            int graphValuesBufferIndex = -1;
            int instancesPrefixSumBufferIndex = -1;
            int spawnBufferIndex = -1;
            if (hasInstancing)
            {
                FillGraphValuesBuffers(outBufferDescs, systemBufferMappings, m_GraphValuesLayout, out graphValuesBufferIndex);

                if (eventGPUFrom != -1)
                {
                    // For GPU events, take the prefix sum from the same buffer as the events
                    instancesPrefixSumBufferIndex = eventGPUFrom;
                }
                else
                {
                    FillPrefixSumBuffers(outBufferDescs, systemBufferMappings, staticSourceCount,
                        out instancesPrefixSumBufferIndex,
                        out spawnBufferIndex);
                }
            }

            // sort buffers
            int sortBufferAIndex = -1;
            int sortBufferBIndex = -1;
            bool needsGlobalSort = NeedsGlobalSort();
            if (needsGlobalSort)
            {
                sortBufferAIndex = outBufferDescs.Count;
                sortBufferBIndex = sortBufferAIndex + 1;

                outBufferDescs.Add(new VFXGPUBufferDesc() { debugName = "VFXSortBufferA", target = GraphicsBuffer.Target.Structured, size = capacity + 1, stride = 8 });
                systemBufferMappings.Add(new VFXMapping("sortBufferA", sortBufferAIndex));

                outBufferDescs.Add(new VFXGPUBufferDesc() { debugName = "VFXSortBufferB", target = GraphicsBuffer.Target.Structured, size = capacity + 1, stride = 8 });
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
                    outTemporaryBufferDescs.Add(new VFXTemporaryGPUBufferDesc() { frameCount = 2u, desc = new VFXGPUBufferDesc { debugName = "VFXMovecsBuffer", target = GraphicsBuffer.Target.Raw, size = capacity * sizePerElement, stride = 4 } });
                    elementToVFXBufferMotionVector.Add(context.output, currentElementToVFXBufferMotionVector);
                }
            }

            PrepareAABBBuffers(out List<VFXAbstractParticleOutput> outputsSharingAABB,
                out Dictionary<VFXAbstractParticleOutput, int> outputsOwningAABB,
                out Dictionary<VFXAbstractParticleOutput, uint> outputAabbSize,
                out int sharedAabbBufferIndex,
                out uint sharedAabbSize,
                ref systemBufferMappings,
                ref outBufferDescs);

            bool hasAnyRaytraced = outputsSharingAABB.Any() || outputsOwningAABB.Any();
            if (hasAnyRaytraced)
                systemFlag |= VFXSystemFlag.SystemIsRayTraced;


            var taskDescs = new List<VFXEditorTaskDesc>();
            var bufferMappings = new List<VFXMapping>();
            var uniformMappings = new List<VFXMapping>();
            var additionalParameters = new List<VFXMapping>();
            var instanceSplitDescs = new List<VFXInstanceSplitDesc>();

            AddInstanceSplitDesc(instanceSplitDescs, new List<uint>());

			List<(VFXContext context, VFXTask task, VFXContextCompiledData contextCompiledData, long sortKey)> sortedTaskList = new();

            for (int i = 0; i < m_Contexts.Count; ++i)
            {
                var context = m_Contexts[i];
                var contextCompiledData = compiledData.contextToCompiledData[context];

                var tasks = contextCompiledData.tasks;

                if (!tasks.Any(t => compiledData.taskToCompiledData.ContainsKey(t)))
                    throw new InvalidOperationException("Unexpected context which hasn't been compiled : " + context);

                foreach (var task in tasks)
                {
                    long genericType = (int)task.type & 0xF0000000;
                    long sortKey = (genericType << 32) | (uint)i;
                    sortedTaskList.Add((context, task, contextCompiledData, sortKey));
                }
            }

            // Ensures that the outputs are always after all the per camera update tasks while keeping the original declaration order of the contexts
            sortedTaskList = sortedTaskList.OrderBy(t => t.sortKey).ToList();
            m_ContextsToTaskIndex.Clear();

            foreach (var (context, task, contextCompiledData, contextIndex) in sortedTaskList)
            {
                var temporaryBufferMappings = new List<VFXMappingTemporary>();

                bufferMappings.Clear();
                additionalParameters.Clear();

                if (context is VFXOutputUpdate update)
                {
                    if (update.HasFeature(VFXOutputUpdate.Features.MotionVector))
                    {
                        var currentIndex = elementToVFXBufferMotionVector[update.output];
                        temporaryBufferMappings.Add(new VFXMappingTemporary() { pastFrameIndex = 0u, perCameraBuffer = true, mapping = new VFXMapping("elementToVFXBuffer", currentIndex) });
                    }
                    if (update.HasFeature(VFXOutputUpdate.Features.FillRaytracingAABB) && outputsOwningAABB.ContainsKey(update.output))
                    {
                        bufferMappings.Add(new VFXMapping("aabbBuffer", outputsOwningAABB[update.output]));
                        additionalParameters.Add(new VFXMapping("aabbBufferCount", (int)outputAabbSize[update.output]));
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
                    bufferMappings.Add(new VFXMapping("deadList", deadListBufferIndex));

                if (attributeSourceBufferIndex != -1 && context.contextType == VFXContextType.Init)
                    bufferMappings.Add(new VFXMapping("sourceAttributeBuffer", attributeSourceBufferIndex));

                if (stripDataIndex != -1 && NeedsStripData(context))
                    bufferMappings.Add(new VFXMapping("stripDataBuffer", stripDataIndex));

                if (sharedAabbBufferIndex != -1 && (context.contextType == VFXContextType.Update ||
                                                    outputsSharingAABB.Contains(context)))
                {
                    bufferMappings.Add(new VFXMapping("aabbBuffer", sharedAabbBufferIndex));
                    additionalParameters.Add(new VFXMapping("aabbBufferCount", (int)sharedAabbSize));
                }

                if (context is VFXAbstractParticleOutput output && outputsOwningAABB.ContainsKey(output))
                {
                    bufferMappings.Add(new VFXMapping("aabbBuffer", outputsOwningAABB[output]));
                    additionalParameters.Add(new VFXMapping("aabbBufferCount", (int)outputAabbSize[output]));
                }

                if (context.contextType == VFXContextType.Init)
                {
                    if(spawnBufferIndex != -1)
                        bufferMappings.Add(new VFXMapping("spawnBuffer", spawnBufferIndex));
                }

                if (hasInstancing)
                {
                    bool needsInstancePrefixSum = context.contextType == VFXContextType.Init;
                    needsInstancePrefixSum |= !hasKill && task.doesGenerateShader && task.shaderType == VFXTaskShaderType.ComputeShader;
                    if (instancesPrefixSumBufferIndex != -1 && needsInstancePrefixSum)
                        bufferMappings.Add(new VFXMapping("instancingPrefixSum", instancesPrefixSumBufferIndex));

                    bool mapIndirectBuffers = contextCompiledData.tasks.Any(t => (t.type & (VFXTaskType.Update | VFXTaskType.Initialize)) != 0);
                    mapIndirectBuffers |= (context.contextType & (VFXContextType.Init | VFXContextType.Update | VFXContextType.Filter)) != 0;

                    if (mapIndirectBuffers)
                    {
                        if (instancingIndirectAndActiveIndirectBufferIndex != -1)
                            bufferMappings.Add(new VFXMapping("instancingIndirectAndActiveIndirect", instancingIndirectAndActiveIndirectBufferIndex));
                    }
                }

                if (hasAttachedStrip)
                {
                    bufferMappings.Add(new VFXMapping("attachedStripDataBuffer", dependentBuffers.stripBuffers[attachedStripData]));
                }

                if (needsIndirectBuffer && task.needsIndirectBuffer)
                {
                    systemFlag |= VFXSystemFlag.SystemHasIndirectBuffer;

                    if ((task.type & VFXTaskType.Output) != 0 && context is VFXAbstractParticleOutput outputContext && outputContext.HasIndirectDraw())
                    {
                        bool hasUpdateTask = indirectOutputHasTaskDependency.Contains(context);

                        int sortBufferIndex = hasUpdateTask ? (outputContext.HasSorting() ? GetBufferIndex(task, k_SortedIndirectBufferName) : GetBufferIndex(task, k_IndirectBufferName)) : globalIndirectBufferIndex;
                        int indirectBufferIndex = hasUpdateTask ? GetBufferIndex(task, k_IndirectBufferName) : globalIndirectBufferIndex;
                        additionalParameters.Add(new VFXMapping("indirectIndex", indirectBufferIndex == -1 ? 0 : indirectBufferIndex));
                        bufferMappings.Add(new VFXMapping(k_IndirectBufferName, sortBufferIndex == -1 ? 0 : sortBufferIndex));
                    }
                }

                if (context.contextType == VFXContextType.Update)
                {
                    if (context.taskType == VFXTaskType.Update && needsGlobalIndirectBuffer)
                        bufferMappings.Add(new VFXMapping(k_IndirectBufferName, globalIndirectBufferIndex));
                }

                if (context.contextType == VFXContextType.Filter)
                {
                    if (context.taskType == VFXTaskType.GlobalSort && needsGlobalIndirectBuffer)
                        bufferMappings.Add(new VFXMapping("inputBuffer", globalIndirectBufferIndex));
                }

                // Generate task mappings from the required buffers
                foreach (var map in task.bufferMappings)
                {
                    int index = GetBufferIndex(task, map.bufferName);
                    if (index == -1)
                        continue;

                    if (map.useBufferCountIndexInName && context is VFXOutputUpdate outputUpdate)
                    {
                        for (int j = 0; j < outputUpdate.bufferCount; j++) // TODO: we can refactor this by generating the list of buffers directly in the PrepareCompiledData()
                            bufferMappings.Add(new VFXMapping(map.mappingName + j, index + j));
                    }
                    else
                    {
                        // Check for duplicated mapping
                        if (bufferMappings.All(m => m.name != map.mappingName))
                            bufferMappings.Add(new VFXMapping(map.mappingName, index));
                    }
                }
                if (deadListBufferIndex != -1 && context.contextType == VFXContextType.Output && (context as VFXAbstractParticleOutput).NeedsDeadListCount())
                    bufferMappings.Add(new VFXMapping("deadList", deadListBufferIndex));

                if (context.taskType == VFXTaskType.GlobalSort)
                {
                    bufferMappings.Add(new VFXMapping("outputBuffer", sortBufferAIndex));
                }

                var contextData = compiledData.taskToCompiledData[task];
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
                        reporter?.RegisterError("GPUNodeLinkedTOCPUSlot", VFXErrorType.Error, "Can not link a GPU operator to a system wide (CPU) input.", context.GetSlotByPath(true, mapping.name));
                        throw new InvalidOperationException("Can not link a GPU operator to a system wide (CPU) input: " + mapping.name);
                    }
                }

                var taskDesc = new VFXEditorTaskDesc();
                taskDesc.type = (UnityEngine.VFX.VFXTaskType)task.type;

                taskDesc.buffers = bufferMappings.ToArray();
                taskDesc.temporaryBuffers = temporaryBufferMappings.ToArray();
                taskDesc.values = uniformMappings.OrderBy(mapping => mapping.index).ToArray();
                taskDesc.parameters = cpuMappings.Concat(contextData.parameters).Concat(additionalParameters).ToArray();
                taskDesc.instanceSplitIndex = AddInstanceSplitDesc(instanceSplitDescs, instancingSplitDescValues);
                taskDesc.shaderSourceIndex = compiledData.taskToCompiledData[task].indexInShaderSource;
                taskDesc.model = context;
                taskDesc.usesMaterialVariant = compilationMode == VFXCompilationMode.Edition && context.usesMaterialVariantInEditMode;

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
                        AddTaskDesc(taskDescs, singleMeshTaskDesc, context);
                    }
                }
                else
                {
                    AddTaskDesc(taskDescs, taskDesc, context);
                }

                // if task is a per output update with sorting, add sort tasks
                // TODO: Replace this hardcoded pass by a task in the OutputUpdate context.
                if (context is VFXOutputUpdate outUpdate)
                {
                    if (outUpdate.HasFeature(VFXOutputUpdate.Features.CameraSort) || outUpdate.HasFeature(VFXOutputUpdate.Features.Sort))
                    {
                        for (int j = 0; j < outUpdate.bufferCount; ++j)
                        {
                            VFXEditorTaskDesc sortTaskDesc = new VFXEditorTaskDesc();
                            sortTaskDesc.type = UnityEngine.VFX.VFXTaskType.PerOutputSort;
                            sortTaskDesc.externalProcessor = null;
                            sortTaskDesc.model = context;

                            sortTaskDesc.buffers = new VFXMapping[3];
                            sortTaskDesc.buffers[0] = new VFXMapping("srcBuffer", GetBufferIndex(task, k_IndirectBufferName) + j);
                            if (capacity > 4096) // Add scratch buffer
                            {
                                sortTaskDesc.buffers[1] = new VFXMapping("scratchBuffer", outBufferDescs.Count);
                                outBufferDescs.Add(new VFXGPUBufferDesc() { debugName = "VFXScratchSortBuffer", target = GraphicsBuffer.Target.Structured, size = capacity + 1, stride = 8 });
                            }
                            else
                                sortTaskDesc.buffers[1] = new VFXMapping("scratchBuffer", -1); // No scratchBuffer needed
                            sortTaskDesc.buffers[2] = new VFXMapping("dstBuffer", GetBufferIndex(task, k_SortedIndirectBufferName) + j);

                            sortTaskDesc.parameters = new VFXMapping[2];
                            sortTaskDesc.parameters[0] = new VFXMapping("globalSort", 0);
                            sortTaskDesc.parameters[1] = new VFXMapping("isPerCameraSort", outUpdate.isPerCamera ? 1 : 0);

                            AddTaskDesc(taskDescs, sortTaskDesc, outUpdate.output);
                        }
                    }
                }
            }

            outBufferDescs[instancingIndirectAndActiveIndirectBufferIndex] = new VFXGPUBufferDesc() { debugName = "VFXInstancesIndirectionBuffer", target = GraphicsBuffer.Target.Structured, size = 1u + (uint)instanceSplitDescs.Count() , stride = 4, mode = ComputeBufferMode.Dynamic };

            if (instancesPrefixSumBufferIndex != -1 && eventGPUFrom == -1) // only if we have a prefix sum and we are not reusing the GPU event buffer
            {
                outBufferDescs[instancesPrefixSumBufferIndex] = new VFXGPUBufferDesc() { debugName = "VFXInstancesPrefixSumBuffer", target = GraphicsBuffer.Target.Structured, size = (uint)instanceSplitDescs.Count() + 1u, stride = 4, mode = ComputeBufferMode.Dynamic };
            }

            if (hasStrip && hasKill)
            {
                var lastUpdateContext = m_Contexts.OfType<VFXBasicUpdate>().LastOrDefault();
                if (lastUpdateContext != null)
                {
                    if (m_ContextsToTaskIndex.TryGetValue(lastUpdateContext, out List<TaskProfilingData> tasksIndices))
                    {
                        TaskProfilingData taskProfilingData = new TaskProfilingData()
                            { taskIndex = taskDescs.Count, taskName = "Update Strips" };
                        tasksIndices.Add(taskProfilingData);
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

        void AddTaskDesc(List<VFXEditorTaskDesc> taskDescs, VFXEditorTaskDesc taskDesc, VFXContext context)
        {
            TaskProfilingData taskProfilingData = new TaskProfilingData()
                { taskIndex = taskDescs.Count, taskName = taskDesc.type.ToString() };

            VFXContext visualContext;
            if (context is VFXOutputUpdate outputUpdate)
            {
                visualContext = outputUpdate.output;
            }
            else if (context is VFXGlobalSort)
            {
                visualContext = m_ContextsToTaskIndex.Keys.FirstOrDefault(o => o is VFXBasicUpdate);
            }
            else
            {
                visualContext = context;
            }

            if (m_ContextsToTaskIndex.TryGetValue(visualContext, out List<TaskProfilingData> tasksIndices))
            {
                tasksIndices.Add(taskProfilingData);
            }
            else
            {
                m_ContextsToTaskIndex.Add(visualContext, new List<TaskProfilingData>() {taskProfilingData});
            }
            taskDescs.Add(taskDesc);
        }

        private void FillGraphValuesBuffers(List<VFXGPUBufferDesc> outBufferDescs, List<VFXMapping> systemBufferMappings, GraphValuesLayout graphValuesLayout, out int graphValuesIndex)
        {
            var graphValuesSize = graphValuesLayout.paddedSizeInBytes / 4;
            graphValuesIndex = outBufferDescs.Count;
            outBufferDescs.Add(new VFXGPUBufferDesc()
            {
                debugName = "VFXGraphValuesBuffer",
                target = GraphicsBuffer.Target.Raw,
                size = graphValuesSize,
                stride = 4u,
                mode = ComputeBufferMode.Dynamic
            });
            systemBufferMappings.Add(new VFXMapping("graphValuesBuffer", graphValuesIndex));
        }

        private static void FillPrefixSumBuffers(List<VFXGPUBufferDesc> outBufferDescs, List<VFXMapping> systemBufferMappings, uint staticSourceCount,
            out int instancesPrefixSumBufferIndex,
            out int spawnBufferIndex)
        {
            instancesPrefixSumBufferIndex = outBufferDescs.Count;
            outBufferDescs.Add(new VFXGPUBufferDesc() { debugName = "VFXInstancingPrefixSumBuffer" });  // description will be filled at the end when knowning split descs size.
            systemBufferMappings.Add(new VFXMapping("instancingPrefixSum", instancesPrefixSumBufferIndex));

            spawnBufferIndex = outBufferDescs.Count;
            uint spawnCountSize = Math.Max(staticSourceCount, 1u);
            outBufferDescs.Add(new VFXGPUBufferDesc() { debugName = "VFXSpawnEventBuffer", target = GraphicsBuffer.Target.Structured, size = spawnCountSize + 1u, stride = 4,  mode = ComputeBufferMode.Dynamic });
            systemBufferMappings.Add(new VFXMapping("spawnBuffer", spawnBufferIndex));
        }

        public override void OnEnable()
        {
            base.OnEnable();
            if ((int)m_Space == int.MaxValue)
            {
                m_Space = VFXSpace.None;
            }
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

            if (version < 12 && (int)m_Space == int.MaxValue)
            {
                m_Space = VFXSpace.None;
                Debug.LogError("Unexpected space none detected in VFXDataParticle");
            }

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
        private VFXSpace m_Space; // TODO Should be an actual setting
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
            private static readonly int kContextDataOffset = 16;
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
                int currentOffset = kContextDataOffset;
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

        public void GenerateSystemUniformMapper(VFXExpressionGraph graph, VFXCompiledData compiledData, ref Dictionary<VFXContext, VFXExpressionMapper> gpuMappers)
        {
            VFXUniformMapper uniformMapper = null;
            foreach (var context in m_Contexts)
            {
                var gpuMapper = graph.BuildGPUMapper(context);
                gpuMappers[context] = gpuMapper;
                var contextUniformMapper = new VFXUniformMapper(gpuMapper, context.doesGenerateShader, true);

                // SG inputs if needed
                var shaderGraph = VFXShaderGraphHelpers.GetShaderGraph(context);
                VFXSGInputs contextSGInputs = null;
                if (shaderGraph)
                {
                    var firstTaskOfContext = compiledData.contextToCompiledData[context].tasks.First();
                    var cpuMapper = compiledData.taskToCompiledData[firstTaskOfContext].cpuMapper;

                    contextSGInputs = new VFXSGInputs(cpuMapper, gpuMapper, contextUniformMapper, shaderGraph);
                    if (contextSGInputs.IsEmpty())
                        contextSGInputs = null;
                }

                // Add gpu and uniform mapper
                foreach (var task in compiledData.contextToCompiledData[context].tasks)
                {
                    var taskData = compiledData.taskToCompiledData[task];
                    taskData.gpuMapper = gpuMapper;
                    taskData.uniformMapper = contextUniformMapper;
                    taskData.SGInputs = contextSGInputs;

                    compiledData.taskToCompiledData[task] = taskData;

                    if (uniformMapper == null)
                        uniformMapper = new VFXUniformMapper(gpuMapper, true, true);
                    else
                    {
                        uniformMapper.AppendMapper(gpuMapper);
                    }
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

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);

            if (boundsMode == BoundsSettingMode.Automatic)
            {
                if (CanBeCompiled())
                    report.RegisterError("WarningAutomaticBoundsFlagChange", VFXErrorType.Warning,
                        $"Changing the bounds mode to Automatic modifies the Culling Flags on the Visual Effect Asset to Always recompute bounds and simulate.", this);
            }
        }
    }
}
