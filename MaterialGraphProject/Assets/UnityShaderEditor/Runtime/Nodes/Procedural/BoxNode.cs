namespace UnityEngine.MaterialGraph
{
    [Title("Procedural/Box")]
    public class BoxNode : Function3Input, IGeneratesFunction
    {
        public BoxNode()
        {
            name = "Box";
        }

        protected override string GetFunctionName()
        {
            return "unity_boxnode_" + precision;
        }

        protected override string GetInputSlot1Name()
        {
            return "X and Y";
        }

        protected override string GetInputSlot2Name()
        {
            return "X Min and Max";
        }

        protected override string GetInputSlot3Name()
        {
            return "Y Min and Max";
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector2, Vector3.zero);
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector2, Vector3.zero);
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
            outputString.AddShaderChunk(GetFunctionPrototype("xy", "xMinAndMax", "yMinAndMax"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("float x = step( xMinAndMax.x, xy.x ) - step( xMinAndMax.y, xy.x );", false);
            outputString.AddShaderChunk("float y = step( yMinAndMax.x, xy.y ) - step( yMinAndMax.y, xy.y );", false);
            outputString.AddShaderChunk("return x * y;", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
