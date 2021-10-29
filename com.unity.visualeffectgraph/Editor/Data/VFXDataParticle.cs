using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor.VFX;
using UnityEngine.VFX;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    interface ILayoutProvider
    {
        void GenerateAttributeLayout(uint capacity, Dictionary<VFXAttribute, int> storedAttribute);
        string GetCodeOffset(VFXAttribute attrib, string index);
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

        public string GetCodeOffset(VFXAttribute attrib, string index)
        {
            AttributeLayout layout;
            if (!m_AttributeLayout.TryGetValue(attrib, out layout))
            {
                throw new InvalidOperationException(string.Format("Cannot find attribute {0}", attrib.name));
            }
            return string.Format("({2} * 0x{0:X} + 0x{1:X}) << 2", m_BucketSizes[layout.bucket], m_BucketOffsets[layout.bucket] + layout.offset, index);
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
                    ctx.RefreshErrors(GetGraph());
                }

                if (boundsMode == BoundsSettingMode.Automatic)
                {
                    needsComputeBounds = true;
                    var graph = GetGraph();
                    graph.visualEffectResource.cullingFlags = VFXCullingFlags.CullNone;
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

        private uint alignedCapacity
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
            var index = location == VFXAttributeLocation.Current ? "index" : "sourceIndex";

            if (location == VFXAttributeLocation.Current && !m_StoredCurrentAttributes.ContainsKey(attrib))
                throw new ArgumentException(string.Format("Attribute {0} does not exist in data layout", attrib.name));

            if (location == VFXAttributeLocation.Source && !m_ReadSourceAttributes.Any(a => a.name == attrib.name))
                throw new ArgumentException(string.Format("Attribute {0} does not exist in data layout", attrib.name));

            return string.Format("{0}({3}.Load{1}({2}))", GetCastAttributePrefix(attrib), GetByteAddressBufferMethodSuffix(attrib), attributeStore.GetCodeOffset(attrib, index), attributeBuffer);
        }

        public override string GetStoreAttributeCode(VFXAttribute attrib, string value)
        {
            if (!m_StoredCurrentAttributes.ContainsKey(attrib))
                throw new ArgumentException(string.Format("Attribute {0} does not exist in data layout", attrib.name));

            return string.Format("attributeBuffer.Store{0}({1},{3}({2}))", GetByteAddressBufferMethodSuffix(attrib), m_layoutAttributeCurrent.GetCodeOffset(attrib, "index"), value, attrib.type == VFXValueType.Boolean ? "uint" : "asuint");
        }

        public override IEnumerable<VFXContext> InitImplicitContexts()
        {
            var contexts = compilableOwners.ToList();

            if (!NeedsGlobalSort() &&
                !contexts.OfType<VFXAbstractParticleOutput>().Any(o => o.NeedsOutputUpdate()))
            {
                //Early out with the most common case
                m_Contexts = contexts;
                return Enumerable.Empty<VFXContext>();
            }

            m_Contexts = new List<VFXContext>(contexts.Count + 2); // Allocate max number
            int index = 0;

            // First add init and updates
            for (index = 0; index < contexts.Count; ++index)
            {
                if ((contexts[index].contextType == VFXContextType.Output))
                    break;
                m_Contexts.Add(contexts[index]);
            }

            var implicitContext = new List<VFXContext>();
            if (NeedsGlobalSort())
            {
                // Then the camera sort
                var cameraSort = VFXContext.CreateImplicitContext<VFXCameraSort>(this);
                implicitContext.Add(cameraSort);
                m_Contexts.Add(cameraSort);
            }

            //additional update
            for (int outputIndex = index; outputIndex < contexts.Count; ++outputIndex)
            {
                var currentOutputContext = contexts[outputIndex];
                var abstractParticleOutput = currentOutputContext as VFXAbstractParticleOutput;
                if (abstractParticleOutput == null)
                    continue;

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
            return compilableOwners.OfType<VFXAbstractParticleOutput>().Any(o => o.CanBeCompiled() && o.HasSorting() && !VFXOutputUpdate.HasFeature(o.outputUpdateFeatures, VFXOutputUpdate.Features.IndirectDraw));
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
            VFXSystemNames systemNames = null)
        {
            bool hasKill = IsAttributeStored(VFXAttribute.Alive);

            var deadListBufferIndex = -1;
            var deadListCountIndex = -1;

            var systemBufferMappings = new List<VFXMapping>();
            var systemValueMappings = new List<VFXMapping>();

            var attributeBufferIndex = dependentBuffers.attributeBuffers[this];

            int attributeSourceBufferIndex = -1;
            int eventGPUFrom = -1;

            var stripDataIndex = -1;
            var boundsBufferIndex = -1;

            if (m_DependenciesIn.Any())
            {
                if (m_DependenciesIn.Count != 1)
                {
                    throw new InvalidOperationException("Unexpected multiple input dependency for GPU event");
                }
                attributeSourceBufferIndex = dependentBuffers.attributeBuffers[m_DependenciesIn.FirstOrDefault()];
                eventGPUFrom = dependentBuffers.eventBuffers[this];
            }

            if (attributeBufferIndex != -1)
            {
                systemBufferMappings.Add(new VFXMapping("attributeBuffer", attributeBufferIndex));
            }

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

            var systemFlag = VFXSystemFlag.SystemDefault;
            if (eventGPUFrom != -1)
            {
                systemFlag |= VFXSystemFlag.SystemReceivedEventGPU;
                systemBufferMappings.Add(new VFXMapping("eventList", eventGPUFrom));
            }

            if (hasKill)
            {
                systemFlag |= VFXSystemFlag.SystemHasKill;

                deadListBufferIndex = outBufferDescs.Count;
                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Counter, size = capacity, stride = 4 });
                systemBufferMappings.Add(new VFXMapping("deadList", deadListBufferIndex));

                deadListCountIndex = outBufferDescs.Count;
                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Raw, size = 1, stride = 4 });
                systemBufferMappings.Add(new VFXMapping("deadListCount", deadListCountIndex));
            }

            if (hasStrip)
            {
                systemFlag |= VFXSystemFlag.SystemHasStrips;

                systemValueMappings.Add(new VFXMapping("stripCount", (int)stripCapacity));
                systemValueMappings.Add(new VFXMapping("particlePerStripCount", (int)particlePerStripCount));

                stripDataIndex = dependentBuffers.stripBuffers[this];
                systemBufferMappings.Add(new VFXMapping("stripDataBuffer", stripDataIndex));
            }

            if (hasDynamicSourceCount)
            {
                systemFlag |= VFXSystemFlag.SystemHasDirectLink;
            }

            if (needsComputeBounds || boundsMode == BoundsSettingMode.Automatic)
            {
                systemFlag |= VFXSystemFlag.SystemNeedsComputeBounds;

                boundsBufferIndex = dependentBuffers.boundsBuffers[this];
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

            var initContext = m_Contexts.FirstOrDefault(o => o.contextType == VFXContextType.Init);
            if (initContext != null)
                systemBufferMappings.AddRange(effectiveFlowInputLinks[initContext].SelectMany(t => t.Select(u => u.context)).Where(o => o.contextType == VFXContextType.Spawner).Select(o => new VFXMapping("spawner_input", contextSpawnToBufferIndex[o])));
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
                    outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Counter, size = capacity, stride = 4 });
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
                            outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Counter, size = capacity, stride = bufferStride });
                        }

                        if (culler.HasFeature(VFXOutputUpdate.Features.Sort))
                        {
                            culler.sortedBufferIndex = outBufferDescs.Count;
                            for (uint i = 0; i < bufferCount; ++i)
                                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Default, size = capacity, stride = 4 });
                        }
                        else
                            culler.sortedBufferIndex = culler.bufferIndex;
                    }
                }
            }

            // sort buffers
            int sortBufferAIndex = -1;
            int sortBufferBIndex = -1;
            bool needsSort = NeedsGlobalSort();
            if (needsSort)
            {
                sortBufferAIndex = outBufferDescs.Count;
                sortBufferBIndex = sortBufferAIndex + 1;

                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Default, size = capacity, stride = 8 });
                systemBufferMappings.Add(new VFXMapping("sortBufferA", sortBufferAIndex));

                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Default, size = capacity, stride = 8 });
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

                if (eventGPUFrom != -1 && context.contextType == VFXContextType.Init)
                    bufferMappings.Add(new VFXMapping("eventList", eventGPUFrom));

                if (deadListBufferIndex != -1 && (context.taskType == VFXTaskType.Initialize || context.taskType == VFXTaskType.Update))
                    bufferMappings.Add(new VFXMapping(context.contextType == VFXContextType.Update ? "deadListOut" : "deadListIn", deadListBufferIndex));

                if (deadListCountIndex != -1 && context.contextType == VFXContextType.Init)
                    bufferMappings.Add(new VFXMapping("deadListCount", deadListCountIndex));

                if (attributeSourceBufferIndex != -1 && context.contextType == VFXContextType.Init)
                    bufferMappings.Add(new VFXMapping("sourceAttributeBuffer", attributeSourceBufferIndex));

                if (stripDataIndex != -1 && context.ownedType == VFXDataType.ParticleStrip)
                    bufferMappings.Add(new VFXMapping("stripDataBuffer", stripDataIndex));

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
                        if (context.taskType == VFXTaskType.CameraSort && needsGlobalIndirectBuffer)
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

                if (context.taskType == VFXTaskType.CameraSort)
                {
                    bufferMappings.Add(new VFXMapping("outputBuffer", sortBufferAIndex));
                    if (deadListCountIndex != -1)
                        bufferMappings.Add(new VFXMapping("deadListCount", deadListCountIndex));
                }

                var gpuTarget = context.allLinkedOutputSlot.SelectMany(o => (o.owner as VFXContext).outputContexts)
                    .Where(c => c.CanBeCompiled())
                    .Select(o => dependentBuffers.eventBuffers[o.GetData()])
                    .ToArray();
                for (uint indexTarget = 0; indexTarget < (uint)gpuTarget.Length; ++indexTarget)
                {
                    var prefix = VFXCodeGeneratorHelper.GeneratePrefix(indexTarget);
                    bufferMappings.Add(new VFXMapping(string.Format("eventListOut_{0}", prefix), gpuTarget[indexTarget]));
                }

                uniformMappings.Clear();

                foreach (var uniform in contextData.uniformMapper.uniforms)
                    uniformMappings.Add(new VFXMapping(contextData.uniformMapper.GetName(uniform), expressionGraph.GetFlattenedIndex(uniform)));
                foreach (var buffer in contextData.uniformMapper.buffers)
                    uniformMappings.Add(new VFXMapping(contextData.uniformMapper.GetName(buffer), expressionGraph.GetFlattenedIndex(buffer)));
                foreach (var texture in contextData.uniformMapper.textures)
                {
                    // TODO At the moment issue all names sharing the same texture as different texture slots. This is not optimized as it required more texture binding than necessary
                    foreach (var name in contextData.uniformMapper.GetNames(texture))
                        uniformMappings.Add(new VFXMapping(name, expressionGraph.GetFlattenedIndex(texture)));
                }

                // Retrieve all cpu mappings at context level (-1)
                var cpuMappings = contextData.cpuMapper.CollectExpression(-1).Select(exp => new VFXMapping(exp.name, expressionGraph.GetFlattenedIndex(exp.exp))).ToArray();

                //Check potential issue with invalid operation on CPU
                foreach (var mapping in cpuMappings)
                {
                    if (mapping.index < 0)
                    {
                        reporter?.RegisterError(context.GetSlotByPath(true, mapping.name), "GPUNodeLinkedTOCPUSlot", VFXErrorType.Error, "Can not link a GPU operator to a system wide (CPU) input."); ;
                        throw new InvalidOperationException("Unable to compute CPU expression for mapping : " + mapping.name);
                    }
                }

                taskDesc.buffers = bufferMappings.ToArray();
                taskDesc.temporaryBuffers = temporaryBufferMappings.ToArray();
                taskDesc.values = uniformMappings.ToArray();
                taskDesc.parameters = cpuMappings.Concat(contextData.parameters).Concat(additionalParameters).ToArray();
                taskDesc.shaderSourceIndex = contextToCompiledData[context].indexInShaderSource;
                taskDesc.model = context;

                if (context is IVFXMultiMeshOutput) // If the context is a multi mesh output, split and patch task desc into several tasks
                {
                    var multiMeshOutput = (IVFXMultiMeshOutput)context;
                    for (int j = (int)multiMeshOutput.meshCount - 1; j >= 0; --j) // Back to front to be consistent with LOD and alpha
                    {
                        VFXEditorTaskDesc singleMeshTaskDesc = taskDesc;
                        singleMeshTaskDesc.parameters = VFXMultiMeshHelper.PatchCPUMapping(taskDesc.parameters, multiMeshOutput.meshCount, j).ToArray();
                        singleMeshTaskDesc.buffers = VFXMultiMeshHelper.PatchBufferMapping(taskDesc.buffers, j).ToArray();
                        taskDescs.Add(singleMeshTaskDesc);
                    }
                }
                else
                    taskDescs.Add(taskDesc);

                // if task is a per camera update with sorting, add sort tasks
                if (context is VFXOutputUpdate)
                {
                    var update = (VFXOutputUpdate)context;

                    if (update.HasFeature(VFXOutputUpdate.Features.Sort))
                    {
                        for (int j = 0; j < update.bufferCount; ++j)
                        {
                            VFXEditorTaskDesc sortTaskDesc = new VFXEditorTaskDesc();
                            sortTaskDesc.type = UnityEngine.VFX.VFXTaskType.PerCameraSort;
                            sortTaskDesc.externalProcessor = null;
                            sortTaskDesc.model = context;

                            sortTaskDesc.buffers = new VFXMapping[3];
                            sortTaskDesc.buffers[0] = new VFXMapping("srcBuffer", update.bufferIndex + j);
                            if (capacity > 4096) // Add scratch buffer
                            {
                                sortTaskDesc.buffers[1] = new VFXMapping("scratchBuffer", outBufferDescs.Count);
                                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Default, size = capacity, stride = 8 });
                            }
                            else
                                sortTaskDesc.buffers[1] = new VFXMapping("scratchBuffer", -1); // No scratchBuffer needed
                            sortTaskDesc.buffers[2] = new VFXMapping("dstBuffer", update.sortedBufferIndex + j);

                            sortTaskDesc.parameters = new VFXMapping[1];
                            sortTaskDesc.parameters[0] = new VFXMapping("globalSort", 0);

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
                type = VFXSystemType.Particle,
                layer = m_Layer
            });
        }

        public override void Sanitize(int version)
        {
            if (version < 8)
            {
                SetSettingValue("boundsMode", BoundsSettingMode.Manual);
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
        private VFXCoordinateSpace m_Space; // TODO Should be an actual setting
        [NonSerialized]
        private StructureOfArrayProvider m_layoutAttributeCurrent = new StructureOfArrayProvider();
        [NonSerialized]
        private StructureOfArrayProvider m_layoutAttributeSource = new StructureOfArrayProvider();
        [NonSerialized]
        private bool m_ownAttributeSourceBuffer;
    }
}
