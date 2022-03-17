using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class PragmaCollection : IEnumerable<PragmaCollection.Item>
    {
        public class Item : IConditional
        {
            public PragmaDescriptor descriptor { get; }
            public FieldCondition[] fieldConditions { get; }
            public string value => $"#pragma {descriptor.value}";

            public Item(PragmaDescriptor descriptor, FieldCondition[] fieldConditions)
            {
                this.descriptor = descriptor;
                this.fieldConditions = fieldConditions;
            }
        }

        readonly List<Item> m_Items;

        public PragmaCollection()
        {
            m_Items = new List<Item>();
        }

        public PragmaCollection Add(PragmaCollection pragmas)
        {
            foreach (PragmaCollection.Item item in pragmas)
            {
                m_Items.Add(item);
            }

            return this;
        }

        public PragmaCollection Add(PragmaDescriptor descriptor)
        {
            m_Items.Add(new Item(descriptor, null));
            return this;
        }

        public PragmaCollection Add(PragmaDescriptor descriptor, FieldCondition fieldCondition)
        {
            m_Items.Add(new Item(descriptor, new FieldCondition[] { fieldCondition }));
            return this;
        }

        public PragmaCollection Add(PragmaDescriptor descriptor, FieldCondition[] fieldConditions)
        {
            m_Items.Add(new Item(descriptor, fieldConditions));
            return this;
        }

        public IEnumerator<Item> GetEnumerator()
        {
            return m_Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
