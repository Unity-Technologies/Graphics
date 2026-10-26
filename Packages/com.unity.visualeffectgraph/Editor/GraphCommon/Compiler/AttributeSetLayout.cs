using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    struct AttributeBucket
    {
        public uint m_Offset;
        public uint m_Size;

        public AttributeBucket(uint bucketOffset, uint bucketSize)
        {
            m_Offset = bucketOffset;
            m_Size = bucketSize;
        }
    }

    struct AttributeSetLayout
    {
        private struct AttributeLayoutInfo
        {
            public uint bucket;
            public uint offset;

            public AttributeLayoutInfo(uint bucket, uint offset)
            {
                this.bucket = bucket;
                this.offset = offset;
            }
        }
        private List<AttributeBucket> m_AttributeBuckets;
        public uint Count => (uint)m_AttributeLayouts.Count;
        public uint Capacity { get; }
        private Dictionary<Attribute, AttributeLayoutInfo> m_AttributeLayouts;

        public bool Valid => m_AttributeBuckets != null && m_AttributeLayouts != null;
        public IEnumerable<Attribute> Attributes => m_AttributeLayouts.Keys;

        public AttributeSetLayout(uint capacity)
        {
            m_AttributeBuckets = new List<AttributeBucket>();
            m_AttributeLayouts = new Dictionary<Attribute, AttributeLayoutInfo>();
            Capacity = capacity;
        }

        public void AddGroup(IEnumerable<Attribute> attributes)
        {
            //TODO: For now all attributes with the same key in the same bucket.
            AddBucket(attributes);
        }

        void AddBucket(IEnumerable<Attribute> attributes)
        {
            uint bucketOffset = m_AttributeBuckets.Count == 0 ? 0u :(uint)(m_AttributeBuckets[^1].m_Offset + Capacity * m_AttributeBuckets[^1].m_Size);
            uint currentOffset = 0;
            foreach (var attribute in attributes)
            {
                m_AttributeLayouts.Add(attribute, new AttributeLayoutInfo((uint)m_AttributeBuckets.Count, currentOffset));
                currentOffset += (uint)System.Runtime.InteropServices.Marshal.SizeOf(attribute.DefaultValue)/sizeof(uint);
            }

            uint bucketSize = currentOffset;
            var attributeBucket = new AttributeBucket(bucketOffset, bucketSize);
            m_AttributeBuckets.Add(attributeBucket);
        }

        public uint GetBufferSize()
        {
            return m_AttributeBuckets.Count > 0 ? m_AttributeBuckets[^1].m_Offset + Capacity * m_AttributeBuckets[^1].m_Size : 0;
        }

        public bool ContainsAttribute(Attribute attribute)
        {
            return m_AttributeLayouts.ContainsKey(attribute);
        }

        public (uint offset, uint stride) GetAttributeLocation(Attribute attrib)
        {
            AttributeLayoutInfo layoutInfo;
            if (!m_AttributeLayouts.TryGetValue(attrib, out layoutInfo))
            {
                Debug.LogError($"Could not find layout for attribute {attrib.Name}");
            }
            return GetAttributeLocation(layoutInfo);
        }

        private (uint offset, uint stride) GetAttributeLocation(AttributeLayoutInfo layoutInfo)
        {
            return (m_AttributeBuckets[(int)layoutInfo.bucket].m_Offset + layoutInfo.offset,
                m_AttributeBuckets[(int)layoutInfo.bucket].m_Size);
        }

        public (uint bucketOffset, uint bucketSize, uint elementOffset) GetBucketLocation(Attribute attrib)
        {
            AttributeLayoutInfo layoutInfo;
            if (!m_AttributeLayouts.TryGetValue(attrib, out layoutInfo))
            {
                Debug.LogError($"Could not find layout for attribute {attrib.Name}");
            }

            return (m_AttributeBuckets[(int)layoutInfo.bucket].m_Offset,
                m_AttributeBuckets[(int)layoutInfo.bucket].m_Size,
                layoutInfo.offset);
        }

        // Iterates the attributes, with their corresponding offset and stride
        public struct Enumerator
        {
            AttributeSetLayout m_Owner;
            Dictionary<Attribute, AttributeLayoutInfo>.Enumerator m_Inner;

            public Enumerator(AttributeSetLayout owner)
            {
                m_Owner = owner;
                m_Inner = owner.m_AttributeLayouts.GetEnumerator();
            }

            public (Attribute, uint, uint) Current
            {
                get
                {
                    var current = m_Inner.Current;
                    (uint offset, uint stride) = m_Owner.GetAttributeLocation(current.Value);
                    return (current.Key, offset, stride);
                }
            }

            public void Dispose() => m_Inner.Dispose();
            public bool MoveNext() => m_Inner.MoveNext();

        }
        public Enumerator GetEnumerator() => new Enumerator(this);
    }

    class AttributeSetLayoutCompilationData
    {
        Dictionary<AttributeData, AttributeSetLayout> m_layouts = new();

        public AttributeSetLayout this[AttributeData attributeData]
        {
            get
            {
                return m_layouts.TryGetValue(attributeData, out AttributeSetLayout layout) ? layout : new AttributeSetLayout();
            }
            set
            {
                m_layouts[attributeData] = value;
            }
        }

        public Dictionary<AttributeData, AttributeSetLayout>.Enumerator GetEnumerator() => m_layouts.GetEnumerator();
    }
}
