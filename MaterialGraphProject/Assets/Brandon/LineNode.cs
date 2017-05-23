namespace UnityEngine.MaterialGraph
{
    [Title("Procedural/Line Node")]
    public class LineNode : Function3Input, IGeneratesFunction
    {
        public LineNode()
        {
            name = "LineNode";
        }

        protected override string GetFunctionName()
        {
            return "unity_linenode_" + precision;
        }

        protected override string GetInputSlot1Name()
        {
            return "UV";
        }

        protected override string GetInputSlot2Name()
        {
            return "StartPoint";
        }

        protected override string GetInputSlot3Name()
        {
            return "EndPoint";
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector2, Vector2.zero);
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector2, Vector2.zero);
        }

        protected override MaterialSlot GetInputSlot3()
        {
            return new MaterialSlot(InputSlot3Id, GetInputSlot3Name(), kInputSlot3ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector2, Vector2.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, UnityEngine.Graphing.SlotType.Output, SlotValueType.Vector1, Vector2.zero);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("uv", "a", "b"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();


            //float line(vec2 a, vec2 b, vec2 p)
            //{
            //    vec2 aTob = b - a;
            //    vec2 aTop = p - a;

            //    float t = dot(aTop, aTob) / dot(aTob, aTob);

            //    t = clamp(t, 0.0, 1.0);

            //    float d = length(p - (a + aTob * t));
            //    d = 1.0 / d;

            //    return clamp(d, 0.0, 1.0);
            //}

            outputString.AddShaderChunk("float2 aTob = b - a;", false);
            outputString.AddShaderChunk("float2 aTop = uv - a;", false);
            outputString.AddShaderChunk("float t = dot(aTop, aTob) / dot(aTob, aTob);", false);
            outputString.AddShaderChunk("t = clamp(t, 0.0, 1.0);", false);
            outputString.AddShaderChunk("float d = 1.0 / length(uv - (a + aTob * t));", false);
            outputString.AddShaderChunk("return clamp(d, 0.0, 1.0);", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
