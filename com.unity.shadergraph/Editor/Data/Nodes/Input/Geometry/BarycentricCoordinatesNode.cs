using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Geometry", "Barycentric Coordinates")]
    class BarycentricCoordinatesNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireBarycentricCoordinates
    {
        public BarycentricCoordinatesNode()
        {
            name = "Barycentric Coordinates";
            UpdateNodeAfterDeserialization();
        }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            //sb.AppendLine(string.Format("$precision4 {0} = {1};", GetVariableNameForSlot(kOutputSlotId), string.Format("IN.{0}", ShaderGeneratorNames.BarycentricCoordinates)));
            sb.AppendLine(string.Format("$precision4 {0} = {1};", GetVariableNameForSlot(kOutputSlotId), "float4(1, 0, 0, 1)"));
        
    }

        public bool RequiresBarycentricCoordinates(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
