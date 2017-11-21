using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

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

        public string GetCodeOffset(VFXAttribute attrib, string index)
        {
            AttributeLayout layout = m_AttributeLayout[attrib];
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
                capacity = capacity,
                layout = layout.ToArray()
            };
        }

        private Dictionary<VFXAttribute, AttributeLayout> m_AttributeLayout = new Dictionary<VFXAttribute, AttributeLayout>();
        private List<int> m_BucketSizes = new List<int>();
        private List<int> m_BucketOffsets = new List<int>();
    }

    class VFXDataParticle : VFXData
    {
        public override VFXDataType type { get { return VFXDataType.kParticle; } }

        public uint capacity
        {
            get { return m_Capacity; }
            set
            {
                const uint kThreadPerGroup = 64;
                if (value > kThreadPerGroup)
                    value = (uint)((value + kThreadPerGroup - 1) & ~(kThreadPerGroup - 1)); // multiple of kThreadPerGroup
                m_Capacity = (value + 3u) & ~3u;
            }
        }

        public override uint sourceCount
        {
            get
            {
                var init = owners.FirstOrDefault(o => o.contextType == VFXContextType.kInit);
                return init != null ? (uint)init.inputContexts.Count() : 0u;
            }
        }

        private uint attributeBufferSize
        {
            get
            {
                return m_layoutAttributeCurrent.GetBufferSize(m_Capacity);
            }
        }

        public Bounds bbox
        {
            get { return m_Bounds; }
            set { m_Bounds = value; }
        }

        public bool worldSpace
        {
            get { return m_WorldSpace; }
            set { m_WorldSpace = value; }
        }

        public override void GenerateAttributeLayout()
        {
            m_layoutAttributeCurrent.GenerateAttributeLayout(m_Capacity, m_StoredCurrentAttributes);
            var readSourceAttribute = m_ReadSourceAttributes.ToDictionary(o => o, _ => (int)VFXAttributeMode.ReadSource);
            m_layoutAttributeSource.GenerateAttributeLayout(sourceCount, readSourceAttribute);
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

            return string.Format("attributeBuffer.Store{0}({1},{3}({2}))", GetByteAddressBufferMethodSuffix(attrib), m_layoutAttributeCurrent.GetCodeOffset(attrib, "index"), value, attrib.type == UnityEngine.VFX.VFXValueType.kBool ? "uint" : "asuint");
        }

        public void FillDescs(
            List<VFXGPUBufferDesc> outBufferDescs,
            List<VFXSystemDesc> outSystemDescs,
            VFXExpressionGraph expressionGraph,
            Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData,
            Dictionary<VFXContext, int> contextSpawnToBufferIndex)
        {
            bool hasKill = IsAttributeStored(VFXAttribute.Alive);

            var attributeBufferIndex = -1;
            var attributeSourceBufferIndex = -1;
            var deadListBufferIndex = -1;
            var deadListCountIndex = -1;

            var systemBufferMappings = new List<VFXMapping>();
            var systemValueMappings = new List<VFXMapping>();

            if (attributeBufferSize > 0)
            {
                attributeBufferIndex = outBufferDescs.Count;
                outBufferDescs.Add(m_layoutAttributeCurrent.GetBufferDesc(m_Capacity));
                systemBufferMappings.Add(new VFXMapping(attributeBufferIndex, "attributeBuffer"));
            }

            if (m_layoutAttributeSource.GetBufferSize(sourceCount) > 0u)
            {
                attributeSourceBufferIndex = outBufferDescs.Count;
                var bufferDesc = m_layoutAttributeSource.GetBufferDesc(sourceCount);
                outBufferDescs.Add(bufferDesc);
                systemBufferMappings.Add(new VFXMapping(attributeSourceBufferIndex, "sourceAttributeBuffer"));
            }

            var systemFlag = VFXSystemFlag.kVFXSystemDefault;
            if (hasKill)
            {
                systemFlag |= VFXSystemFlag.kVFXSystemHasKill;

                deadListBufferIndex = outBufferDescs.Count;
                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Append, size = capacity });
                systemBufferMappings.Add(new VFXMapping(deadListBufferIndex, "deadList"));

                deadListCountIndex = outBufferDescs.Count;
                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Raw, size = 1 });
                systemBufferMappings.Add(new VFXMapping(deadListCountIndex, "deadListCount"));
            }

            var initContext = owners.FirstOrDefault(o => o.contextType == VFXContextType.kInit);
            if (initContext != null)
                systemBufferMappings.AddRange(initContext.inputContexts.Select(o => new VFXMapping(contextSpawnToBufferIndex[o], "spawner_input")));
            if (owners.Count() > 0 && owners.First().contextType == VFXContextType.kInit) // TODO This test can be removed once we ensure priorly the system is valid
            {
                var mapper = contextToCompiledData[owners.First()].cpuMapper;

                var boundsCenterExp = mapper.FromNameAndId("bounds_center", -1);
                var boundsSizeExp = mapper.FromNameAndId("bounds_size", -1);

                int boundsCenterIndex = boundsCenterExp != null ? expressionGraph.GetFlattenedIndex(boundsCenterExp) : -1;
                int boundsSizeIndex = boundsSizeExp != null ? expressionGraph.GetFlattenedIndex(boundsSizeExp) : -1;

                if (boundsCenterIndex != -1 && boundsSizeIndex != -1)
                {
                    systemValueMappings.Add(new VFXMapping(boundsCenterIndex, "bounds_center"));
                    systemValueMappings.Add(new VFXMapping(boundsSizeIndex, "bounds_size"));
                }
            }

            var taskDescs = new List<VFXTaskDesc>();
            var bufferMappings = new List<VFXMapping>();
            var uniformMappings = new List<VFXMapping>();

            foreach (var context in owners)
            {
                //if (!contextToCompiledData.ContainsKey(context))
                //    continue;

                var contextData = contextToCompiledData[context];

                var taskDesc = new VFXTaskDesc();
                taskDesc.type = context.taskType;

                bufferMappings.Clear();
                if (attributeBufferIndex != -1)
                    bufferMappings.Add(new VFXMapping(attributeBufferIndex, "attributeBuffer"));
                if (deadListBufferIndex != -1 && context.contextType != VFXContextType.kOutput)
                    bufferMappings.Add(new VFXMapping(deadListBufferIndex, context.contextType == VFXContextType.kUpdate ? "deadListOut" : "deadListIn"));
                if (deadListCountIndex != -1 && context.contextType == VFXContextType.kInit)
                    bufferMappings.Add(new VFXMapping(deadListCountIndex, "deadListCount"));
                if (attributeSourceBufferIndex != -1 && context.contextType == VFXContextType.kInit)
                    bufferMappings.Add(new VFXMapping(attributeSourceBufferIndex, "sourceAttributeBuffer"));

                uniformMappings.Clear();
                foreach (var uniform in contextData.uniformMapper.uniforms.Concat(contextData.uniformMapper.textures))
                    uniformMappings.Add(new VFXMapping(expressionGraph.GetFlattenedIndex(uniform), contextData.uniformMapper.GetName(uniform)));

                taskDesc.buffers = bufferMappings.ToArray();
                taskDesc.values = uniformMappings.ToArray();
                taskDesc.parameters = new VFXMapping[0];
                taskDesc.processor = contextToCompiledData[context].processor;

                taskDescs.Add(taskDesc);
            }

            outSystemDescs.Add(new VFXSystemDesc()
            {
                flags = systemFlag,
                tasks = taskDescs.ToArray(),
                capacity = capacity,
                buffers = systemBufferMappings.ToArray(),
                values = systemValueMappings.ToArray(),
                type = VFXSystemType.kVFXParticle,
            });
        }

        [SerializeField]
        private uint m_Capacity = 65536;
        [SerializeField]
        private Bounds m_Bounds;
        [SerializeField]
        private bool m_WorldSpace;
        [NonSerialized]
        private StructureOfArrayProvider m_layoutAttributeCurrent = new StructureOfArrayProvider();
        [NonSerialized]
        private StructureOfArrayProvider m_layoutAttributeSource = new StructureOfArrayProvider();
    }
}
