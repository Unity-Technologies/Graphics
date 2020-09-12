using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class PragmaCollection : IEnumerable<PragmaCollection.Item>
    {
        public class Item : IConditional, IShaderString
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

        public void Add(PragmaCollection pragmas)
        {
            foreach(PragmaCollection.Item item in pragmas)
            {
                m_Items.Add(item);
            }
        }

        public void Add(PragmaDescriptor descriptor)
        {
            m_Items.Add(new Item(descriptor, null));
        }

        public void Add(PragmaDescriptor descriptor, FieldCondition fieldCondition)
        {
            m_Items.Add(new Item(descriptor, new FieldCondition[]{ fieldCondition }));
        }

        public void Add(PragmaDescriptor descriptor, FieldCondition[] fieldConditions)
        {
            m_Items.Add(new Item(descriptor, fieldConditions));
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
