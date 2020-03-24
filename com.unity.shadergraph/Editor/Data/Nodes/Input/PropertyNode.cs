using System;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing.Util;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Property")]
    class PropertyNode : AbstractMaterialNode, IGeneratesBodyCode, IOnAssetEnabled
    {
        public PropertyNode()
        {
            name = "Property";
            UpdateNodeAfterDeserialization();
        }
        
        [SerializeField]
        string m_PropertyGuidSerialized;

        Guid m_PropertyGuid;

        public Guid propertyGuid
        {
            get { return m_PropertyGuid; }
            set
        {
                if (m_PropertyGuid == value)
                    return;

                m_PropertyGuid = value;
                var property = owner.properties.FirstOrDefault(x => x.guid == value);
                if (property == null)
                    return;
                
                AddOutputSlot(property);
                Dirty(ModificationScope.Topological);
        }
        }
        public override bool canSetPrecision => false;

        public void OnEnable()
        {
            var property = owner.properties.FirstOrDefault(x => x.guid == propertyGuid);
            if (property == null)
                return;

            AddOutputSlot(property);
        }
        
        public const int OutputSlotId = 0;

        void AddOutputSlot(AbstractShaderProperty property)
        {
            switch(property.concreteShaderValueType)
            {
                case ConcreteSlotValueType.Boolean:
                    AddSlot(new BooleanMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output, false));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                case ConcreteSlotValueType.Vector1:
                AddSlot(new Vector1MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output, 0));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
                    break;
                case ConcreteSlotValueType.Vector2:
                AddSlot(new Vector2MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
                    break;
                case ConcreteSlotValueType.Vector3:
                AddSlot(new Vector3MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
                    break;
                case ConcreteSlotValueType.Vector4:
                AddSlot(new Vector4MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
                    break;
                case ConcreteSlotValueType.Matrix2:
                    AddSlot(new Matrix2MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                case ConcreteSlotValueType.Matrix3:
                    AddSlot(new Matrix3MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                case ConcreteSlotValueType.Matrix4:
                    AddSlot(new Matrix4MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                case ConcreteSlotValueType.Texture2D:
                AddSlot(new Texture2DMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
                    break;
                case ConcreteSlotValueType.Texture2DArray:
                AddSlot(new Texture2DArrayMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
                    break;
                case ConcreteSlotValueType.Texture3D:
                AddSlot(new Texture3DMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
                    break;
                case ConcreteSlotValueType.Cubemap:
                AddSlot(new CubemapMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                case ConcreteSlotValueType.SamplerState:
                AddSlot(new SamplerStateMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                case ConcreteSlotValueType.Gradient:
                AddSlot(new GradientMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var property = owner.properties.FirstOrDefault(x => x.guid == propertyGuid);
            if (property == null)
                return;
            
            switch(property.propertyType)
            {
                case PropertyType.Boolean:
                    sb.AppendLine($"$precision {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case PropertyType.Vector1:
                    sb.AppendLine($"$precision {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case PropertyType.Vector2:
                    sb.AppendLine($"$precision2 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case PropertyType.Vector3:
                    sb.AppendLine($"$precision3 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case PropertyType.Vector4:
                    sb.AppendLine($"$precision4 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case PropertyType.Color:
                    sb.AppendLine($"$precision4 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case PropertyType.Matrix2:
                    sb.AppendLine($"$precision2x2 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case PropertyType.Matrix3:
                    sb.AppendLine($"$precision3x3 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case PropertyType.Matrix4:
                    sb.AppendLine($"$precision4x4 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case PropertyType.SamplerState:
                    sb.AppendLine($"SamplerState {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case PropertyType.Gradient:
                if(generationMode == GenerationMode.Preview)
                        sb.AppendLine($"Gradient {GetVariableNameForSlot(OutputSlotId)} = {GradientUtil.GetGradientForPreview(property.referenceName)};");
                else
                        sb.AppendLine($"Gradient {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
            }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            var property = owner.properties.FirstOrDefault(x => x.guid == propertyGuid);
                if (property == null)
                throw new NullReferenceException();
            
            if (!(property is Texture2DShaderProperty) &&
                !(property is Texture2DArrayShaderProperty) &&
                !(property is Texture3DShaderProperty) &&
                !(property is CubemapShaderProperty))
                return base.GetVariableNameForSlot(slotId);

            return property.referenceName;
        }
        
        protected override void CalculateNodeHasError()
        {
            if (!propertyGuid.Equals(Guid.Empty) && !owner.properties.Any(x => x.guid == propertyGuid))
            {
                owner.AddConcretizationError(guid, "Property Node has no associated Blackboard property.");
            }
        }

        public override void EvaluateConcretePrecision()
        {
            // Get precision from Property
            var property = owner.properties.FirstOrDefault(x => x.guid == propertyGuid);
            if (property == null)
            {
                owner.AddConcretizationError(guid, string.Format("No matching poperty found on owner for node {0}", guid));
                hasError = true;
                return;
            }
            // If Property has a precision override use that
            precision = property.precision;
            if (precision != Precision.Inherit)
                concretePrecision = precision.ToConcrete();
            else
                concretePrecision = owner.concretePrecision;
            }
        
        public override void OnBeforeSerialize()
            {
            base.OnBeforeSerialize();
            m_PropertyGuidSerialized = m_PropertyGuid.ToString();
            }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (!string.IsNullOrEmpty(m_PropertyGuidSerialized))
                m_PropertyGuid = new Guid(m_PropertyGuidSerialized);
        }
    }
}
