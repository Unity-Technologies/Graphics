using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/World Pos Node")]
    public class WorldPosNode : AbstractMaterialNode, IGeneratesVertexToFragmentBlock
    {
        private const string kOutputSlotName = "WorldPos";

        public override bool hasPreview { get { return true; } }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public WorldPosNode()
        {
            name = "WorldPos";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotName, kOutputSlotName, SlotType.Output, 0, SlotValueType.Vector3, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotName });
        }
       
        public override string GetOutputVariableNameForSlot(MaterialSlot slot)
        {
            return "IN.worldPos";
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderChunk(precision + "3 worldPos;", true);
        }
    }
}
