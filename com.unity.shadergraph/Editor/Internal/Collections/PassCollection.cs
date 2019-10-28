using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data.Util;

namespace UnityEditor.ShaderGraph.Internal
{
    public class PassCollection : IEnumerable<PassCollection.Item>
    {
        public class Item
        {
            public PassDescriptor descriptor { get; }
            public FieldCondition[] fieldConditions { get; }

            public Item(PassDescriptor descriptor, FieldCondition[] fieldConditions)
            {
                this.descriptor = descriptor;
                this.fieldConditions = fieldConditions;
            }

            public bool TestActive(ActiveFields fields)
            {
                // Test FieldCondition against current active Fields
                bool TestFieldCondition(FieldCondition fieldCondition)
                {
                    // Required active field is not active
                    if(fieldCondition.condition == true && !fields.baseInstance.Contains(fieldCondition.field))
                        return false;

                    // Required non-active field is active
                    else if(fieldCondition.condition == false && fields.baseInstance.Contains(fieldCondition.field))
                        return false;

                    return true;
                }

                // No FieldConditions is always true
                if(fieldConditions == null)
                {
                    return true;
                }

                // One or more FieldConditions failed
                if(fieldConditions.Where(x => !TestFieldCondition(x)).Any())
                {
                    return false;
                }

                // All FieldConditions passed
                return true;
            }
        }

        readonly List<Item> m_Items;

        public PassCollection()
        {
            m_Items = new List<Item>();
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
