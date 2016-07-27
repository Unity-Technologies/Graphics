
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/UV Node")]
    public class UVNode : AbstractMaterialNode, IGeneratesVertexToFragmentBlock, IGeneratesVertexShaderBlock, IGeneratesBodyCode
    {
        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "UV";

        public override bool hasPreview { get { return true; } }

        public UVNode()
        {
            name = "UV";
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
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
            string uvValue = "IN.meshUV0";
            visitor.AddShaderChunk(precision + "4 " + GetVariableNameForSlot(OutputSlotId) + " = " + uvValue + ";", true);
        }
    }
}
