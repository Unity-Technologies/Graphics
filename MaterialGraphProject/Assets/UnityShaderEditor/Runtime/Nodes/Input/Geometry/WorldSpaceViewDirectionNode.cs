using System.ComponentModel;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    interface IMayRequireViewDirection
    {
        bool RequiresViewDirection();
    }

    [Title("Input/Geometry/World Space View Direction")]
    public class WorldSpaceViewDirectionNode : AbstractMaterialNode, IMayRequireViewDirection
    {
        private const int kOutputSlotId = 0;

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public WorldSpaceViewDirectionNode()
        {
            name = "WorldSpaceViewDirection";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(
                kOutputSlotId,
                ShaderGeneratorNames.WorldSpaceViewDirection, 
                ShaderGeneratorNames.WorldSpaceViewDirection, 
                SlotType.Output, 
                SlotValueType.Vector3, 
                Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return ShaderGeneratorNames.WorldSpaceViewDirection;
        }

        public bool RequiresViewDirection()
        {
            return true;
        }
    }
}
