using System;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Input", "Property")]
    class PropertyNode : AbstractMaterialNode, IGeneratesBodyCode, IOnAssetEnabled
    {
        public PropertyNode()
        {
            name = "Property";
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();

            if (owner == null)
                return;

            if (property is Vector1ShaderProperty vector1ShaderProperty && vector1ShaderProperty.floatType == FloatType.Slider)
            {
                // Previously, the Slider vector1 property allowed the min value to be greater than the max
                // We no longer want to support that behavior so if such a property is encountered, swap the values
                if (vector1ShaderProperty.rangeValues.x > vector1ShaderProperty.rangeValues.y)
                {
                    vector1ShaderProperty.rangeValues = new Vector2(vector1ShaderProperty.rangeValues.y, vector1ShaderProperty.rangeValues.x);
                    Dirty(ModificationScope.Graph);
                }
            }
        }

        [SerializeField]
        JsonRef<AbstractShaderProperty> m_Property;

        public AbstractShaderProperty property
        {
            get { return m_Property; }
            set
            {
                if (m_Property == value)
                    return;

                m_Property = value;
                AddOutputSlot();
                Dirty(ModificationScope.Topological);
            }
        }

        public override bool canSetPrecision => false;

        public void OnEnable()
        {
            AddOutputSlot();
        }

        public const int OutputSlotId = 0;

        void AddOutputSlot()
        {
            if (property is MultiJsonInternal.UnknownShaderPropertyType uspt)
            {
                // keep existing slots, don't modify them
                return;
            }
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
                case ConcreteSlotValueType.VirtualTexture:
                    AddSlot(new VirtualTextureMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            switch (property.propertyType)
            {
                case PropertyType.Boolean:
                    sb.AppendLine($"$precision {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                    break;
                case PropertyType.Float:
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
                    switch (property.sgVersion)
                    {
                        case 0:
                        case 2:
                            sb.AppendLine($"$precision4 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                            break;
                        case 1:
                        case 3:
                            //Exposed color properties get put into the correct space automagikally by Unity UNLESS tagged as HDR, then they just get passed in as is.
                            //for consistency with other places in the editor, we assume HDR colors are in linear space, and correct for gamma space here
                            if ((property as ColorShaderProperty).colorMode == ColorMode.HDR)
                            {
                                sb.AppendLine($"$precision4 {GetVariableNameForSlot(OutputSlotId)} = IsGammaSpace() ? LinearToSRGB({property.referenceName}) : {property.referenceName};");
                            }
                            else
                            {
                                sb.AppendLine($"$precision4 {GetVariableNameForSlot(OutputSlotId)} = {property.referenceName};");
                            }
                            break;
                        default:
                            throw new Exception($"Unknown Color Property Version on property {property.displayName}");
                    }
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
            
            // TODO: I don't like this exception list being buried in PropertyNode.cs, should be something on the ShaderProperty themselves...
            if (!(property is Texture2DShaderProperty) &&
                !(property is Texture2DArrayShaderProperty) &&
                !(property is Texture3DShaderProperty) &&
                !(property is CubemapShaderProperty) &&
                !(property is VirtualTextureShaderProperty))
                return base.GetVariableNameForSlot(slotId);

            return property.referenceName;
        }

        protected override void CalculateNodeHasError()
        {
            if (property == null || !owner.properties.Any(x => x == property))
            {
                owner.AddConcretizationError(objectId, "Property Node has no associated Blackboard property.");
            }
            else if (property is MultiJsonInternal.UnknownShaderPropertyType)
            {
                owner.AddValidationError(objectId, "Property is of unknown type, a package may be missing.", Rendering.ShaderCompilerMessageSeverity.Warning);
            }
        }

        public override void EvaluateConcretePrecision()
        {
            // Get precision from Property
            if (property == null)
            {
                owner.AddConcretizationError(objectId, string.Format("No matching poperty found on owner for node {0}", objectId));
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
    }
}
