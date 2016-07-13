using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Screen Pos Node")]
    public class ScreenPosNode : AbstractMaterialNode, IGeneratesVertexToFragmentBlock
    {
        public ScreenPosNode()
        {
            name = "ScreenPos";
            UpdateNodeAfterDeserialization();
        }

        private const string kOutputSlotName = "ScreenPos";

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotName, kOutputSlotName, SlotType.Output, 0, SlotValueType.Vector4, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotName });
        }
        
        public override string GetVariableNameForSlot(MaterialSlot slot)
        {
            return "IN.screenPos";
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string temp = precision + "4 screenPos";
            if (generationMode == GenerationMode.Preview2D)
                temp += " : TEXCOORD1";
            temp += ";";
            visitor.AddShaderChunk(temp, true);
        }
    }
}
