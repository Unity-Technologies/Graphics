namespace UnityEngine.MaterialGraph
{
    [Title("UV/Spherize")]
    public class SpherizeNode : Function3Input, IGeneratesFunction
    {
        public SpherizeNode()
        {
            name = "Spherize";
        }

        protected override string GetFunctionName()
        {
            return "unity_spherize_" + precision;
        }

        protected override string GetInputSlot1Name()
        {
            return "UV";
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector2, Vector2.zero);
        }

        protected override string GetInputSlot2Name()
        {
            return "Position";
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector2, Vector2.zero);
        }

        protected override string GetInputSlot3Name()
        {
            return "RadiusAndStrength";
        }

        protected override MaterialSlot GetInputSlot3()
        {
            return new MaterialSlot(InputSlot3Id, GetInputSlot3Name(), kInputSlot3ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector2, Vector3.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, UnityEngine.Graphing.SlotType.Output, SlotValueType.Vector2, Vector2.zero);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("uv", "position", "radiusAndStrength"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            //vec2 fromUVToPoint = pos - uv;
            //dist = length(fromUVToPoint);

            //float mag = (1.0 - (dist / radius)) * strength;
            //mag *= step(dist, radius);

            //return uv + (mag * fromUVToPoint);

            outputString.AddShaderChunk("float2 fromUVToPoint = position - uv;", false);
            outputString.AddShaderChunk("float dist = length(fromUVToPoint);", false);
            outputString.AddShaderChunk("float mag = ((1.0 - (dist / radiusAndStrength.x)) * radiusAndStrength.y) * step(dist, radiusAndStrength.x);", false);
            outputString.AddShaderChunk("return uv + (mag * fromUVToPoint);", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
