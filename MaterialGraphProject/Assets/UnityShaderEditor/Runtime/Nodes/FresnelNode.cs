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
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, SlotType.Input, SlotValueType.Vector3, Vector4.zero);
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, SlotType.Input, SlotValueType.Vector3, Vector4.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector1, Vector4.zero);
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

            visitor.AddShaderChunk(outputString.GetShaderString(0), true, false);
        }
    }
}
