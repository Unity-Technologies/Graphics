using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
            set { m_Capacity = value; }
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

        public void GenerateAttributeLayout()
        {
            m_BucketSizes.Clear();
            m_AttributeLayout.Clear();

            var attributeBuckets = new Dictionary<int, List<VFXAttribute>>();
            foreach (var kvp in m_StoredAttributes)
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
                m_BucketSizes.Add(GenerateBucketLayout(bucket.Value, bucketId++));

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

        [SerializeField]
        private uint m_Capacity = 1024;
        [SerializeField]
        private Bounds m_Bounds;
        [SerializeField]
        private bool m_WorldSpace;

        [NonSerialized]
        private Dictionary<VFXAttribute, AttributeLayout> m_AttributeLayout = new Dictionary<VFXAttribute, AttributeLayout>();
        [NonSerialized]
        private List<int> m_BucketSizes = new List<int>();
    }
}
