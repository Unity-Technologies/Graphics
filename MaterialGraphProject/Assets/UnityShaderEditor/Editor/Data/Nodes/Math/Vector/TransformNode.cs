using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Vector/Transform")]
    public class TransformNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireTangent, IMayRequireBitangent, IMayRequireNormal
    {
        [SerializeField]
        private CoordinateSpace m_spaceListFrom;
        [SerializeField]
        private CoordinateSpace m_spaceListTo;

        private const int InputSlotId = 0;
        private const int OutputSlotId = 1;
        private const string kInputSlotName = "In";
        private const string kOutputSlotName = "Out";

        public TransformNode()
        {
            name = "Transform";
            UpdateNodeAfterDeserialization();
        }

        [EnumControl("From")]
        public CoordinateSpace spaceFrom
        {
            get { return m_spaceListFrom; }
            set
            {
                if (m_spaceListFrom == value)
                    return;

                m_spaceListFrom = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        [EnumControl("To")]
        public CoordinateSpace spaceTo
        {
            get { return m_spaceListTo; }
            set
            {
                if (m_spaceListTo == value)
                    return;

                m_spaceListTo = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(GetInputSlot());
            AddSlot(GetOutputSlot());
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] {InputSlotId, OutputSlotId}; }
        }

        protected virtual MaterialSlot GetInputSlot()
        {
            return new Vector3MaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector3.zero);
        }

        protected virtual MaterialSlot GetOutputSlot()
        {
            return new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlotId }, new[] { OutputSlotId });
            string inputValue = GetSlotValue(InputSlotId, generationMode);
            string transformString = "";
            bool requiresTangentTransform = false;

            if (spaceFrom == CoordinateSpace.World)
            {
                if (spaceTo == CoordinateSpace.World)
                {
                    transformString = inputValue;
                }
                else if (spaceTo == CoordinateSpace.Object)
                {
                    transformString = "mul(unity_WorldToObject, float4(" + inputValue + ", 0)).xyz";
                }
                else if (spaceTo == CoordinateSpace.Tangent)
                {
                    requiresTangentTransform = true;
                    transformString = "mul(tangentTransform, " + inputValue + ").xyz";
                }
                else if (spaceTo == CoordinateSpace.View)
                {
                    transformString = "mul( UNITY_MATRIX_V, float4(" + inputValue + ", 0)).xyz";
                }
            }
            else if (spaceFrom == CoordinateSpace.Object)
            {
                if (spaceTo == CoordinateSpace.World)
                {
                    transformString = "mul(unity_ObjectToWorld, float4(" + inputValue + ", 0)).xyz";
                }
                else if (spaceTo == CoordinateSpace.Object)
                {
                    transformString = inputValue;
                }
                else if (spaceTo == CoordinateSpace.Tangent)
                {
                    requiresTangentTransform = true;
                    transformString = "mul( tangentTransform, mul( unity_ObjectToWorld, float4(" + inputValue + ", 0) ).xyz).xyz";
                }
                else if (spaceTo == CoordinateSpace.View)
                {
                    transformString = "mul( UNITY_MATRIX_MV, float4(" + inputValue + ", 0)).xyz";
                }
            }
            else if (spaceFrom == CoordinateSpace.Tangent)
            {
                requiresTangentTransform = true;
                if (spaceTo == CoordinateSpace.World)
                {
                    transformString = "mul( " + inputValue + ", tangentTransform ).xyz";
                }
                else if (spaceTo == CoordinateSpace.Object)
                {
                    transformString = "mul( unity_WorldToObject, float4(mul(" + inputValue + ", tangentTransform ),0) ).xyz";
                }
                else if (spaceTo == CoordinateSpace.Tangent)
                {
                    transformString = inputValue;
                }
                else if (spaceTo == CoordinateSpace.View)
                {
                    transformString = "mul( UNITY_MATRIX_V, float4(mul(" + inputValue + ", tangentTransform ),0) ).xyz";
                }
            }
            else if (spaceFrom == CoordinateSpace.View)
            {
                if (spaceTo == CoordinateSpace.World)
                {
                    transformString = "mul( float4(" + inputValue + ", 0), UNITY_MATRIX_V ).xyz";
                }
                else if (spaceTo == CoordinateSpace.Object)
                {
                    transformString = "mul( float4(" + inputValue + ", 0), UNITY_MATRIX_MV ).xyz";
                }
                else if (spaceTo == CoordinateSpace.Tangent)
                {
                    requiresTangentTransform = true;
                    transformString = "mul( tangentTransform, mul( float4(" + inputValue + ", 0), UNITY_MATRIX_V ).xyz ).xyz";
                }
                else if (spaceTo == CoordinateSpace.View)
                {
                    transformString = inputValue;
                }
            }

            if (requiresTangentTransform)
                visitor.AddShaderChunk("float3x3 tangentTransform = float3x3(" + spaceFrom.ToString() + "SpaceTangent, " + spaceFrom.ToString() + "SpaceBiTangent, " + spaceFrom.ToString() + "SpaceNormal);", false);

            visitor.AddShaderChunk(string.Format("{0} {1} = {2};",
                    ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType),
                    GetVariableNameForSlot(OutputSlotId),
                    transformString), true);
        }

        public NeededCoordinateSpace RequiresTangent()
        {
            return spaceFrom.ToNeededCoordinateSpace();
        }

        public NeededCoordinateSpace RequiresBitangent()
        {
            return spaceFrom.ToNeededCoordinateSpace();
        }

        public NeededCoordinateSpace RequiresNormal()
        {
            return spaceFrom.ToNeededCoordinateSpace();
        }
    }
}
