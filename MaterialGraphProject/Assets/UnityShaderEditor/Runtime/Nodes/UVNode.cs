
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/UV Node")]
    public class UVNode : AbstractMaterialNode, IGeneratesVertexToFragmentBlock, IGeneratesVertexShaderBlock, IGeneratesBodyCode
    {
        private const string kOutputSlotName = "UV";

        public override bool hasPreview { get { return true; } }

        public UVNode()
        {
            name = "UV";
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotName, kOutputSlotName, SlotType.Output, 0, SlotValueType.Vector4, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] {kOutputSlotName});
        }
        
        public static void StaticGenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string temp = "half4 meshUV0";
            if (generationMode == GenerationMode.Preview2D)
                temp += " : TEXCOORD0";
            temp += ";";
            visitor.AddShaderChunk(temp, true);
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            StaticGenerateVertexToFragmentBlock(visitor, generationMode);
        }

        public static void GenerateVertexShaderBlock(ShaderGenerator visitor)
        {
            visitor.AddShaderChunk("o.meshUV0 = v.texcoord;", true);
        }

        public void GenerateVertexShaderBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            GenerateVertexShaderBlock(visitor);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputSlot = FindOutputSlot<MaterialSlot>(kOutputSlotName);

            string uvValue = "IN.meshUV0";
            visitor.AddShaderChunk(precision + "4 " + GetOutputVariableNameForSlot(outputSlot) + " = " + uvValue + ";", true);
        }
    }
}
