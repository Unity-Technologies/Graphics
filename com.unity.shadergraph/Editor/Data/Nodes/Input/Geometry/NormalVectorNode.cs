using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEngine.MaterialGraph.NormalNode")]
    [Title("Input", "Geometry", "Normal Vector")]
    class NormalVectorNode : GeometryNode, IMayRequireNormal
    {
        public const int kOutputSlotId = 0;
        public const string kOutputSlotName = "Out";

        public NormalVectorNode()
        {
            name = "Normal Vector";
            synonyms = new string[] { "surface direction" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, new Vector4(0, 0, 1)));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return string.Format("IN.{0}", space.ToVariableName(InterpolatorType.Normal));
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            return space.ToNeededCoordinateSpace();
        }
    }
}
