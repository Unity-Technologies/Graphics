using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("UV/Cartesian To Polar")]
    public class CartesianToPolarNode : Function1Input, IGeneratesFunction
    {
        public CartesianToPolarNode()
        {
            name = "CartesianToPolar";
        }

        protected override string GetFunctionName()
        {
            return "unity_cartesiantopolar_" + precision;
        }

        protected override string GetInputSlotName()
        {
            return "UV";
        }

        protected override MaterialSlot GetInputSlot()
        {
            return new MaterialSlot(InputSlotId, GetInputSlotName(), kInputSlotShaderName, SlotType.Input, SlotValueType.Vector2, Vector2.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector2, Vector2.zero);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("uv"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("float radius = length(uv);", false);
            outputString.AddShaderChunk("float angle = atan2(uv.x, uv.y);", false);
            outputString.AddShaderChunk("return float2(radius, angle);", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
