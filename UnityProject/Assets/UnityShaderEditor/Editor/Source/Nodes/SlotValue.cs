using System;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    /*  public static class SlotExtensions
        {
            public static bool Editable (this Slot slot)
            {
                var node = slot.node as BaseMaterialNode;
                if (node == null)
                    return false;
                var properties = node[slot];
                if (properties != null)
                    return properties.editable;
                return false;
            }

            public static bool Removable (this Slot slot)
            {
                var node = slot.node as BaseMaterialNode;
                if (node == null)
                    return false;
                var properties = node[slot];
                if (properties != null)
                    return properties.removable;
                return false;
            }

            public static bool SupportsDefault (this Slot slot)
            {
                var node = slot.node as BaseMaterialNode;
                if (node == null)
                    return true;
                var properties = node[slot];
                if (properties != null)
                    return properties.supportsDefault;
                return true;
            }

            public static ShaderProperty GetDefaultValue (this Slot slot)
            {
                var node = slot.node as BaseMaterialNode;
                if (node == null)
                    return null;
                var properties = node[slot];
                if (properties != null && properties.supportsDefault)
                    return properties.value;
                return null;
            }

            public static void SetDefaultValue (this Slot slot, ShaderProperty value)
            {
                var node = slot.node as BaseMaterialNode;
                if (node == null)
                    return;
                var properties = node[slot];
                if (properties != null && properties.supportsDefault)
                    slot.SetDefaultValueForSlot (value);
            }

            public static SlotProperties GetSlotProperties (this Slot slot)
            {
                var node = slot.node as BaseMaterialNode;
                if (node == null)
                    return null;
                int index = node.m_SlotPropertiesIndexes.FindIndex (x => x == slot.name);
                if (index > -1)
                    return node.m_SlotProperties[index];
                return null;
            }

            public static void SetDefaultValueForSlot (this Slot slot, ShaderProperty value)
            {
                var node = slot.node as BaseMaterialNode;
                if (node == null)
                    return;
                var properties = node[slot];
                if (properties != null && properties.supportsDefault) {
                    Object.DestroyImmediate (properties.value, true);
                    properties.value = value;
                } else {
                    properties = new SlotProperties() {value = value};
                    slot.SetPropertiesForSlot (properties);
                }
            }

            public static void SetPropertiesForSlot (this Slot slot, SlotProperties property)
            {
                var node = slot.node as BaseMaterialNode;
                if (node == null)
                    return;
                int index = node.m_SlotPropertiesIndexes.FindIndex (x => x == slot.name);
                var oldValue = index > -1 ? node.m_SlotProperties[index] : null;
                if (oldValue != null && oldValue.value != null)
                    Object.DestroyImmediate (oldValue.value, true);

                if (property == null && index > -1)
                {
                    if (oldValue != null)
                        oldValue.value = null;
                    node.m_SlotPropertiesIndexes.RemoveAt (index);
                    node.m_SlotProperties.RemoveAt (index);
                }
                else
                {
                    if (index > -1) {
                        node.m_SlotProperties[index] = property;
                        if (node is IPropertyNode)
                            ((IPropertyNode)node).UpdateProperty ();
                    }
                    else
                    {
                        node.m_SlotPropertiesIndexes.Add (slot.name);
                        node.m_SlotProperties.Add (property);
                        if (node is IPropertyNode)
                            ((IPropertyNode)node).UpdateProperty ();
                    }
                }
            }

            public static void ClearDefaultValue (this Slot slot)
            {
                slot.SetDefaultValueForSlot (null);
            }

        }*/

    [Serializable]
    public enum SlotValueType
    {
        Dynamic,
        Vector4,
        Vector3,
        Vector2,
        Vector1
    }
    
    public enum ConcreteSlotValueType
    {
        Vector4 = 4,
        Vector3 = 3,
        Vector2 = 2,
        Vector1 = 1,
        Error = 0
    }
}
