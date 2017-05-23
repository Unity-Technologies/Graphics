using UnityEngine.Graphing;
using System.Linq;
using System.Collections;

namespace UnityEngine.MaterialGraph
{
    [Title("Procedural/Fractal")]
    public class FractalNode : FunctionNInNOut, IGeneratesFunction
    {

        public FractalNode()
        {
            name = "Fractal";
            AddSlot("UV", "texCoord", Graphing.SlotType.Input, SlotValueType.Vector2);
            AddSlot("Pan", "Pan", Graphing.SlotType.Input, SlotValueType.Vector2);
            AddSlot("Zoom", "Zoom", Graphing.SlotType.Input, SlotValueType.Vector1);
            AddSlot("Aspect", "Aspect", Graphing.SlotType.Input, SlotValueType.Vector1);

            AddSlot("FracResult", "fractalRes", Graphing.SlotType.Output, SlotValueType.Dynamic);

            UpdateNodeAfterDeserialization();
        }

        protected override string GetFunctionName()
        {
            return "unity_Fractal";
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
            outputString.AddShaderChunk("const int Iterations = 128;", false);
            outputString.Indent();
            outputString.AddShaderChunk("float2 c = (texCoord - 0.5) * Zoom * float2(1, Aspect) - Pan;", false);
            outputString.AddShaderChunk("float2 v = 0;", false);
            outputString.AddShaderChunk("for (int n = 0; n < Iterations && dot(v,v) < 4; n++)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("v = float2(v.x * v.x - v.y * v.y, v.x * v.y * 2) + c;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);
            outputString.Deindent();
            outputString.AddShaderChunk("fractalRes = (dot(v, v) > 4) ? (float)n / (float)Iterations : 0;", false);
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
