using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/View Direction Node")]
    public class ViewDirectionNode : AbstractMaterialNode, IGeneratesVertexToFragmentBlock
    {
        private const string kOutputSlotName = "ViewDirection";

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public ViewDirectionNode()
        {
            name = "View Direction";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotName, kOutputSlotName, SlotType.Output, 0, SlotValueType.Vector3, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotName });
        }

        public override string GetVariableNameForSlot(MaterialSlot slot)
        {
            return "IN.viewDir";
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.Preview2D)
                Debug.LogError("Trying to generate 2D preview on a node that does not support it!");

            visitor.AddShaderChunk("float3 viewDir;", true);
        }
    }
}
