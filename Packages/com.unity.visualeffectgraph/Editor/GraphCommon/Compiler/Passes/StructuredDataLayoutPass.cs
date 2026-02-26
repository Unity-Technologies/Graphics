using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    class StructuredDataLayoutPass : CompilationPass
    {
        public bool Execute(ref CompilationContext context)
        {
            StructuredDataLayoutContainer structuredDataLayoutContainer = context.data.GetOrCreate<StructuredDataLayoutContainer>();

            foreach (var dataView in context.graph.DataViews)
            {
                if (dataView.DataDescription is StructuredData structuredData)
                {
                    ValueBufferLayout valueBufferLayout = structuredDataLayoutContainer.CreateLayout(structuredData);
                    foreach (var subData in structuredData.SubDataDescriptions)
                    {
                        if (subData is ValueData valueData && !typeof(Texture).IsAssignableFrom(valueData.Type))
                        {
                            valueBufferLayout.AddValueData(valueData);
                        }
                    }
                    valueBufferLayout.ComputeOffsets();
                }
            }
            return true;
        }
    }



    class ValueBufferLayout
    {
        private List<ValueData> m_ValueDatas = new();
        private Dictionary<ValueData, int> m_ValueDataOffsets = new();
        private List<Bucket> m_BucketedValueDatas = new();
        private int m_TotalSize;
        private static readonly int kAlignement = 4;

        public IEnumerable<ValueData> ValueDatas => m_ValueDatas;

        public uint GetBufferSize()
        {
            if (m_ValueDataOffsets.Count == 0)
            {
                throw new Exception("Value data offsets have not been initialized.");
            }

            return (uint)m_TotalSize;
        }

        public void AddValueData(ValueData value)
        {
            m_ValueDatas.Add(value);
        }

        public int GetValueOffset(ValueData valueData)
        {
            if (m_ValueDataOffsets.Count == 0)
            {
                throw new Exception("Value data offsets have not been initialized.");
            }

            return m_ValueDataOffsets.GetValueOrDefault(valueData, -1);
        }

        public bool ContainsValueData(ValueData valueData)
        {
            return m_ValueDataOffsets.ContainsKey(valueData);
        }

        struct OffsetValueData
        {
            public ValueData ValueData { get; }
            public int OffsetInBucket { get; }

            public OffsetValueData(ValueData valueData, int offsetInBucket)
            {
                ValueData = valueData;
                OffsetInBucket = offsetInBucket;
            }
        }
        class Bucket
        {
            List<OffsetValueData> m_BucketValueDatas;
            int m_CurrentSize;

            public Bucket(ValueData valueData)
            {
                m_BucketValueDatas = new List<OffsetValueData>();
                m_BucketValueDatas.Add(new OffsetValueData(valueData, 0));
                int valueSize = ValueSize(valueData);
                m_CurrentSize = valueSize;
            }

            public bool TryAdd(ValueData valueData)
            {
                int valueSize = ValueSize(valueData);
                if(m_CurrentSize + valueSize <= kAlignement)
                {
                    m_BucketValueDatas.Add(new OffsetValueData(valueData, m_CurrentSize));
                    m_CurrentSize += valueSize;
                    return true;
                }
                return false;
            }

            public int CurrentSize => m_CurrentSize;
            public IEnumerable<OffsetValueData> OffsetValueDatas => m_BucketValueDatas;
        }
        List<Bucket> CreateBuckets()
        {
            List<Bucket> buckets = new();

            foreach (var valueData in m_ValueDatas)
            {
                bool bucketFound = false;
                foreach (var bucket in buckets)
                {
                    if (bucket.TryAdd(valueData))
                    {
                        bucketFound = true;
                        break;
                    }
                }
                if (bucketFound)
                    continue;
                // Suitable bucket not found, create a new one
                var newBucket = new Bucket(valueData);
                buckets.Add(newBucket);
            }

            return buckets;
        }
        public void ComputeOffsets()
        {
            m_ValueDataOffsets.Clear();
            m_BucketedValueDatas = CreateBuckets();
            int currentBucketOffset = 0;
            foreach (var bucket in m_BucketedValueDatas)
            {
                foreach (var offsetValueData in bucket.OffsetValueDatas)
                {
                    m_ValueDataOffsets[offsetValueData.ValueData] = currentBucketOffset + offsetValueData.OffsetInBucket;
                }
                currentBucketOffset += kAlignement;
            }
            m_TotalSize = currentBucketOffset;
        }

        //TODO: Temporary method to get the sorted list of value datas
        public List<ValueData> GetSortedValueDatas()
        {
            List<ValueData> sortedValueDatas = new();
            foreach (var bucket in m_BucketedValueDatas)
            {
                foreach (var offsetValueData in bucket.OffsetValueDatas)
                {
                    sortedValueDatas.Add(offsetValueData.ValueData);
                }
            }
            return sortedValueDatas;
        }

        static int ValueSize(ValueData valueData)
        {
            return System.Runtime.InteropServices.Marshal.SizeOf(valueData.Type) / sizeof(uint);
        }
    }

    class StructuredDataLayoutContainer
    {
        Dictionary<StructuredData, ValueBufferLayout> m_GraphValuesLayouts = new();

        internal ValueBufferLayout CreateLayout(StructuredData structuredData)
        {
            if (!m_GraphValuesLayouts.TryGetValue(structuredData, out var layout))
            {
                layout = new ValueBufferLayout();
                m_GraphValuesLayouts[structuredData] = layout;
            }

            return layout;
        }

        internal bool TryGetLayout(StructuredData structuredData, out ValueBufferLayout layout)
        {
            return m_GraphValuesLayouts.TryGetValue(structuredData, out layout);
        }
    }
}
