//using System.Reflection;
using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;
using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEngine.Pool;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Multiply")]
    class MultiplyNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        public MultiplyNode()
        {
            name = "Multiply";
            UpdateNodeAfterDeserialization();
        }

        const int Input1SlotId = 0;
        const int Input2SlotId = 1;
        const int OutputSlotId = 2;
        const string kInput1SlotName = "A";
        const string kInput2SlotName = "B";
        const string kOutputSlotName = "Out";

        enum MultiplyType
        {
            Vector,
            Matrix,
            Mixed
        }

        MultiplyType m_MultiplyType;

        public override bool hasPreview => true;

        string GetFunctionName()
        {
            return $"Unity_Multiply_{FindSlot<MaterialSlot>(Input1SlotId).concreteValueType.ToShaderString()}_{FindSlot<MaterialSlot>(Input2SlotId).concreteValueType.ToShaderString()}";
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicValueMaterialSlot(Input1SlotId, kInput1SlotName, kInput1SlotName, SlotType.Input, Matrix4x4.zero));
            AddSlot(new DynamicValueMaterialSlot(Input2SlotId, kInput2SlotName, kInput2SlotName, SlotType.Input, new Matrix4x4(new Vector4(2, 2, 2, 2), new Vector4(2, 2, 2, 2), new Vector4(2, 2, 2, 2), new Vector4(2, 2, 2, 2))));
            AddSlot(new DynamicValueMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Matrix4x4.zero));
            RemoveSlotsNameNotMatching(new[] { Input1SlotId, Input2SlotId, OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var input1Value = GetSlotValue(Input1SlotId, generationMode);
            var input2Value = GetSlotValue(Input2SlotId, generationMode);
            var outputValue = GetSlotValue(OutputSlotId, generationMode);

            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputSlotId));
            sb.AppendLine("{0}({1}, {2}, {3});", GetFunctionName(), input1Value, input2Value, outputValue);
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            var functionName = GetFunctionName();

            registry.ProvideFunction(functionName, s =>
            {
                s.AppendLine("void {0}({1} A, {2} B, out {3} Out)",
                    functionName,
                    FindInputSlot<MaterialSlot>(Input1SlotId).concreteValueType.ToShaderString(),
                    FindInputSlot<MaterialSlot>(Input2SlotId).concreteValueType.ToShaderString(),
                    FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString());     // TODO: should this type be part of the function name?
                                                                                                        // is output concrete value type related to node's concrete precision??
                using (s.BlockScope())
                {
                    switch (m_MultiplyType)
                    {
                        case MultiplyType.Vector:
                            s.AppendLine("Out = A * B;");
                            break;
                        default:
                            s.AppendLine("Out = mul(A, B);");
                            break;
                    }
                }
            });
        }

        // Internal validation
        // -------------------------------------------------

        public override void EvaluateDynamicMaterialSlots(List<MaterialSlot> inputSlots, List<MaterialSlot> outputSlots)
        {
            var dynamicInputSlotsToCompare = DictionaryPool<DynamicValueMaterialSlot, ConcreteSlotValueType>.Get();
            var skippedDynamicSlots = ListPool<DynamicValueMaterialSlot>.Get();

            // iterate the input slots
            {
                foreach (var inputSlot in inputSlots)
                {
                    inputSlot.hasError = false;

                    // if there is a connection
                    var edges = owner.GetEdges(inputSlot.slotReference).ToList();
                    if (!edges.Any())
                    {
                        if (inputSlot is DynamicValueMaterialSlot)
                            skippedDynamicSlots.Add(inputSlot as DynamicValueMaterialSlot);
                        continue;
                    }

                    // get the output details
                    var outputSlotRef = edges[0].outputSlot;
                    var outputNode = outputSlotRef.node;
                    if (outputNode == null)
                        continue;

                    var outputSlot = outputNode.FindOutputSlot<MaterialSlot>(outputSlotRef.slotId);
                    if (outputSlot == null)
                        continue;

                    if (outputSlot.hasError)
                    {
                        inputSlot.hasError = true;
                        continue;
                    }

                    var outputConcreteType = outputSlot.concreteValueType;
                    // dynamic input... depends on output from other node.
                    // we need to compare ALL dynamic inputs to make sure they
                    // are compatable.
                    if (inputSlot is DynamicValueMaterialSlot)
                    {
                        dynamicInputSlotsToCompare.Add((DynamicValueMaterialSlot)inputSlot, outputConcreteType);
                        continue;
                    }
                }

                m_MultiplyType = GetMultiplyType(dynamicInputSlotsToCompare.Values);

                // Resolve dynamics depending on matrix/vector configuration
                switch (m_MultiplyType)
                {
                    // If all matrix resolve as per dynamic matrix
                    case MultiplyType.Matrix:
                        var dynamicMatrixType = ConvertDynamicMatrixInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
                        foreach (var dynamicKvP in dynamicInputSlotsToCompare)
                            dynamicKvP.Key.SetConcreteType(dynamicMatrixType);
                        foreach (var skippedSlot in skippedDynamicSlots)
                            skippedSlot.SetConcreteType(dynamicMatrixType);
                        break;
                    // If mixed handle differently:
                    // Iterate all slots and set their concretes based on their edges
                    // Find matrix slot and convert its type to a vector type
                    // Reiterate all slots and set non matrix slots to the vector type
                    case MultiplyType.Mixed:
                        foreach (var dynamicKvP in dynamicInputSlotsToCompare)
                        {
                            SetConcreteValueTypeFromEdge(dynamicKvP.Key);
                        }
                        MaterialSlot matrixSlot = GetMatrixSlot();
                        ConcreteSlotValueType vectorType = SlotValueHelper.ConvertMatrixToVectorType(matrixSlot.concreteValueType);
                        foreach (var dynamicKvP in dynamicInputSlotsToCompare)
                        {
                            if (dynamicKvP.Key != matrixSlot)
                                dynamicKvP.Key.SetConcreteType(vectorType);
                        }
                        foreach (var skippedSlot in skippedDynamicSlots)
                        {
                            skippedSlot.SetConcreteType(vectorType);
                        }
                        break;
                    // If all vector resolve as per dynamic vector
                    default:
                        var dynamicVectorType = ConvertDynamicVectorInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
                        foreach (var dynamicKvP in dynamicInputSlotsToCompare)
                            dynamicKvP.Key.SetConcreteType(dynamicVectorType);
                        foreach (var skippedSlot in skippedDynamicSlots)
                            skippedSlot.SetConcreteType(dynamicVectorType);
                        break;
                }

                bool inputError = inputSlots.Any(x => x.hasError);
                if (inputError)
                {
                    owner.AddConcretizationError(objectId, string.Format("Node {0} had input error", objectId));
                    hasError = true;
                }
                // configure the output slots now
                // their slotType will either be the default output slotType
                // or the above dynanic slotType for dynamic nodes
                // or error if there is an input error
                foreach (var outputSlot in outputSlots)
                {
                    outputSlot.hasError = false;

                    if (inputError)
                    {
                        outputSlot.hasError = true;
                        continue;
                    }

                    if (outputSlot is DynamicValueMaterialSlot)
                    {
                        // Apply similar logic to output slot
                        switch (m_MultiplyType)
                        {
                            // As per dynamic matrix
                            case MultiplyType.Matrix:
                                var dynamicMatrixType = ConvertDynamicMatrixInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
                                (outputSlot as DynamicValueMaterialSlot).SetConcreteType(dynamicMatrixType);
                                break;
                            // Mixed configuration
                            // Find matrix slot and convert type to vector
                            // Set output concrete to vector
                            case MultiplyType.Mixed:
                                MaterialSlot matrixSlot = GetMatrixSlot();
                                ConcreteSlotValueType vectorType = SlotValueHelper.ConvertMatrixToVectorType(matrixSlot.concreteValueType);
                                (outputSlot as DynamicValueMaterialSlot).SetConcreteType(vectorType);
                                break;
                            // As per dynamic vector
                            default:
                                var dynamicVectorType = ConvertDynamicVectorInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
                                (outputSlot as DynamicValueMaterialSlot).SetConcreteType(dynamicVectorType);
                                break;
                        }
                        continue;
                    }
                }

                if (outputSlots.Any(x => x.hasError))
                {
                    owner.AddConcretizationError(objectId, string.Format("Node {0} had output error", objectId));
                    hasError = true;
                }
            }

            CalculateNodeHasError();

            ListPool<DynamicValueMaterialSlot>.Release(skippedDynamicSlots);
            DictionaryPool<DynamicValueMaterialSlot, ConcreteSlotValueType>.Release(dynamicInputSlotsToCompare);
        }

        private MultiplyType GetMultiplyType(IEnumerable<ConcreteSlotValueType> inputTypes)
        {
            var concreteSlotValueTypes = inputTypes as List<ConcreteSlotValueType> ?? inputTypes.ToList();
            int matrixCount = 0;
            int vectorCount = 0;
            for (int i = 0; i < concreteSlotValueTypes.Count; i++)
            {
                if (concreteSlotValueTypes[i] == ConcreteSlotValueType.Vector4
                    || concreteSlotValueTypes[i] == ConcreteSlotValueType.Vector3
                    || concreteSlotValueTypes[i] == ConcreteSlotValueType.Vector2
                    || concreteSlotValueTypes[i] == ConcreteSlotValueType.Vector1)
                {
                    vectorCount++;
                }
                else if (concreteSlotValueTypes[i] == ConcreteSlotValueType.Matrix4
                         || concreteSlotValueTypes[i] == ConcreteSlotValueType.Matrix3
                         || concreteSlotValueTypes[i] == ConcreteSlotValueType.Matrix2)
                {
                    matrixCount++;
                }
            }
            if (matrixCount == 2)
                return MultiplyType.Matrix;
            else if (vectorCount == 2)
                return MultiplyType.Vector;
            else if (matrixCount == 1)
                return MultiplyType.Mixed;
            else
                return MultiplyType.Vector;
        }

        private MaterialSlot GetMatrixSlot()
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetInputSlots(slots);
            for (int i = 0; i < slots.Count; i++)
            {
                var edges = owner.GetEdges(slots[i].slotReference).ToList();
                if (!edges.Any())
                    continue;
                var outputNode = edges[0].outputSlot.node;
                var outputSlot = outputNode.FindOutputSlot<MaterialSlot>(edges[0].outputSlot.slotId);
                if (outputSlot.concreteValueType == ConcreteSlotValueType.Matrix4
                    || outputSlot.concreteValueType == ConcreteSlotValueType.Matrix3
                    || outputSlot.concreteValueType == ConcreteSlotValueType.Matrix2)
                    return slots[i];
            }
            return null;
        }

        private void SetConcreteValueTypeFromEdge(DynamicValueMaterialSlot slot)
        {
            var edges = owner.GetEdges(slot.slotReference).ToList();
            if (!edges.Any())
                return;
            var outputNode = edges[0].outputSlot.node;
            var outputSlot = outputNode.FindOutputSlot<MaterialSlot>(edges[0].outputSlot.slotId);
            slot.SetConcreteType(outputSlot.concreteValueType);
        }
    }
}
