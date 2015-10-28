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
        Vector4Dynamic,
        Vector4,
        Vector3,
        Vector2,
        Vector1
    }

    [Serializable]
    public class SlotValue : IGenerateProperties
    {
        [SerializeField]
        private Vector4 m_DefaultVector;
        public Vector4 defaultValue
        {
            get { return m_DefaultVector; }
        }

        [SerializeField]
        private string m_SlotName;
        public string slotName
        {
            get { return m_SlotName; }
        }

        [SerializeField]
        private BaseMaterialNode m_Node;
        public string nodeName
        {
            get { return m_Node.GetOutputVariableNameForNode(); }
        }

        public SlotValue(BaseMaterialNode node, string slotName, Vector4 value)
        {
            m_DefaultVector = value;
            m_SlotName = slotName;
            m_Node = node;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(m_SlotName) && m_Node != null;
        }

        public void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            // no need to generate a property block.
            // we can just set the uniforms.
        }
 
        public string inputName
        {
            get { return nodeName + "_" + slotName; }
        }

        public void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            visitor.AddShaderChunk("float4 " + inputName + ";", true);
        }

        public string GetDefaultValue(GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return "half4 (" + m_DefaultVector.x + "," + m_DefaultVector.y + "," + m_DefaultVector.z + "," + m_DefaultVector.w + ")";
            else
                return inputName;
        }

        public bool OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            m_DefaultVector = EditorGUILayout.Vector4Field("Value", m_DefaultVector);
            return EditorGUI.EndChangeCheck();
        }
    }
}
