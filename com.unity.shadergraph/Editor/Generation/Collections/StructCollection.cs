using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class StructCollection : IEnumerable<StructCollection.Item>
    {
        public class Item : IConditional
        {
            public StructDescriptor descriptor { get; }
            public FieldCondition[] fieldConditions { get; }

            public Item(StructDescriptor descriptor, FieldCondition[] fieldConditions)
            {
                this.descriptor = descriptor;
                this.fieldConditions = fieldConditions;
            }
        }

        readonly List<StructCollection.Item> m_Items;

        public StructCollection()
        {
            m_Items = new List<StructCollection.Item>();
        }

        public StructCollection Add(StructCollection structs)
        {
            foreach(StructCollection.Item item in structs)
            {
                m_Items.Add(item);
            }

            return this;
        }

        public StructCollection Add(StructDescriptor descriptor)
        {
            m_Items.Add(new StructCollection.Item(descriptor, null));
            return this;
        }

        public StructCollection Add(StructDescriptor descriptor, FieldCondition fieldCondition)
        {
            m_Items.Add(new StructCollection.Item(descriptor, new FieldCondition[]{ fieldCondition }));
            return this;
        }

        public StructCollection Add(StructDescriptor descriptor, FieldCondition[] fieldConditions)
        {
            m_Items.Add(new StructCollection.Item(descriptor, fieldConditions));
            return this;
        }

        public IEnumerator<StructCollection.Item> GetEnumerator()
        {
            return m_Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
