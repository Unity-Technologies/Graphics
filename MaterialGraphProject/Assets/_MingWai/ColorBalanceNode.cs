namespace UnityEngine.MaterialGraph
{
    [Title("ColorBalance")]
    public class ColorBalanceNode : FunctionNInNOut, IGeneratesFunction
    {
        public ColorBalanceNode()
        {
            name = "ColorBalance";
            AddSlot("Color", "inputColor", Graphing.SlotType.Input, SlotValueType.Vector4, Vector4.one);
            AddSlot("AdjustRGB", "adjustRGB", Graphing.SlotType.Input, SlotValueType.Vector3, Vector3.zero);
            AddSlot("RGBA", "finalColor", Graphing.SlotType.Output, SlotValueType.Vector4, Vector4.zero);
            UpdateNodeAfterDeserialization();
        }

        protected override string GetFunctionName()
        {
            return "unity_colorbalance_" + precision;
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype(), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("float red = 0;", false);
            outputString.AddShaderChunk("float green = 0;", false);
            outputString.AddShaderChunk("float blue = 0;", false);

            outputString.AddShaderChunk("red = 1.00f / (1-adjustRGB.r) * inputColor.r;", false);
            outputString.AddShaderChunk("green = 1.00f / (1-adjustRGB.g) * inputColor.g;", false);
            outputString.AddShaderChunk("blue = 1.00f / (1-adjustRGB.b) * inputColor.b;", false);

            outputString.AddShaderChunk("red = clamp(red,0.00f,1.00f);", false);
            outputString.AddShaderChunk("green = clamp(green,0.00f,1.00f);", false);
            outputString.AddShaderChunk("blue = clamp(blue,0.00f,1.00f);", false);

            outputString.AddShaderChunk("finalColor.r = red;", false);
            outputString.AddShaderChunk("finalColor.g = green;", false);
            outputString.AddShaderChunk("finalColor.b = blue;", false);
            outputString.AddShaderChunk("finalColor.a = inputColor.a;", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
