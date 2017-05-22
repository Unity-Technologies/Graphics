using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Color/Desaturate")]
    public class DesaturateNode : Function1Input, IGeneratesFunction
    {
        public DesaturateNode()
        {
            name = "Desaturate";
        }

        protected override string GetFunctionName()
        {
            return "unity_desaturate_" + precision;
        }

        protected override MaterialSlot GetInputSlot()
        {
            return new MaterialSlot(InputSlotId, GetInputSlotName(), kInputSlotShaderName, SlotType.Input, SlotValueType.Vector3, Vector4.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector3, Vector4.zero);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("arg1"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk(precision+" intensity = "+precision+" (arg1.r * 0.3 + arg1.g * 0.59 + arg1.b * 0.11);", false);
            outputString.AddShaderChunk("return " + precision + "3(intensity, intensity, intensity);", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}


