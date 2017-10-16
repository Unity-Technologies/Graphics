using System;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class Vector1MaterialSlot : MaterialSlot
    {
        [SerializeField]
        private float m_Value;
        
        public Vector1MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            float value,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            :base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
            m_Value = value;
        }

        public float value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return value.ToString();
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector1; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector1; } }
    }

    [Serializable]
    public class Vector2MaterialSlot : MaterialSlot
    {
        [SerializeField]
        private Vector2 m_Value;

        [SerializeField]
        private Vector2 m_DefaultValue;

        public Vector2MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Vector2 value,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            :base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
            m_Value = value;
        }

        public Vector2 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return precision + "2 (" + value.x + "," + value.y + ")";
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector2; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector2; } }
    }

    [Serializable]
    public class Vector3MaterialSlot : MaterialSlot
    {
        [SerializeField]
        private Vector3 m_Value;

        [SerializeField]
        private Vector3 m_DefaultValue;

        public Vector3MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Vector3 value,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            :base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
            m_Value = value;
        }

        public Vector3 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }
        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return precision + "3 (" + value.x + "," + value.y + "," + value.z + ")";
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector3; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector3; } }
    }

    [Serializable]
    public class Vector4MaterialSlot : MaterialSlot
    {
        [SerializeField]
        private Vector4 m_Value;

        [SerializeField]
        private Vector4 m_DefaultValue;

        public Vector4MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Vector4 value,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            :base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
            m_Value = value;
        }

        public Vector4 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return precision + "4 (" + value.x + "," + value.y + "," + value.z + "," + value.w + ")";
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector4; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector4; } }
    }

    [Serializable]
    public class Matrix2MaterialSlot : MaterialSlot
    {
        public Matrix2MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            :base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
        }
        
        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return precision + "2x2 (1,0,0,1)";
        }

        public override SlotValueType valueType { get { return SlotValueType.Matrix2; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Matrix2; } }
    }

    [Serializable]
    public class Matrix3MaterialSlot : MaterialSlot
    {
        public Matrix3MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            :base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return precision + "3x3 (1,0,0,0,1,0,0,0,1)";
        }

        public override SlotValueType valueType { get { return SlotValueType.Matrix3; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Matrix3; } }
    }

    [Serializable]
    public class Matrix4MaterialSlot : MaterialSlot
    {
        public Matrix4MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            :base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
        }


        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return precision + "4x4 (1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1)";
        }

        public override SlotValueType valueType { get { return SlotValueType.Matrix4; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Matrix4; } }
    }

    [Serializable]
    public class Texture2DMaterialSlot : MaterialSlot
    {
        public Texture2DMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            :base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
        }

        public static readonly string DefaultTextureName = "ShaderGraph_DefaultTexture";

        public override SlotValueType valueType { get { return SlotValueType.Texture2D; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Texture2D; } }
    }

    [Serializable]
    public class SamplerStateMaterialSlot : MaterialSlot
    {
        public SamplerStateMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            :base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
        }

        public override SlotValueType valueType { get { return SlotValueType.SamplerState; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.SamplerState; } }
    }

    [Serializable]
    public class DynamicVectorMaterialSlot : MaterialSlot
    {
        [SerializeField]
        private Vector4 m_Value;

        [SerializeField]
        private Vector4 m_DefaultValue;

        public DynamicVectorMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Vector4 value,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            :base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
            m_Value = value;
        }

        public override SlotValueType valueType { get { return SlotValueType.Dynamic; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Error; } }
    }

    [Serializable]
    public abstract class MaterialSlot : SerializableSlot
    {
        [SerializeField]
        string m_ShaderOutputName;

        [SerializeField]
        ShaderStage m_ShaderStage;
        
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
    }
}
