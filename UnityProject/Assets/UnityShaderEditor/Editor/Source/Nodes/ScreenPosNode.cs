namespace UnityEditor.Graphs.Material
{
    [Title("Input/Screen Pos Node")]
    public class ScreenPosNode : BaseMaterialNode, IGeneratesVertexToFragmentBlock
    {
        private const string kOutputSlotName = "ScreenPos";

        public override bool hasPreview { get { return true; } }

        public override void Init()
        {
            name = "ScreenPos";
            base.Init();
            AddSlot(new Slot(SlotType.OutputSlot, kOutputSlotName));
        }

        public override string GetOutputVariableNameForSlot(Slot slot, GenerationMode generationMode)
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
