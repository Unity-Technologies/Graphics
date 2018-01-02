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
                Dirty(ModificationScope.Graph);
            }
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlotId }, new[] { OutputSlotId });
            string inputValue = string.Format("{0}.xyz", GetSlotValue(InputSlotId, generationMode));
            string targetTransformString = "tangentTransform_" + conversion.from.ToString();
            string transposeTargetTransformString = "transposeTangent";
            string transformString = "";
            string tangentTransformSpace = conversion.from.ToString();
            bool requiresTangentTransform = false;
            bool requiresTransposeTangentTransform = false;

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
                    transformString = "mul(" + inputValue + ", " + targetTransformString + ").xyz";
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
                    transformString = "mul(float4(" + inputValue + ", 0).xyz, " + targetTransformString + ").xyz";
                }
                else if (conversion.to == CoordinateSpace.View)
                {
                    transformString = "mul( UNITY_MATRIX_MV, float4(" + inputValue + ", 0)).xyz";
                }
            }
            else if (conversion.from == CoordinateSpace.Tangent)
            {
                if (conversion.to == CoordinateSpace.World)
                {
                    requiresTransposeTangentTransform = true;
                    transformString = "mul( " + inputValue + ", " + transposeTargetTransformString + " ).xyz";
                }
                else if (conversion.to == CoordinateSpace.Object)
                {
                    requiresTransposeTangentTransform = true;
                    transformString = "mul( unity_WorldToObject, float4(mul(" + inputValue + ", " + transposeTargetTransformString + " ),0) ).xyz";
                }
                else if (conversion.to == CoordinateSpace.Tangent)
                {
                    transformString = inputValue;
                }
                else if (conversion.to == CoordinateSpace.View)
                {
                    requiresTransposeTangentTransform = true;
                    transformString = "mul( UNITY_MATRIX_V, float4(mul(" + inputValue + ", " + transposeTargetTransformString + " ),0) ).xyz";
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
                    tangentTransformSpace = CoordinateSpace.World.ToString();
                    transformString = "mul( mul( float4(" + inputValue + ", 0), UNITY_MATRIX_V ).xyz, " + targetTransformString + ").xyz";
                }
                else if (conversion.to == CoordinateSpace.View)
                {
                    transformString = inputValue;
                }
            }

            if (requiresTransposeTangentTransform)
                visitor.AddShaderChunk("float3x3 " + transposeTargetTransformString + " = transpose(float3x3(" + CoordinateSpace.World.ToString() + "SpaceTangent, " + CoordinateSpace.World.ToString() + "SpaceBiTangent, " + CoordinateSpace.World.ToString() + "SpaceNormal));", true);
            else if (requiresTangentTransform)
                visitor.AddShaderChunk("float3x3 " + targetTransformString + " = float3x3(" + tangentTransformSpace + "SpaceTangent, " + tangentTransformSpace + "SpaceBiTangent, " + tangentTransformSpace + "SpaceNormal);", true);

            visitor.AddShaderChunk(string.Format("{0} {1} = {2};", NodeUtils.ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType),
                    GetVariableNameForSlot(OutputSlotId),
                    transformString), true);
        }

        bool RequiresWorldSpaceTangentTransform()
        {
            if (conversion.from == CoordinateSpace.View && conversion.to == CoordinateSpace.Tangent
                || conversion.from == CoordinateSpace.Tangent)
                return true;
            else
                return false;
        }

        public NeededCoordinateSpace RequiresTangent()
        {
            if(RequiresWorldSpaceTangentTransform())
                return NeededCoordinateSpace.World;
            return conversion.from.ToNeededCoordinateSpace();
        }

        public NeededCoordinateSpace RequiresBitangent()
        {
            if (RequiresWorldSpaceTangentTransform())
                return NeededCoordinateSpace.World;
            return conversion.from.ToNeededCoordinateSpace();
        }

        public NeededCoordinateSpace RequiresNormal()
        {
            if (RequiresWorldSpaceTangentTransform())
                return NeededCoordinateSpace.World;
            return conversion.from.ToNeededCoordinateSpace();
        }
    }
}
