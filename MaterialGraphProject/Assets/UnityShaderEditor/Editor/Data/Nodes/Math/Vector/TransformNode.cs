using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public struct CoordinateSpaceConversion : IEnumConversion
    {
        public CoordinateSpace from;
        public CoordinateSpace to;

        public CoordinateSpaceConversion(CoordinateSpace from, CoordinateSpace to)
        {
            this.from = from;
            this.to = to;
        }

        Enum IEnumConversion.from
        {
            get { return from; }
            set { from = (CoordinateSpace)value; }
        }

        Enum IEnumConversion.to
        {
            get { return to; }
            set { to = (CoordinateSpace)value; }
        }
    }

    [Title("Math", "Vector", "Transform")]
    public class TransformNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireTangent, IMayRequireBitangent, IMayRequireNormal
    {
        private const int InputSlotId = 0;
        private const int OutputSlotId = 1;
        private const string kInputSlotName = "In";
        private const string kOutputSlotName = "Out";

        public TransformNode()
        {
            name = "Transform";
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        CoordinateSpaceConversion m_Conversion = new CoordinateSpaceConversion(CoordinateSpace.Object, CoordinateSpace.World);

        [EnumConversionControl]
        public CoordinateSpaceConversion conversion
        {
            get { return m_Conversion; }
            set
            {
                if (Equals(m_Conversion, value))
                    return;
                m_Conversion = value;
                if (onModified != null)
                    onModified(this, ModificationScope.Graph);
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
            get { return new[] { InputSlotId, OutputSlotId }; }
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

            if (conversion.from == CoordinateSpace.World)
            {
                if (conversion.to == CoordinateSpace.World)
                {
                    transformString = inputValue;
                }
                else if (conversion.to == CoordinateSpace.Object)
                {
                    transformString = "mul(unity_WorldToObject, float4(" + inputValue + ", 0)).xyz";
                }
                else if (conversion.to == CoordinateSpace.Tangent)
                {
                    requiresTangentTransform = true;
                    transformString = "mul(tangentTransform, " + inputValue + ").xyz";
                }
                else if (conversion.to == CoordinateSpace.View)
                {
                    transformString = "mul( UNITY_MATRIX_V, float4(" + inputValue + ", 0)).xyz";
                }
            }
            else if (conversion.from == CoordinateSpace.Object)
            {
                if (conversion.to == CoordinateSpace.World)
                {
                    transformString = "mul(unity_ObjectToWorld, float4(" + inputValue + ", 0)).xyz";
                }
                else if (conversion.to == CoordinateSpace.Object)
                {
                    transformString = inputValue;
                }
                else if (conversion.to == CoordinateSpace.Tangent)
                {
                    requiresTangentTransform = true;
                    transformString = "mul( tangentTransform, mul( unity_ObjectToWorld, float4(" + inputValue + ", 0) ).xyz).xyz";
                }
                else if (conversion.to == CoordinateSpace.View)
                {
                    transformString = "mul( UNITY_MATRIX_MV, float4(" + inputValue + ", 0)).xyz";
                }
            }
            else if (conversion.from == CoordinateSpace.Tangent)
            {
                requiresTangentTransform = true;
                if (conversion.to == CoordinateSpace.World)
                {
                    transformString = "mul( " + inputValue + ", tangentTransform ).xyz";
                }
                else if (conversion.to == CoordinateSpace.Object)
                {
                    transformString = "mul( unity_WorldToObject, float4(mul(" + inputValue + ", tangentTransform ),0) ).xyz";
                }
                else if (conversion.to == CoordinateSpace.Tangent)
                {
                    transformString = inputValue;
                }
                else if (conversion.to == CoordinateSpace.View)
                {
                    transformString = "mul( UNITY_MATRIX_V, float4(mul(" + inputValue + ", tangentTransform ),0) ).xyz";
                }
            }
            else if (conversion.from == CoordinateSpace.View)
            {
                if (conversion.to == CoordinateSpace.World)
                {
                    transformString = "mul( float4(" + inputValue + ", 0), UNITY_MATRIX_V ).xyz";
                }
                else if (conversion.to == CoordinateSpace.Object)
                {
                    transformString = "mul( float4(" + inputValue + ", 0), UNITY_MATRIX_MV ).xyz";
                }
                else if (conversion.to == CoordinateSpace.Tangent)
                {
                    requiresTangentTransform = true;
                    transformString = "mul( tangentTransform, mul( float4(" + inputValue + ", 0), UNITY_MATRIX_V ).xyz ).xyz";
                }
                else if (conversion.to == CoordinateSpace.View)
                {
                    transformString = inputValue;
                }
            }

            if (requiresTangentTransform)
                visitor.AddShaderChunk("float3x3 tangentTransform = float3x3(" + conversion.from.ToString() + "SpaceTangent, " + conversion.from.ToString() + "SpaceBiTangent, " + conversion.from.ToString() + "SpaceNormal);", false);

            visitor.AddShaderChunk(string.Format("{0} {1} = {2};",
                    ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType),
                    GetVariableNameForSlot(OutputSlotId),
                    transformString), true);
        }

        public NeededCoordinateSpace RequiresTangent()
        {
            return conversion.from.ToNeededCoordinateSpace();
        }

        public NeededCoordinateSpace RequiresBitangent()
        {
            return conversion.from.ToNeededCoordinateSpace();
        }

        public NeededCoordinateSpace RequiresNormal()
        {
            return conversion.from.ToNeededCoordinateSpace();
        }
    }
}