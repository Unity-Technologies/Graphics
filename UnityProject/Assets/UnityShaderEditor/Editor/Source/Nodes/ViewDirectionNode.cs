using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Input/View Direction Node")]
    public class ViewDirectionNode : BaseMaterialNode, IGeneratesVertexToFragmentBlock
    {
        private const string kOutputSlotName = "ViewDirection";

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public override void OnCreate()
        {
            base.OnCreate();
            name = "View Direction";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotName), null));
        }

        public override string GetOutputVariableNameForSlot(Slot slot, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.Preview2D)
                Debug.LogError("Trying to generate 2D preview on a node that does not support it!");

            return "float4 (IN.viewDir, 1)";
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.Preview2D)
                Debug.LogError("Trying to generate 2D preview on a node that does not support it!");

            visitor.AddShaderChunk("float3 viewDir;", true);
        }
    }
}
