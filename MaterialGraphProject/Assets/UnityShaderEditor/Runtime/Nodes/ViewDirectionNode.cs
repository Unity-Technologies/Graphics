using System.ComponentModel;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/View Direction Node")]
    public class ViewDirectionNode : AbstractMaterialNode, IGeneratesVertexToFragmentBlock
    {
        private const int kOutputSlotId = 0;
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
            AddSlot(new MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector3, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return "IN.viewDir";
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.Preview2D)
                throw new InvalidEnumArgumentException(string.Format("Trying to generate 2D preview on {0}. This is not supported!", this));

            visitor.AddShaderChunk("float3 viewDir;", true);
        }
    }
}
