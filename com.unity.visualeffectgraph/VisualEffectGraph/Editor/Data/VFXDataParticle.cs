using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXDataParticle : VFXData
    {
        public struct AttributeLayout
        {
            public int bucket;
            public int offset;

            public AttributeLayout(int bucket, int offset)
            {
                this.bucket = bucket;
                this.offset = offset;
            }
        }

        public override VFXDataType type { get { return VFXDataType.kParticle; } }

        public uint capacity
        {
            get { return m_Capacity; }
            set
            {
                const uint kThreadPerGroup = 64;
                if (value > kThreadPerGroup)
                    value = (value + kThreadPerGroup - 1u) & ~(kThreadPerGroup - 1u); // multiple of kThreadPerGroup
                m_Capacity = (value + 3u) & ~3u;
            }
        }

        public uint bufferSize
        {
            get
            {
                return (uint)m_BucketOffsets.LastOrDefault() + m_Capacity * (uint)m_BucketSizes.LastOrDefault();
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

        public override bool CanBeCompiled()
        {
            return m_Owners.Count > 1 && m_Owners[0].contextType == VFXContextType.kInit && m_Owners[0].inputContexts.Count() > 0;
        }

        public override void GenerateAttributeLayout()
        {
            m_BucketSizes.Clear();
            m_AttributeLayout.Clear();
            m_BucketOffsets.Clear();

            var attributeBuckets = new Dictionary<int, List<VFXAttribute>>();
            foreach (var kvp in m_StoredCurrentAttributes)
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
                int bucketOffset = bucketId == 0 ? 0 : m_BucketOffsets[bucketId - 1] + (int)m_Capacity * m_BucketSizes[bucketId - 1];
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

        private string GetOffset(VFXAttribute attrib)
        {
            AttributeLayout layout = m_AttributeLayout[attrib];
            return string.Format("(index * 0x{0:X} + 0x{1:X}) << 2", m_BucketSizes[layout.bucket], m_BucketOffsets[layout.bucket] + layout.offset);
        }

        public override string GetLoadAttributeCode(VFXAttribute attrib)
        {
            if (!m_StoredCurrentAttributes.ContainsKey(attrib))
                throw new ArgumentException(string.Format("Attribute {0} does not exist in data layout", attrib.name));


            return string.Format("{0}(attributeBuffer.Load{1}({2}))", GetCastAttributePrefix(attrib), GetByteAddressBufferMethodSuffix(attrib), GetOffset(attrib));
        }

        public override string GetStoreAttributeCode(VFXAttribute attrib, string value)
        {
            if (!m_StoredCurrentAttributes.ContainsKey(attrib))
                throw new ArgumentException(string.Format("Attribute {0} does not exist in data layout", attrib.name));

            return string.Format("attributeBuffer.Store{0}({1},{3}({2}))", GetByteAddressBufferMethodSuffix(attrib), GetOffset(attrib), value, attrib.type == UnityEngine.VFX.VFXValueType.kBool ? "uint" : "asuint");
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

        public void FillDescs(
            List<VFXBufferDesc> outBufferDescs,
            List<VFXSystemDesc> outSystemDescs,
            VFXExpressionGraph expressionGraph,
            Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData,
            Dictionary<VFXContext, int> contextSpawnToBufferIndex)
        {
            bool hasState = bufferSize > 0;
            bool hasKill = IsAttributeStored(VFXAttribute.Alive);

            var attributeBufferIndex = -1;
            var deadListBufferIndex = -1;
            var deadListCountIndex = -1;

            var systemBufferMappings = new List<VFXBufferMapping>();
            var systemValueMappings = new List<VFXValueMapping>();

            if (hasState)
            {
                attributeBufferIndex = outBufferDescs.Count;
                outBufferDescs.Add(new VFXBufferDesc(ComputeBufferType.Raw, bufferSize, 4));
                systemBufferMappings.Add(new VFXBufferMapping(attributeBufferIndex, "attributeBuffer"));
            }

            var systemFlag = VFXSystemFlag.kVFXSystemDefault;
            if (hasKill)
            {
                systemFlag |= VFXSystemFlag.kVFXSystemHasKill;

                deadListBufferIndex = outBufferDescs.Count;
                outBufferDescs.Add(new VFXBufferDesc(ComputeBufferType.Append, capacity, 4));
                systemBufferMappings.Add(new VFXBufferMapping(deadListBufferIndex, "deadList"));

                deadListCountIndex = outBufferDescs.Count;
                outBufferDescs.Add(new VFXBufferDesc(ComputeBufferType.Raw, 1, 4));
                systemBufferMappings.Add(new VFXBufferMapping(deadListCountIndex, "deadListCount"));
            }


            var taskDescs = new List<VFXTaskDesc>();
            var bufferMappings = new List<VFXBufferMapping>();
            var uniformMappings = new List<VFXValueMapping>();

            foreach (var context in owners)
            {
                //if (!contextToCompiledData.ContainsKey(context))
                //    continue;

                if (context.contextType == VFXContextType.kInit)
                    systemBufferMappings.AddRange(context.inputContexts.Select(o => new VFXBufferMapping(contextSpawnToBufferIndex[o], "spawner_input")));

                var contextData = contextToCompiledData[context];

                var taskDesc = new VFXTaskDesc();
                taskDesc.type = context.taskType;

                bufferMappings.Clear();
                if (attributeBufferIndex != -1)
                    bufferMappings.Add(new VFXBufferMapping(attributeBufferIndex, "attributeBuffer"));
                if (deadListBufferIndex != -1 && context.contextType != VFXContextType.kOutput)
                    bufferMappings.Add(new VFXBufferMapping(deadListBufferIndex, context.contextType == VFXContextType.kUpdate ? "deadListOut" : "deadListIn"));
                if (deadListCountIndex != -1 && context.contextType == VFXContextType.kInit)
                    bufferMappings.Add(new VFXBufferMapping(deadListCountIndex, "deadListCount"));

                uniformMappings.Clear();
                foreach (var uniform in contextData.uniformMapper.uniforms.Concat(contextData.uniformMapper.textures))
                    uniformMappings.Add(new VFXValueMapping(expressionGraph.GetFlattenedIndex(uniform), contextData.uniformMapper.GetName(uniform)));

                taskDesc.buffers = bufferMappings.ToArray();
                taskDesc.values = uniformMappings.ToArray();

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
        private Dictionary<VFXAttribute, AttributeLayout> m_AttributeLayout = new Dictionary<VFXAttribute, AttributeLayout>();
        [NonSerialized]
        private List<int> m_BucketSizes = new List<int>();
        [NonSerialized]
        private List<int> m_BucketOffsets = new List<int>();
    }
}
