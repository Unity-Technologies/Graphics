using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class KernelCollection : IEnumerable<KernelCollection.Item>
    {
        public class Item : IConditional
        {
            public KernelDescriptor descriptor { get; }
            public FieldCondition[] fieldConditions { get; }

            public Item(KernelDescriptor descriptor, FieldCondition[] fieldConditions)
            {
                this.descriptor = descriptor;
                this.fieldConditions = fieldConditions;
            }
        }

        readonly List<Item> m_Items;

        public KernelCollection()
        {
            m_Items = new List<Item>();
        }

        public KernelCollection Add(KernelCollection passes)
        {
            foreach (KernelCollection.Item item in passes)
            {
                m_Items.Add(item);
            }

            return this;
        }

        public KernelCollection Add(KernelDescriptor descriptor)
        {
            m_Items.Add(new Item(descriptor, null));
            return this;
        }

        public KernelCollection Add(KernelDescriptor descriptor, FieldCondition fieldCondition)
        {
            m_Items.Add(new Item(descriptor, new FieldCondition[] { fieldCondition }));
            return this;
        }

        public KernelCollection Add(KernelDescriptor descriptor, FieldCondition[] fieldConditions)
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
