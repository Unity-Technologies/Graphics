using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Serializable]
    public class MaterialSlot : SerializableSlot
    {
        [SerializeField]
        private SlotValueType m_ValueType;

        [SerializeField]
        private Vector4 m_DefaultValue;

        [SerializeField]
        private Vector4 m_CurrentValue;
        
        [SerializeField]
        private ConcreteSlotValueType m_ConcreteValueType;

        public MaterialSlot(string name, string displayName, SlotType slotType, SlotValueType valueType, Vector4 defaultValue) 
            : base(name, displayName, slotType)
        {
            m_ValueType = valueType;
            m_DefaultValue = defaultValue;
            m_CurrentValue = defaultValue;
        }
        
        public Vector4 defaultValue
        {
            get { return m_DefaultValue; }
            set { m_DefaultValue = value; }
        }

        public SlotValueType valueType
        { 
            get { return m_ValueType; }
            set { m_ValueType = value; }
        }

        public Vector4 currentValue
        {
            get { return m_CurrentValue; }
            set { m_CurrentValue = value; }
        }

        public ConcreteSlotValueType concreteValueType
        {
            get { return m_ConcreteValueType; }
            set { m_ConcreteValueType = value; }
        }

        public string GetInputName (AbstractMaterialNode node)
        {
            return string.Format( "{0}_{1}", node.name, name);
        }

        public void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType slotValueType, AbstractMaterialNode owner)
        {
            if (!generationMode.IsPreview())
                return;

            visitor.AddShaderChunk("float" + AbstractMaterialNode.ConvertConcreteSlotValueTypeToString(slotValueType) + " " + GetInputName(owner) + ";", true);
        }

        public string GetDefaultValue(GenerationMode generationMode, ConcreteSlotValueType slotValueType, AbstractMaterialNode owner)
        {
            if (generationMode.IsPreview())
                return GetInputName(owner);

            switch (slotValueType)
            {
                case ConcreteSlotValueType.Vector1:
                    return m_CurrentValue.x.ToString();
                case ConcreteSlotValueType.Vector2:
                    return "half2 (" + m_CurrentValue.x + "," + m_CurrentValue.y + ")";
                case ConcreteSlotValueType.Vector3:
                    return "half3 (" + m_CurrentValue.x + "," + m_CurrentValue.y + "," + m_CurrentValue.z + ")";
                case ConcreteSlotValueType.Vector4:
                    return "half4 (" + m_CurrentValue.x + "," + m_CurrentValue.y + "," + m_CurrentValue.z + "," + m_CurrentValue.w + ")";
                default:
                    return "error";
            }
        }

        public override bool OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            m_CurrentValue = EditorGUILayout.Vector4Field("Value", m_CurrentValue);
            return EditorGUI.EndChangeCheck();
        }

        public bool OnGUI(Rect rect, ConcreteSlotValueType inputSlotType)
        {
            EditorGUI.BeginChangeCheck();

            var rectXmax = rect.xMax;
            switch (inputSlotType)
            {
                case ConcreteSlotValueType.Vector1:
                    rect.x = rectXmax - 50;
                    rect.width = 50;
                    EditorGUIUtility.labelWidth = 15;
                    EditorGUI.DrawRect(rect, new Color(0.0f, 0.0f, 0.0f, 0.7f));
                    m_CurrentValue.x = EditorGUI.FloatField(rect, "X", m_CurrentValue.x);
                    break;
                case ConcreteSlotValueType.Vector2:
                    rect.x = rectXmax - 90;
                    rect.width = 90;
                    EditorGUI.DrawRect(rect, new Color(0.0f, 0.0f, 0.0f, 0.7f));
                    var result2 = new Vector4(m_CurrentValue.x, m_CurrentValue.y);
                    result2 = EditorGUI.Vector2Field(rect, GUIContent.none, result2);
                    m_CurrentValue.x = result2.x;
                    m_CurrentValue.y = result2.y;
                    break;
                case ConcreteSlotValueType.Vector3:
                    rect.x = rectXmax - 140;
                    rect.width = 140;
                    EditorGUI.DrawRect(rect, new Color(0.0f, 0.0f, 0.0f, 0.7f));
                    var result3 = new Vector3(m_CurrentValue.x, m_CurrentValue.y, m_CurrentValue.z);
                    result3 = EditorGUI.Vector3Field(rect, GUIContent.none, result3);
                    m_CurrentValue.x = result3.x;
                    m_CurrentValue.y = result3.y;
                    m_CurrentValue.z = result3.z;
                    break;
                default:
                    rect.x = rectXmax - 190;
                    rect.width = 190;
                    EditorGUI.DrawRect(rect, new Color(0.0f, 0.0f, 0.0f, 0.7f));
                    m_CurrentValue = EditorGUI.Vector4Field(rect, GUIContent.none, m_CurrentValue);
                    break;
            }
            return EditorGUI.EndChangeCheck();
        }
    }
}
