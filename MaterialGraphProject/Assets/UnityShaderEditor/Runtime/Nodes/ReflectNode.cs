using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Reflect Node")]
    class ReflectNode : Function2Input, IGeneratesFunction
    {
        public ReflectNode()
        {
            name = "ReflectNode";
        }

        protected override string GetInputSlot1Name() {return "Normal"; }
        protected override string GetInputSlot2Name() {return "Direction"; }
        protected override string GetOutputSlotName() {return "Reflection"; }

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
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector3, Vector4.zero);
        }

        protected override string GetFunctionName() {return "unity_reflect_" + precision; }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("normal", "direction"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk(precision + "3 vn = normalize(normal);", false);
            outputString.AddShaderChunk(precision + "3 vd = normalize(direction);", false);
            outputString.AddShaderChunk("return 2 * dot(vn, vd) * vn - vd, 1.0;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
