using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class RenderStateCollection : IEnumerable<RenderStateCollection.Item>
    {
        public class Item : IConditional, IShaderString
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

        public void Add(RenderStateCollection renderStates)
        {
            foreach(RenderStateCollection.Item item in renderStates)
            {
                m_Items.Add(item);
            }
        }

        public void Add(RenderStateDescriptor descriptor)
        {
            m_Items.Add(new Item(descriptor, null));
        }

        public void Add(RenderStateDescriptor descriptor, FieldCondition fieldCondition)
        {
            m_Items.Add(new Item(descriptor, new FieldCondition[]{ fieldCondition }));
        }

        public void Add(RenderStateDescriptor descriptor, FieldCondition[] fieldConditions)
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
