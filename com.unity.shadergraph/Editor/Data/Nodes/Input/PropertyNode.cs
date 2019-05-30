using System;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Property")]
    [FormerName("UnityEditor.ShaderGraph.PropertyNode")]
    class GraphInputNode : AbstractMaterialNode, IGeneratesBodyCode, IOnAssetEnabled
    {
#region Constructors
        public GraphInputNode()
        {
            UpdateNodeAfterDeserialization();
        }
#endregion

#region Guid
        [SerializeField][FormerlySerializedAs("m_PropertyGuidSerialized")]
        private string m_GraphInputGuidSerialized;

        private Guid m_GraphInputGuid;

        public Guid graphInputGuid
        {
            get { return m_GraphInputGuid; }
            set
            {
                if (m_GraphInputGuid == value)
                    return;

                m_GraphInputGuid = value;
                var input = owner.inputs.FirstOrDefault(x => x.guid == value);
                if (input == null)
                    return;
                
                AddGraphInputPort(input);
                Dirty(ModificationScope.Topological);
            }
        }
#endregion

#region Precision
        public override bool canSetPrecision => false;
#endregion

#region Initiailization
        public void OnEnable()
        {
            var input = owner.inputs.FirstOrDefault(x => x.guid == graphInputGuid);
            if (input == null)
                return;

            AddGraphInputPort(input);
        }
#endregion

#region Port
        public const int OutputSlotId = 0;

        private void AddGraphInputPort(ShaderInput input)
        {
            switch(input.concreteShaderValueType)
            {
                case ConcreteSlotValueType.Boolean:
                    AddSlot(new BooleanMaterialSlot(OutputSlotId, input.displayName, "Out", SlotType.Output, false));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                case ConcreteSlotValueType.Vector1:
                    AddSlot(new Vector1MaterialSlot(OutputSlotId, input.displayName, "Out", SlotType.Output, 0));
                    RemoveSlotsNameNotMatching(new[] {OutputSlotId});
                    break;
                case ConcreteSlotValueType.Vector2:
                    AddSlot(new Vector2MaterialSlot(OutputSlotId, input.displayName, "Out", SlotType.Output, Vector4.zero));
                    RemoveSlotsNameNotMatching(new[] {OutputSlotId});
                    break;
                case ConcreteSlotValueType.Vector3:
                    AddSlot(new Vector3MaterialSlot(OutputSlotId, input.displayName, "Out", SlotType.Output, Vector4.zero));
                    RemoveSlotsNameNotMatching(new[] {OutputSlotId});
                    break;
                case ConcreteSlotValueType.Vector4:
                    AddSlot(new Vector4MaterialSlot(OutputSlotId, input.displayName, "Out", SlotType.Output, Vector4.zero));
                    RemoveSlotsNameNotMatching(new[] {OutputSlotId});
                    break;
                case ConcreteSlotValueType.Matrix2:
                    AddSlot(new Matrix2MaterialSlot(OutputSlotId, input.displayName, "Out", SlotType.Output));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                case ConcreteSlotValueType.Matrix3:
                    AddSlot(new Matrix3MaterialSlot(OutputSlotId, input.displayName, "Out", SlotType.Output));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                case ConcreteSlotValueType.Matrix4:
                    AddSlot(new Matrix4MaterialSlot(OutputSlotId, input.displayName, "Out", SlotType.Output));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                case ConcreteSlotValueType.Texture2D:
                    AddSlot(new Texture2DMaterialSlot(OutputSlotId, input.displayName, "Out", SlotType.Output));
                    RemoveSlotsNameNotMatching(new[] {OutputSlotId});
                    break;
                case ConcreteSlotValueType.Texture2DArray:
                    AddSlot(new Texture2DArrayMaterialSlot(OutputSlotId, input.displayName, "Out", SlotType.Output));
                    RemoveSlotsNameNotMatching(new[] {OutputSlotId});
                    break;
                case ConcreteSlotValueType.Texture3D:
                    AddSlot(new Texture3DMaterialSlot(OutputSlotId, input.displayName, "Out", SlotType.Output));
                    RemoveSlotsNameNotMatching(new[] {OutputSlotId});
                    break;
                case ConcreteSlotValueType.Cubemap:
                    AddSlot(new CubemapMaterialSlot(OutputSlotId, input.displayName, "Out", SlotType.Output));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                case ConcreteSlotValueType.SamplerState:
                    AddSlot(new SamplerStateMaterialSlot(OutputSlotId, input.displayName, "Out", SlotType.Output));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                case ConcreteSlotValueType.Gradient:
                    AddSlot(new GradientMaterialSlot(OutputSlotId, input.displayName, "Out", SlotType.Output));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
#endregion

#region CodeGeneration
        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            if (owner.inputs.FirstOrDefault(x => x.guid == graphInputGuid) is AbstractShaderProperty property)
                GeneratePropertyNodeCode(property, sb, generationMode);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            if (owner.inputs.FirstOrDefault(x => x.guid == graphInputGuid) is AbstractShaderProperty property)
            {
                if (!(property is TextureShaderProperty) &&
                    !(property is Texture2DArrayShaderProperty) &&
                    !(property is Texture3DShaderProperty) &&
                    !(property is CubemapShaderProperty))
                    return base.GetVariableNameForSlot(slotId);

                return property.referenceName;
            }

            return "Error";
        }
#endregion
        
#region Property
        private void GeneratePropertyNodeCode(AbstractShaderProperty property, ShaderStringBuilder sb, GenerationMode generationMode)
        {
            switch(property)
            {
                case BooleanShaderProperty booleanProp:
                    sb.AppendLine($"$precision {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case Vector1ShaderProperty vector1Prop:
                    sb.AppendLine($"$precision {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case Vector2ShaderProperty vector2Prop:
                    sb.AppendLine($"$precision2 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case Vector3ShaderProperty vector3Prop:
                    sb.AppendLine($"$precision3 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case Vector4ShaderProperty vector4Prop:
                    sb.AppendLine($"$precision4 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case ColorShaderProperty colorProp:
                    sb.AppendLine($"$precision4 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case Matrix2ShaderProperty matrix2Prop:
                    sb.AppendLine($"$precision2x2 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case Matrix3ShaderProperty matrix3Prop:
                    sb.AppendLine($"$precision3x3 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case Matrix4ShaderProperty matrix4Prop:
                    sb.AppendLine($"$precision4x4 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case SamplerStateShaderProperty samplerProp:
                    sb.AppendLine($"SamplerState {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case GradientShaderProperty gradientProp:
                    if(generationMode == GenerationMode.Preview)
                        sb.AppendLine($"Gradient {GetVariableNameForSlot(OutputSlotId)} = {GradientUtils.GetGradientForPreview(property.referenceName)};");
                    else
                        sb.AppendLine($"Gradient {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
#endregion

#region Validation
        protected override bool CalculateNodeHasError(ref string errorMessage)
        {
            if (!graphInputGuid.Equals(Guid.Empty) && !owner.inputs.Any(x => x.guid == graphInputGuid))
                return true;

            return false;
        }

        public override bool ValidateConcretePrecision(ref string errorMessage)
        {
            // Get precision from Property
            if (owner.inputs.FirstOrDefault(x => x.guid == graphInputGuid) is AbstractShaderProperty property)
            {
                precision = property.precision;
                if (precision != Precision.Inherit)
                    concretePrecision = precision.ToConcrete();
                else
                    concretePrecision = owner.concretePrecision;
                return false;
            }
            
            return false;
        }
#endregion

#region Serialization
        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_GraphInputGuidSerialized = m_GraphInputGuid.ToString();
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (!string.IsNullOrEmpty(m_GraphInputGuidSerialized))
                m_GraphInputGuid = new Guid(m_GraphInputGuidSerialized);
        }
#endregion
        
    }
}
