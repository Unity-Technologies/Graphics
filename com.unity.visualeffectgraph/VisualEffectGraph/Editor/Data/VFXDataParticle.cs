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
            int offset;
            int bucket;
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
                GenerateBucketLayout(bucket.Value, bucketId++);
        }

        private void GenerateBucketLayout(List<VFXAttribute> attributes, int bucketId)
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
        }

        [SerializeField]
        private uint m_Capacity = 1024;
        [SerializeField]
        private Bounds m_Bounds;
        [SerializeField]
        private bool m_WorldSpace;

        [NonSerialized]
        private Dictionary<VFXAttribute, AttributeLayout> m_AttributeLayout = new Dictionary<VFXAttribute, AttributeLayout>();
    }
}
