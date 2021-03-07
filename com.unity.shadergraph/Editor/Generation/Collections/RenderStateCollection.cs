using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class RenderStateCollection : IEnumerable<RenderStateCollection.Item>
    {
        public class Item : IConditional
        {
            public RenderStateDescriptor descriptor { get; }
            public FieldCondition[] fieldConditions { get; }
            public string value => descriptor.value;

            public Item(RenderStateDescriptor descriptor, FieldCondition[] fieldConditions)
            {
                this.descriptor = descriptor;
                this.fieldConditions = fieldConditions;
            }
        }

        readonly List<Item> m_Items;

        public RenderStateCollection()
        {
            m_Items = new List<Item>();
        }

        public RenderStateCollection Add(RenderStateCollection renderStates)
        {
            foreach (RenderStateCollection.Item item in renderStates)
            {
                m_Items.Add(item);
            }

            return this;
        }

        public RenderStateCollection Add(RenderStateDescriptor descriptor)
        {
            m_Items.Add(new Item(descriptor, null));
            return this;
        }

        public RenderStateCollection Add(RenderStateDescriptor descriptor, FieldCondition fieldCondition)
        {
            m_Items.Add(new Item(descriptor, new FieldCondition[] { fieldCondition }));
            return this;
        }

        public RenderStateCollection Add(RenderStateDescriptor descriptor, FieldCondition[] fieldConditions)
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
