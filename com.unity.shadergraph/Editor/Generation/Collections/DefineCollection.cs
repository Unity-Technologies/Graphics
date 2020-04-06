using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class DefineCollection : IEnumerable<DefineCollection.Item>
    {
        public class Item : IConditional, IShaderString
        {
            public KeywordDescriptor descriptor { get; }
            public FieldCondition[] fieldConditions { get; }
            public string value => descriptor.ToDefineString(index);
            public int index { get; }

            public Item(KeywordDescriptor descriptor, int index, FieldCondition[] fieldConditions)
            {
                this.descriptor = descriptor;
                this.fieldConditions = fieldConditions;
                this.index = index;
            }
        }

        readonly List<Item> m_Items;

        public DefineCollection()
        {
            m_Items = new List<Item>();
        }

        public void Add(DefineCollection defines)
        {
            foreach(DefineCollection.Item item in defines)
            {
                m_Items.Add(item);
            }
        }

        public void Add(KeywordDescriptor descriptor, int index)
        {
            m_Items.Add(new Item(descriptor, index, null));
        }

        public void Add(KeywordDescriptor descriptor, int index, FieldCondition fieldCondition)
        {
            m_Items.Add(new Item(descriptor, index, new FieldCondition[]{ fieldCondition }));
        }

        public void Add(KeywordDescriptor descriptor, int index, FieldCondition[] fieldConditions)
        {
            m_Items.Add(new Item(descriptor, index, fieldConditions));
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
