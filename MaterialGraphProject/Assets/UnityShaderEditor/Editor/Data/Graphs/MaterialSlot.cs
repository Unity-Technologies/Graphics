using System;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public abstract class MaterialSlot : SerializableSlot
    {
        [SerializeField]
        string m_ShaderOutputName;

        [SerializeField]
        ShaderStage m_ShaderStage;

        private bool m_HasError;

        protected MaterialSlot() { }

        protected MaterialSlot(int slotId, string displayName, string shaderOutputName, SlotType slotType, ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
            : base(slotId, displayName, slotType, hidden)
        {
            m_ShaderOutputName = shaderOutputName;
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

        public string RawDisplayName()
        {
            return base.displayName;
        }

        public static MaterialSlot CreateMaterialSlot(SlotValueType type, int slotId, string displayName, string shaderOutputName, SlotType slotType, Vector4 defaultValue, ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
        {
            switch (type)
            {
                case SlotValueType.SamplerState:
                    return new SamplerStateMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden);
                case SlotValueType.Matrix4:
                    return new Matrix4MaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden);
                case SlotValueType.Matrix3:
                    return new Matrix3MaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden);
                case SlotValueType.Matrix2:
                    return new Matrix2MaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden);
                case SlotValueType.Texture2D:
                    return new Texture2DMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden);
                case SlotValueType.Dynamic:
                    return new DynamicVectorMaterialSlot(slotId, displayName, shaderOutputName, slotType, defaultValue, shaderStage, hidden);
                case SlotValueType.Vector4:
                    return new Vector4MaterialSlot(slotId, displayName, shaderOutputName, slotType, defaultValue, shaderStage, hidden);
                case SlotValueType.Vector3:
                    return new Vector3MaterialSlot(slotId, displayName, shaderOutputName, slotType, defaultValue, shaderStage, hidden);
                case SlotValueType.Vector2:
                    return new Vector2MaterialSlot(slotId, displayName, shaderOutputName, slotType, defaultValue, shaderStage, hidden);
                case SlotValueType.Vector1:
                    return new Vector1MaterialSlot(slotId, displayName, shaderOutputName, slotType, defaultValue.x, shaderStage, hidden);
            }

            throw new ArgumentOutOfRangeException("type", type, null);
        }

        public abstract SlotValueType valueType { get; }

        public abstract ConcreteSlotValueType concreteValueType { get; }

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

        public bool hasError
        {
            get { return m_HasError; }
            set { m_HasError = value; }
        }

        public bool IsCompatibleWithInputSlotType(ConcreteSlotValueType inputType)
        {
            switch (concreteValueType)
            {
                case ConcreteSlotValueType.SamplerState:
                    return inputType == ConcreteSlotValueType.SamplerState;
                case ConcreteSlotValueType.Matrix4:
                    return inputType == ConcreteSlotValueType.Matrix4
                        || inputType == ConcreteSlotValueType.Matrix3
                        || inputType == ConcreteSlotValueType.Matrix2;
                case ConcreteSlotValueType.Matrix3:
                    return inputType == ConcreteSlotValueType.Matrix3
                        || inputType == ConcreteSlotValueType.Matrix2;
                case ConcreteSlotValueType.Matrix2:
                    return inputType == ConcreteSlotValueType.Matrix2;
                case ConcreteSlotValueType.Texture2D:
                    return inputType == ConcreteSlotValueType.Texture2D;
                case ConcreteSlotValueType.Vector4:
                    return inputType == ConcreteSlotValueType.Vector4
                        || inputType == ConcreteSlotValueType.Vector3
                        || inputType == ConcreteSlotValueType.Vector2
                        || inputType == ConcreteSlotValueType.Vector1;
                case ConcreteSlotValueType.Vector3:
                    return inputType == ConcreteSlotValueType.Vector3
                        || inputType == ConcreteSlotValueType.Vector2
                        || inputType == ConcreteSlotValueType.Vector1;
                case ConcreteSlotValueType.Vector2:
                    return inputType == ConcreteSlotValueType.Vector2
                        || inputType == ConcreteSlotValueType.Vector1;
                case ConcreteSlotValueType.Vector1:
                    return inputType == ConcreteSlotValueType.Vector4
                        || inputType == ConcreteSlotValueType.Vector3
                        || inputType == ConcreteSlotValueType.Vector2
                        || inputType == ConcreteSlotValueType.Vector1;
            }
            return false;
        }

        public bool IsCompatibleWith(MaterialSlot otherSlot)
        {
            return otherSlot != null
                && otherSlot.owner != owner
                && otherSlot.isInputSlot != isInputSlot
                && (isInputSlot
                    ? otherSlot.IsCompatibleWithInputSlotType(concreteValueType)
                    : IsCompatibleWithInputSlotType(otherSlot.concreteValueType));
        }

        public virtual string GetDefaultValue(GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            if (concreteValueType == ConcreteSlotValueType.Texture2D)
                return Texture2DMaterialSlot.DefaultTextureName;

            if (generationMode.IsPreview())
                return matOwner.GetVariableNameForSlot(id);

            return ConcreteSlotValueAsVariable(matOwner.precision);
        }

        protected virtual string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return "error";
        }

        public void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            // share tex2d for all non connected slots :)
            if (concreteValueType == ConcreteSlotValueType.Texture2D)
            {
                var prop = new TextureShaderProperty();
                prop.overrideReferenceName = Texture2DMaterialSlot.DefaultTextureName;
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

        protected static PropertyType ConvertConcreteSlotValueTypeToPropertyType(ConcreteSlotValueType slotValue)
        {
            switch (slotValue)
            {
                case ConcreteSlotValueType.Texture2D:
                    return PropertyType.Texture;
                case ConcreteSlotValueType.Vector1:
                    return PropertyType.Float;
                case ConcreteSlotValueType.Vector2:
                    return PropertyType.Vector2;
                case ConcreteSlotValueType.Vector3:
                    return PropertyType.Vector3;
                case ConcreteSlotValueType.Vector4:
                    return PropertyType.Vector4;
                case ConcreteSlotValueType.Matrix2:
                    return PropertyType.Matrix2;
                case ConcreteSlotValueType.Matrix3:
                    return PropertyType.Matrix3;
                case ConcreteSlotValueType.Matrix4:
                    return PropertyType.Matrix4;
                case ConcreteSlotValueType.SamplerState:
                    return PropertyType.SamplerState;
                default:
                    return PropertyType.Vector4;
            }
        }

        public virtual PreviewProperty GetPreviewProperty(string name)
        {
            return null;
        }
    }
}
