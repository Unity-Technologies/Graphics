using System;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Property")]
    class PropertyNode : AbstractMaterialNode, IGeneratesBodyCode, IOnAssetEnabled
    {
        private Guid m_PropertyGuid;

        [SerializeField]
        private string m_PropertyGuidSerialized;

        public const int OutputSlotId = 0;

        public PropertyNode()
        {
            name = "Property";
            UpdateNodeAfterDeserialization();
        }

        public override bool canSetPrecision
        {
            get { return false; }
        }

        private void UpdateNode()
        {
            var graph = owner as GraphData;
            var property = graph.properties.FirstOrDefault(x => x.guid == propertyGuid);
            if (property == null)
                return;

            if (property is Vector1ShaderProperty)
            {
                AddSlot(new Vector1MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output, 0));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is Vector2ShaderProperty)
            {
                AddSlot(new Vector2MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is Vector3ShaderProperty)
            {
                AddSlot(new Vector3MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is Vector4ShaderProperty)
            {
                AddSlot(new Vector4MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is ColorShaderProperty)
            {
                AddSlot(new Vector4MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is TextureShaderProperty)
            {
                AddSlot(new Texture2DMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is Texture2DArrayShaderProperty)
            {
                AddSlot(new Texture2DArrayMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is Texture3DShaderProperty)
            {
                AddSlot(new Texture3DMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is CubemapShaderProperty)
            {
                AddSlot(new CubemapMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
            else if (property is BooleanShaderProperty)
            {
                AddSlot(new BooleanMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output, false));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
            else if (property is Matrix2ShaderProperty)
            {
                AddSlot(new Matrix2MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
            else if (property is Matrix3ShaderProperty)
            {
                AddSlot(new Matrix3MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
            else if (property is Matrix4ShaderProperty)
            {
                AddSlot(new Matrix4MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
            else if (property is SamplerStateShaderProperty)
            {
                AddSlot(new SamplerStateMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
            else if (property is GradientShaderProperty)
            {
                AddSlot(new GradientMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            var graph = owner as GraphData;
            var property = graph.properties.FirstOrDefault(x => x.guid == propertyGuid);
            if (property == null)
                return;

            if (property is Vector1ShaderProperty)
            {
                var result = string.Format("$precision {0} = {1};"
                        , GetVariableNameForSlot(OutputSlotId)
                        , property.referenceName);
                sb.AppendLine(result);
            }
            else if (property is Vector2ShaderProperty)
            {
                var result = string.Format("$precision2 {0} = {1};"
                        , GetVariableNameForSlot(OutputSlotId)
                        , property.referenceName);
                sb.AppendLine(result);
            }
            else if (property is Vector3ShaderProperty)
            {
                var result = string.Format("$precision3 {0} = {1};"
                        , GetVariableNameForSlot(OutputSlotId)
                        , property.referenceName);
                sb.AppendLine(result);
            }
            else if (property is Vector4ShaderProperty)
            {
                var result = string.Format("$precision4 {0} = {1};"
                        , GetVariableNameForSlot(OutputSlotId)
                        , property.referenceName);
                sb.AppendLine(result);
            }
            else if (property is ColorShaderProperty)
            {
                var result = string.Format("$precision4 {0} = {1};"
                        , GetVariableNameForSlot(OutputSlotId)
                        , property.referenceName);
                sb.AppendLine(result);
            }
            else if (property is BooleanShaderProperty)
            {
                var result = string.Format("$precision {0} = {1};"
                        , GetVariableNameForSlot(OutputSlotId)
                        , property.referenceName);
                sb.AppendLine(result);
            }
            else if (property is Matrix2ShaderProperty)
            {
                var result = string.Format("$precision2x2 {0} = {1};"
                        , GetVariableNameForSlot(OutputSlotId)
                        , property.referenceName);
                sb.AppendLine(result);
            }
            else if (property is Matrix3ShaderProperty)
            {
                var result = string.Format("$precision3x3 {0} = {1};"
                        , GetVariableNameForSlot(OutputSlotId)
                        , property.referenceName);
                sb.AppendLine(result);
            }
            else if (property is Matrix4ShaderProperty)
            {
                var result = string.Format("$precision4x4 {0} = {1};"
                        , GetVariableNameForSlot(OutputSlotId)
                        , property.referenceName);
                sb.AppendLine(result);
            }
            else if (property is SamplerStateShaderProperty)
            {
                SamplerStateShaderProperty samplerStateProperty = property as SamplerStateShaderProperty;
                var result = string.Format("SamplerState {0} = {1}_{2}_{3};"
                        , GetVariableNameForSlot(OutputSlotId)
                        , samplerStateProperty.referenceName
                        , samplerStateProperty.value.filter
                        , samplerStateProperty.value.wrap);
                sb.AppendLine(result);
            }
            else if (property is GradientShaderProperty)
            {
                if(generationMode == GenerationMode.Preview)
                {
                    var result = string.Format("Gradient {0} = {1};"
                        , GetVariableNameForSlot(OutputSlotId) 
                        , GradientUtils.GetGradientForPreview(property.referenceName));
                    sb.AppendLine(result);
                }
                else
                {
                    var result = string.Format("Gradient {0} = {1};"
                        , GetVariableNameForSlot(OutputSlotId)
                        , property.referenceName);
                    sb.AppendLine(result);
                }
            }
        }

        public Guid propertyGuid
        {
            get { return m_PropertyGuid; }
            set
            {
                if (m_PropertyGuid == value)
                    return;

                var graph = owner as GraphData;
                var property = graph.properties.FirstOrDefault(x => x.guid == value);
                if (property == null)
                    return;
                m_PropertyGuid = value;

                UpdateNode();

                Dirty(ModificationScope.Topological);
            }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            var graph = owner as GraphData;
            var property = graph.properties.FirstOrDefault(x => x.guid == propertyGuid);

            if (!(property is TextureShaderProperty) &&
                !(property is Texture2DArrayShaderProperty) &&
                !(property is Texture3DShaderProperty) &&
                !(property is CubemapShaderProperty))
                return base.GetVariableNameForSlot(slotId);

            return property.referenceName;
        }

        protected override bool CalculateNodeHasError(ref string errorMessage)
        {
            var graph = owner as GraphData;

            if (!propertyGuid.Equals(Guid.Empty) && !graph.properties.Any(x => x.guid == propertyGuid))
                return true;

            return false;
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

        public void OnEnable()
        {
            UpdateNode();
        }

        public override bool ValidateConcretePrecision(ref string errorMessage)
        {
            // Get precision from Property
            var property = owner.properties.FirstOrDefault(x => x.guid == propertyGuid);
            if (property == null)
                return true;

            precision = property.precision;

            // If Property has a precision override use that
            if (precision != Precision.Inherit)
            {
                concretePrecision = precision.ToConcrete();
                return false;
            }
            else
            {
                concretePrecision = owner.concretePrecision;
                return false;
            }
        }
    }
}
