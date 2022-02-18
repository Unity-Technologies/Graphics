using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class AdditionalCommandCollection : IEnumerable<AdditionalCommandCollection.Item>
    {
        public class Item
        {
            public AdditionalCommandDescriptor field { get; }

            public Item(AdditionalCommandDescriptor field)
            {
                this.field = field;
            }
        }

        readonly List<AdditionalCommandCollection.Item> m_Items;

        public AdditionalCommandCollection()
        {
            m_Items = new List<AdditionalCommandCollection.Item>();
        }

        public AdditionalCommandCollection Add(AdditionalCommandCollection fields)
        {
            foreach (AdditionalCommandCollection.Item item in fields)
            {
                m_Items.Add(item);
            }

            return this;
        }

        public AdditionalCommandCollection Add(AdditionalCommandDescriptor field)
        {
            m_Items.Add(new Item(field));
            return this;
        }

        public IEnumerator<AdditionalCommandCollection.Item> GetEnumerator()
        {
            return m_Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
