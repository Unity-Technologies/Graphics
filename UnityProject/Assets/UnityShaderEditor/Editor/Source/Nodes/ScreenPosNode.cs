using UnityEditor.Graphs;

namespace UnityEditor.MaterialGraph
{
    [Title("Input/Screen Pos Node")]
    public class ScreenPosNode : BaseMaterialNode, IGeneratesVertexToFragmentBlock
    {
        private const string kOutputSlotName = "ScreenPos";

        public override bool hasPreview { get { return true; } }

        public override void OnCreate()
        {
            name = "ScreenPos";
            base.OnCreate();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.InputSlot, kOutputSlotName),  SlotValueType.Vector4));
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
