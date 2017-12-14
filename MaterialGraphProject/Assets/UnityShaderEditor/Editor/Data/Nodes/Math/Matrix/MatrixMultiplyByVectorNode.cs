//using System.Reflection;
//using UnityEngine;
//using System.Collections.Generic;

using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Matrix", "Matrix Multiply Vector")]
    public class MatrixMultiplyVectorNode : /*CodeFunctionNode*/ AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        /*public MatrixMultiplyVectorNode()
        {
            name = "Matrix Multiply Vector";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            List<MaterialSlot> outSlots = new List<MaterialSlot>();
            GetOutputSlots(outSlots);
            switch (outSlots[0].concreteValueType)
            {
                case ConcreteSlotValueType.Vector1:
                    return GetType().GetMethod("Unity_MatrixMultiplyVector1", BindingFlags.Static | BindingFlags.NonPublic);
                case ConcreteSlotValueType.Vector2:
                    return GetType().GetMethod("Unity_MatrixMultiplyVector2", BindingFlags.Static | BindingFlags.NonPublic);
                case ConcreteSlotValueType.Vector3:
                    return GetType().GetMethod("Unity_MatrixMultiplyVector3", BindingFlags.Static | BindingFlags.NonPublic);
                default:
                    return GetType().GetMethod("Unity_MatrixMultiplyVector4", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_MatrixMultiplyVector1(
            [Slot(0, Binding.None)] DynamicDimensionVector Vector,
            [Slot(1, Binding.None)] DynamicDimensionMatrix Matrix,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = mul(Vector, Matrix);
}
";
        }

        static string Unity_MatrixMultiplyVector2(
    [Slot(0, Binding.None)] DynamicDimensionVector Vector,
    [Slot(1, Binding.None)] DynamicDimensionMatrix Matrix,
    [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = mul(Vector, Matrix);
}
";
        }

        static string Unity_MatrixMultiplyVector3(
    [Slot(0, Binding.None)] DynamicDimensionVector Vector,
    [Slot(1, Binding.None)] DynamicDimensionMatrix Matrix,
    [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = mul(Vector, Matrix);
}
";
        }

        static string Unity_MatrixMultiplyVector4(
    [Slot(0, Binding.None)] DynamicDimensionVector Vector,
    [Slot(1, Binding.None)] DynamicDimensionMatrix Matrix,
    [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = mul(Vector, Matrix);
}
";
        }*/

        protected const string k_InputSlot1Name = "Vector";
        protected const string k_InputSlot2Name = "Matrix";
        protected const string k_OutputSlotName = "Out";

        public const int InputSlot1Id = 0;
        public const int InputSlot2Id = 1;
        public const int OutputSlotId = 2;

        public override bool hasPreview
        {
            get { return true; }
        }

        public MatrixMultiplyVectorNode()
        {
            name = "Matrix Multiply Vector";
            UpdateNodeAfterDeserialization();
        }

        protected string GetFunctionName()
        {
            int channelCount = (int)SlotValueHelper.GetChannelCount(FindSlot<MaterialSlot>(OutputSlotId).concreteValueType);
            return "Unity_MatrixMultiplyVector_" + precision + channelCount;
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(InputSlot1Id, k_InputSlot1Name, k_InputSlot1Name, SlotType.Input, Vector4.zero));
            AddSlot(new DynamicMatrixMaterialSlot(InputSlot2Id, k_InputSlot2Name, k_InputSlot2Name, SlotType.Input));
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlot1Id, InputSlot2Id, OutputSlotId });
        }

        protected string GetFunctionPrototype(string argIn1, string argIn2, string argOut)
        {
            return string.Format("inline void {0} ({1} {2}, {3} {4}, out {5} {6})", GetFunctionName(), 
                ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlot1Id).concreteValueType), argIn1, 
                ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlot2Id).concreteValueType), argIn2,
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), argOut);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlot1Id, InputSlot2Id }, new[] { OutputSlotId });
            string input1Value = GetSlotValue(InputSlot1Id, generationMode);
            string input2Value = GetSlotValue(InputSlot2Id, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);
            visitor.AddShaderChunk(string.Format("{0} {1};", ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId)), true);
            visitor.AddShaderChunk(GetFunctionCallBody(input1Value, input2Value, outputValue), true);
        }

        protected string GetFunctionCallBody(string input1Value, string input2Value, string outputValue)
        {
            return string.Format("{0} ({1}, {2}, {3});", GetFunctionName(), input1Value, input2Value, outputValue);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string vectorString = "Vector";
            int inputChannelCount = (int)SlotValueHelper.GetChannelCount(FindSlot<MaterialSlot>(InputSlot1Id).concreteValueType);
            if (inputChannelCount == 1)
            {
                int outputChannelCount = (int)SlotValueHelper.GetChannelCount(FindSlot<MaterialSlot>(OutputSlotId).concreteValueType);
                vectorString += ".";
                for(int i = 0; i < outputChannelCount; i++)
                    vectorString += "x";
            }

            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("Vector", "Matrix", "Out"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk(string.Format("Out = mul({0}, Matrix);", vectorString), false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
