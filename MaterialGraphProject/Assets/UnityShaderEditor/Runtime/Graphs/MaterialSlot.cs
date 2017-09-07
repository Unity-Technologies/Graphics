using System;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
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

        [SerializeField]
        private string m_ShaderOutputName;

        public MaterialSlot() { }

        public MaterialSlot(int slotId, string displayName, string shaderOutputName, SlotType slotType, SlotValueType valueType, Vector4 defaultValue, bool hidden = false)
            : base(slotId, displayName, slotType, hidden)
        {
            SharedInitialize(shaderOutputName, valueType, defaultValue);
        }

        private void SharedInitialize(string inShaderOutputName, SlotValueType inValueType, Vector4 inDefaultValue)
        {
            m_ShaderOutputName = inShaderOutputName;
            valueType = inValueType;
            m_DefaultValue = inDefaultValue;
            m_CurrentValue = inDefaultValue;
        }

        private static string ConcreteSlotValueTypeAsString(ConcreteSlotValueType type)
        {
            switch (type)
            {
                case ConcreteSlotValueType.Vector1:
                    return "(1)";
                case ConcreteSlotValueType.Vector2:
                    return "(2)";
                case ConcreteSlotValueType.Vector3:
                    return "(3)";
                case ConcreteSlotValueType.Vector4:
                    return "(4)";
                case ConcreteSlotValueType.Matrix2:
                    return "(2x2)";
                case ConcreteSlotValueType.Matrix3:
                    return "(3x3)";
                case ConcreteSlotValueType.Matrix4:
                    return "(4x4)";
                case ConcreteSlotValueType.SamplerState:
                    return "(SS)";
                case ConcreteSlotValueType.Texture2D:
                    return "(T)";
                default:
                    return "(E)";
            }
        }

        public override string displayName
        {
            get { return base.displayName + ConcreteSlotValueTypeAsString(concreteValueType); }
            set { base.displayName = value; }
        }

        public Vector4 defaultValue
        {
            get { return m_DefaultValue; }
            set { m_DefaultValue = value; }
        }

        public SlotValueType valueType
        {
            get { return m_ValueType; }
            set
            {
                switch (value)
                {
                    case SlotValueType.Vector1:
                        concreteValueType = ConcreteSlotValueType.Vector1;
                        break;
                    case SlotValueType.Vector2:
                        concreteValueType = ConcreteSlotValueType.Vector2;
                        break;
                    case SlotValueType.Vector3:
                        concreteValueType = ConcreteSlotValueType.Vector3;
                        break;
                    case SlotValueType.Matrix2:
                        concreteValueType = ConcreteSlotValueType.Matrix2;
                        break;
                    case SlotValueType.Matrix3:
                        concreteValueType = ConcreteSlotValueType.Matrix3;
                        break;
                    case SlotValueType.Matrix4:
                        concreteValueType = ConcreteSlotValueType.Matrix4;
                        break;
                    case SlotValueType.Texture2D:
                        concreteValueType = ConcreteSlotValueType.Texture2D;
                        break;
                    case SlotValueType.SamplerState:
                        concreteValueType = ConcreteSlotValueType.SamplerState;
                        break;
                    default:
                        concreteValueType = ConcreteSlotValueType.Vector4;
                        break;
                }
                m_ValueType = value;
            }
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

        public string shaderOutputName
        {
            get { return m_ShaderOutputName; }
            private set { m_ShaderOutputName = value; }
        }

        public void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            if (concreteValueType == ConcreteSlotValueType.SamplerState ||
                concreteValueType == ConcreteSlotValueType.Texture2D)
                return;

            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            visitor.AddShaderChunk(AbstractMaterialNode.ConvertConcreteSlotValueTypeToString(matOwner.precision, concreteValueType) + " " + matOwner.GetVariableNameForSlot(id) + ";", true);
        }

        public bool IsCompatibleWithInputSlotType(SlotValueType inputType)
        {
            switch (valueType)
            {
                case SlotValueType.SamplerState:
                    return inputType == SlotValueType.SamplerState;
                case SlotValueType.Matrix4:
                    return inputType == SlotValueType.Matrix4
                        || inputType == SlotValueType.Matrix3
                        || inputType == SlotValueType.Matrix2;
                case SlotValueType.Matrix3:
                    return inputType == SlotValueType.Matrix3
                        || inputType == SlotValueType.Matrix2;
                case SlotValueType.Matrix2:
                    return inputType == SlotValueType.Matrix2;
                case SlotValueType.Texture2D:
                    return inputType == SlotValueType.Texture2D;
                case SlotValueType.Vector4:
                    return inputType == SlotValueType.Vector4
                        || inputType == SlotValueType.Vector3
                        || inputType == SlotValueType.Vector2
                        || inputType == SlotValueType.Vector1
                        || inputType == SlotValueType.Dynamic;
                case SlotValueType.Vector3:
                    return inputType == SlotValueType.Vector3
                        || inputType == SlotValueType.Vector2
                        || inputType == SlotValueType.Vector1
                        || inputType == SlotValueType.Dynamic;
                case SlotValueType.Vector2:
                    return inputType == SlotValueType.Vector2
                        || inputType == SlotValueType.Vector1
                        || inputType == SlotValueType.Dynamic;
                case SlotValueType.Dynamic:
                case SlotValueType.Vector1:
                    return inputType == SlotValueType.Vector4
                        || inputType == SlotValueType.Vector3
                        || inputType == SlotValueType.Vector2
                        || inputType == SlotValueType.Vector1
                        || inputType == SlotValueType.Dynamic;
            }
            return false;
        }

        public string GetDefaultValue(GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            if (generationMode.IsPreview())
                return matOwner.GetVariableNameForSlot(id);

            switch (concreteValueType)
            {
                case ConcreteSlotValueType.Vector1:
                    return m_CurrentValue.x.ToString();
                case ConcreteSlotValueType.Vector2:
                    return matOwner.precision + "2 (" + m_CurrentValue.x + "," + m_CurrentValue.y + ")";
                case ConcreteSlotValueType.Vector3:
                    return matOwner.precision + "3 (" + m_CurrentValue.x + "," + m_CurrentValue.y + "," + m_CurrentValue.z + ")";
                case ConcreteSlotValueType.Vector4:
                    return matOwner.precision + "4 (" + m_CurrentValue.x + "," + m_CurrentValue.y + "," + m_CurrentValue.z + "," + m_CurrentValue.w + ")";
                case ConcreteSlotValueType.Matrix2:
                    return matOwner.precision + "2x2 (" + m_CurrentValue.x + ", " + m_CurrentValue.x + ", " + m_CurrentValue.y + ", " + m_CurrentValue.y + ")";
                case ConcreteSlotValueType.Matrix3:
                    return matOwner.precision + "3x3 (" + m_CurrentValue.x + ", " + m_CurrentValue.x + ", " + m_CurrentValue.x + ", " + m_CurrentValue.y + ", " + m_CurrentValue.y + ", " + m_CurrentValue.y + ", " + m_CurrentValue.z + ", " + m_CurrentValue.z + ", " + m_CurrentValue.z + ")";
                case ConcreteSlotValueType.Matrix4:
                    return matOwner.precision + "4x4 (" + m_CurrentValue.x + ", " + m_CurrentValue.x + ", " + m_CurrentValue.x + ", " + m_CurrentValue.x + ", " + m_CurrentValue.y + ", " + m_CurrentValue.y + ", " + m_CurrentValue.y + ", " + m_CurrentValue.y + ", " + m_CurrentValue.z + ", " + m_CurrentValue.z + ", " + m_CurrentValue.z + ", " + m_CurrentValue.z + ", " + m_CurrentValue.w + ", " + m_CurrentValue.w + ", " + m_CurrentValue.w + ", " + m_CurrentValue.w + ")";
                default:
                    return "error";
            }
        }

        /*
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
        }*/
    }
}
