using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class FieldCollection : IEnumerable<FieldCollection.Item>
    {
        public class Item
        {
            public FieldDescriptor field { get; }

            public Item(FieldDescriptor field)
            {
                this.field = field;
            }
        }

        readonly List<FieldCollection.Item> m_Items;

        public FieldCollection()
        {
            m_Items = new List<FieldCollection.Item>();
        }

        public FieldCollection Add(FieldCollection fields)
        {
            m_Items.AddRange(fields);

            return this;
        }

        public FieldCollection Add(FieldDescriptor field)
        {
            m_Items.Add(new Item(field));
            return this;
        }

        public IEnumerator<FieldCollection.Item> GetEnumerator()
        {
            return m_Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
