using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input/Time/Sine Time")]
    public class SinTimeNode : AbstractMaterialNode, IMayRequireTime
    {
        public SinTimeNode()
        {
            name = "SineTime";
            UpdateNodeAfterDeserialization();
        }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "SinTime";

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.one));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public override string GetVariableNameForSlot(int slotIds)
        {
            return "_SinTime";
        }

        public bool RequiresTime()
        {
            return true;
        }
    }
}
