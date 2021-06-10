using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class KeywordCollection : IEnumerable<KeywordCollection.Item>
    {
        public class Item : IConditional
        {
            public KeywordDescriptor descriptor { get; }
            public FieldCondition[] fieldConditions { get; }

            public Item(KeywordDescriptor descriptor, FieldCondition[] fieldConditions)
            {
                this.descriptor = descriptor;
                this.fieldConditions = fieldConditions;
            }
        }

        readonly List<Item> m_Items;

        public KeywordCollection()
        {
            m_Items = new List<Item>();
        }

        public KeywordCollection Add(KeywordCollection keywords)
        {
            foreach (KeywordCollection.Item item in keywords)
            {
                m_Items.Add(item);
            }

            return this;
        }

        public KeywordCollection Add(KeywordDescriptor descriptor)
        {
            m_Items.Add(new Item(descriptor, null));
            return this;
        }

        public KeywordCollection Add(KeywordDescriptor descriptor, FieldCondition fieldCondition)
        {
            m_Items.Add(new Item(descriptor, new FieldCondition[] { fieldCondition }));
            return this;
        }

        public KeywordCollection Add(KeywordDescriptor descriptor, FieldCondition[] fieldConditions)
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
