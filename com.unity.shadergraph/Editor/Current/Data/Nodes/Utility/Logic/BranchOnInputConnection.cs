using System;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Rendering;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Utility", "Logic", "Branch On Input Connection")]
    class BranchOnInputConnectionNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public const int InputSlotId = 0;
        public const int ConnectedSlotId = 1;
        public const int NotConnectedSlotId = 2;
        public const int OutSlotId = 3;

        const string kInputSlotName = "Input";
        const string kConnectedSlotName = "Connected";
        const string kNotConnectedSlotName = "NotConnected";
        const string kOutSlotName = "Out";

        public BranchOnInputConnectionNode()
        {
            name = "Branch On Input Connection";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new PropertyConnectionStateMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, Graphing.SlotType.Input));
            AddSlot(new DynamicVectorMaterialSlot(ConnectedSlotId, kConnectedSlotName, kConnectedSlotName, Graphing.SlotType.Input, UnityEngine.Vector4.one));
            AddSlot(new DynamicVectorMaterialSlot(NotConnectedSlotId, kNotConnectedSlotName, kNotConnectedSlotName, Graphing.SlotType.Input, UnityEngine.Vector4.zero));
            AddSlot(new DynamicVectorMaterialSlot(OutSlotId, kOutSlotName, kOutSlotName, Graphing.SlotType.Output, UnityEngine.Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, ConnectedSlotId, NotConnectedSlotId, OutSlotId });
        }

        public override bool allowedInMainGraph => false;
        public override bool hasPreview => true;

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            // inspect one of the dynamic slots to figure out what type we are actually using
            var dynSlot = this.FindInputSlot<DynamicVectorMaterialSlot>(ConnectedSlotId);
            string dynamicDimension = NodeUtils.GetSlotDimension(dynSlot.concreteValueType);
            var dynamicType = "$precision" + dynamicDimension;

            // declare output variable
            var input = GetSlotValue(InputSlotId, generationMode);
            var connected = GetSlotValue(ConnectedSlotId, generationMode);
            var notconnected = GetSlotValue(NotConnectedSlotId, generationMode);
            var output = GetVariableNameForSlot(OutSlotId);

            if (generationMode == GenerationMode.Preview)
                sb.AppendLine($"{dynamicType} {output} = {notconnected};");
            else
                sb.AppendLine($"{dynamicType} {output} = {input} ? {connected} : {notconnected};");
        }
    }
}
