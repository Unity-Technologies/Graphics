using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Channel", "Append")]
    class AppendVectorNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public AppendVectorNode()
        {
            name = "Append";
            synonyms = new string[] { "join", "combine" };
            UpdateNodeAfterDeserialization();
        }

        const int Input1SlotId = 0;
        const int Input2SlotId = 1;
        const int OutputSlotId = 2;
        const string kInput1SlotName = "A";
        const string kInput2SlotName = "B";
        const string kOutputSlotName = "Out";

        public override bool hasPreview => true;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(Input1SlotId, kInput1SlotName, kInput1SlotName, SlotType.Input, Vector4.zero));
            AddSlot(new DynamicVectorMaterialSlot(Input2SlotId, kInput2SlotName, kInput2SlotName, SlotType.Input, Vector4.zero));
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { Input1SlotId, Input2SlotId, OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var input1Value = GetSlotValue(Input1SlotId, generationMode);
            var input2Value = GetSlotValue(Input2SlotId, generationMode);
            var outputValue = GetSlotValue(OutputSlotId, generationMode);

            var outputTypeString = FindOutputSlot<DynamicVectorMaterialSlot>(OutputSlotId).concreteValueType.ToShaderString();
            var input1Type = FindInputSlot<DynamicVectorMaterialSlot>(Input1SlotId).concreteValueType;
            var input2Type = FindInputSlot<DynamicVectorMaterialSlot>(Input2SlotId).concreteValueType;

            var input1Swizzle = SwizzleFromVectorSlotType(input1Type, 3);
            var input2Swizzle = SwizzleFromVectorSlotType(input2Type, 3);

            sb.AppendLine("{0} {1} = {0}( {2}.{3}, {4}.{5} );",
                outputTypeString,
                GetVariableNameForSlot(OutputSlotId),
                input1Value,
                input1Swizzle,
                input2Value,
                input2Swizzle
            );
        }

        string SwizzleFromVectorSlotType( ConcreteSlotValueType type , uint dimensionLimit = 3)
        {
            if (dimensionLimit == 0)
                dimensionLimit = 4;

            uint typeDimension = type switch {
                ConcreteSlotValueType.Vector2 => 2,
                ConcreteSlotValueType.Vector3 => 3,
                ConcreteSlotValueType.Vector4 => 4,
                _ => 1,
            };

            if (typeDimension > dimensionLimit)
                typeDimension = dimensionLimit;

            return typeDimension switch {
                1 => "x",
                2 => "xy",
                3 => "xyz",
                _ => "xyzw",
            };
        }

        uint ProcessInputSlot(MaterialSlot inputSlot, string referenceName, uint maxDimensions = 4)
        {
            uint dimensions = 0;

            if (maxDimensions == 0)
                maxDimensions = 4;

            inputSlot.hasError = false;

            // default input type
            var outputConcreteType = ConcreteSlotValueType.Vector1;

            // if there is a connection
            var edges = owner.GetEdges(inputSlot.slotReference);
            foreach(var edge in edges)
            {
                if (edge != null)
                {
                    // get the output details
                    var outputSlotRef = edge.outputSlot;
                    var outputNode = outputSlotRef.node;
                    if (outputNode != null)
                    {
                        var outputSlot = outputNode.FindOutputSlot<MaterialSlot>(outputSlotRef.slotId);
                        if (outputSlot != null)
                        {
                            if (!outputSlot.hasError)
                            {
                                outputConcreteType = outputSlot.concreteValueType;
                            }
                        }
                    }

                    break;
                }
            }

            var dynVectorInputSlot = inputSlot as DynamicVectorMaterialSlot;

            // get the connected output dimensions and limit it if needed
            dimensions = outputConcreteType switch {
                ConcreteSlotValueType.Vector2 => 2,
                ConcreteSlotValueType.Vector3 => 3,
                ConcreteSlotValueType.Vector4 => 4,
                _ => 1,
            };

            if (dimensions > maxDimensions)
                dimensions = maxDimensions;

            outputConcreteType = dimensions switch {
                2 => ConcreteSlotValueType.Vector2,
                3 => ConcreteSlotValueType.Vector3,
                4 => ConcreteSlotValueType.Vector4,
                _ => ConcreteSlotValueType.Vector1
            };

            dynVectorInputSlot.SetConcreteType(outputConcreteType);

            return dimensions;
        }

        public override void EvaluateDynamicMaterialSlots(List<MaterialSlot> inputSlots, List<MaterialSlot> outputSlots)
        {
            uint slot1Dimensions = 1;
            uint slot2Dimensions = 1;
            uint availableDimensionsForInput2 = 4;
            uint outputVectorDimensions = 0;

            // iterate over the input slots
            int i = 0;
            foreach (var inputSlot in inputSlots)
            {
                if (i == 0)
                {
                    slot1Dimensions = ProcessInputSlot(inputSlot, kInput1SlotName, 3);
                    availableDimensionsForInput2 -= slot1Dimensions;
                }
                else if (i == 1)
                {
                    slot2Dimensions = ProcessInputSlot(inputSlot, kInput2SlotName, availableDimensionsForInput2);
                }
                else
                    break; // No other input slots should be present

                i++;
            }

            // Set the output vector dimension to the sum of the input
            outputVectorDimensions = slot1Dimensions + slot2Dimensions;
            foreach (var outputSlot in outputSlots)
            {
                (outputSlot as DynamicVectorMaterialSlot).SetConcreteType( outputVectorDimensions switch {
                    2 => ConcreteSlotValueType.Vector2,
                    3 => ConcreteSlotValueType.Vector3,
                    4 => ConcreteSlotValueType.Vector4,
                    _ => ConcreteSlotValueType.Vector1
                });
            }

            CalculateNodeHasError();
        }
    }
}
