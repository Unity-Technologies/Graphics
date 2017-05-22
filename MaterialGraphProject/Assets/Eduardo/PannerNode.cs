namespace UnityEngine.MaterialGraph
{
    [Title("UV/Panner")]
    public class PannerNode : Function3Input, IGeneratesFunction
    {
        public PannerNode()
        {
            name = "Panner";
        }

        protected override string GetFunctionName()
        {
            return "unity_pan_" + precision;
        }

        protected override string GetInputSlot1Name()
        {
            return "UV";
        }

        protected override string GetInputSlot2Name()
        {
            return "HorizontalOffset";
        }

        protected override string GetInputSlot3Name()
        {
            return "VerticalOffset";
        }
        
        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector2, Vector4.zero);
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector1, Vector4.zero);
        }

        protected override MaterialSlot GetInputSlot3()
        {
            return new MaterialSlot(InputSlot3Id, GetInputSlot3Name(), kInputSlot3ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector1, Vector4.zero);
        }
        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, UnityEngine.Graphing.SlotType.Output, SlotValueType.Vector2, Vector2.zero);
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("UV", "HorizontalOffset", "VerticalOffset"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return float2(UV.x + HorizontalOffset, UV.y + VerticalOffset);", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
