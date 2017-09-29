using System;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class MaterialSlot : SerializableSlot
    {
        [SerializeField]
        SlotValueType m_ValueType;

        [SerializeField]
        Vector4 m_DefaultValue;

        [SerializeField]
        Vector4 m_CurrentValue;

        [SerializeField]
        ConcreteSlotValueType m_ConcreteValueType;

        [SerializeField]
        string m_ShaderOutputName;

        [SerializeField]
        ShaderStage m_ShaderStage;

        public static readonly string DefaultTextureName = "ShaderGraph_DefaultTexture";

        public MaterialSlot() { }

        public MaterialSlot(int slotId, string displayName, string shaderOutputName, SlotType slotType, SlotValueType valueType, Vector4 defaultValue, ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
            : base(slotId, displayName, slotType, hidden)
        {
            SharedInitialize(shaderOutputName, valueType, defaultValue, shaderStage);
        }

        void SharedInitialize(string inShaderOutputName, SlotValueType inValueType, Vector4 inDefaultValue, ShaderStage shaderStage)
        {
            m_ShaderOutputName = inShaderOutputName;
            valueType = inValueType;
            m_DefaultValue = inDefaultValue;
            m_CurrentValue = inDefaultValue;
            this.shaderStage = shaderStage;
        }

        static string ConcreteSlotValueTypeAsString(ConcreteSlotValueType type)
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

        public ShaderStage shaderStage
        {
            get { return m_ShaderStage; }
            set { m_ShaderStage = value; }
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

            if (concreteValueType == ConcreteSlotValueType.Texture2D)
                return DefaultTextureName;

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

        public void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            // share tex2d for all non connected slots :)
            if (concreteValueType == ConcreteSlotValueType.Texture2D)
            {
                var prop = new TextureShaderProperty();
                prop.overrideReferenceName = DefaultTextureName;
                prop.modifiable = false;
                prop.generatePropertyBlock = true;
                properties.AddShaderProperty(prop);
                return;
            }

            if (concreteValueType == ConcreteSlotValueType.SamplerState)
                return;

            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            IShaderProperty property;
            switch (concreteValueType)
            {
                case ConcreteSlotValueType.Vector4:
                    property = new Vector4ShaderProperty();
                    break;
                case ConcreteSlotValueType.Vector3:
                    property = new Vector3ShaderProperty();
                    break;
                case ConcreteSlotValueType.Vector2:
                    property = new Vector2ShaderProperty();
                    break;
                case ConcreteSlotValueType.Vector1:
                    property = new FloatShaderProperty();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            property.overrideReferenceName = matOwner.GetVariableNameForSlot(id);
            property.generatePropertyBlock = false;

            properties.AddShaderProperty(property);
        }
    }
}
