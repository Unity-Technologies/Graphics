using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class PassCollection : IEnumerable<PassCollection.Item>
    {
        public class Item : IConditional
        {
            public PassDescriptor descriptor { get; }
            public FieldCondition[] fieldConditions { get; }

            public Item(PassDescriptor descriptor, FieldCondition[] fieldConditions)
            {
                this.descriptor = descriptor;
                this.fieldConditions = fieldConditions;
            }
        }

        readonly List<Item> m_Items;

        public PassCollection()
        {
            m_Items = new List<Item>();
        }

        public void Add(PassCollection passes)
        {
            foreach(PassCollection.Item item in passes)
            {
                m_Items.Add(item);
            }
        }

        public void Add(PassDescriptor descriptor)
        {
            m_Items.Add(new Item(descriptor, null));
        }

        public void Add(PassDescriptor descriptor, FieldCondition fieldCondition)
        {
            m_Items.Add(new Item(descriptor, new FieldCondition[]{ fieldCondition }));
        }

        public void Add(PassDescriptor descriptor, FieldCondition[] fieldConditions)
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
