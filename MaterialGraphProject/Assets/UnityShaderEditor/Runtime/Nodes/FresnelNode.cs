using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Fresnel Node")]
    class FresnelNode : Function2Input, IGeneratesFunction
    {
        public FresnelNode()
        {
            name = "FresnelNode";
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(GetInputSlot1Name(), GetInputSlot1Name(), SlotType.Input, 0, SlotValueType.Vector3, Vector4.zero);
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(GetInputSlot2Name(), GetInputSlot2Name(), SlotType.Input, 1, SlotValueType.Vector3, Vector4.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(GetOutputSlotName(), GetOutputSlotName(), SlotType.Output, 0, SlotValueType.Vector1, Vector4.zero);
        }

        protected override string GetFunctionName() { return "unity_fresnel_" + precision; }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return (1.0 - dot (normalize (arg1), normalize (arg2)));", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
