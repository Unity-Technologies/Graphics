using System;
using System.Linq;
using System.Reflection;
using UnityEditor.Rendering;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Utility", "Logic", "Branch On Input Connection")]
    class BranchOnInputConnectionNode : CodeFunctionNode
    {
        public BranchOnInputConnectionNode()
        {
            name = "Branch On Input Connection";
        }

        public override bool allowedInMainGraph { get => false; }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_BranchOnInputConnection", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_BranchOnInputConnection(
            [Slot(0, Binding.None)] PropertyConnectionState Input,
            [Slot(1, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector Connected,
            [Slot(2, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector NotConnected,
            [Slot(3, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = Input ? Connected : NotConnected;
}
";
        }

        public override void ValidateNode()
        {
            base.ValidateNode();
            var slot = FindInputSlot<MaterialSlot>(0);
            if (slot.isConnected)
            {
                var property = GetSlotProperty(0);
                if (property == null || !property.isConnectionTestable)
                {
                    var edges = owner.GetEdges(GetSlotReference(0));
                    owner.RemoveElements(new AbstractMaterialNode[] {}, edges.ToArray(), new GroupData[] {}, new StickyNoteData[] {});
                    if (property != null)
                        owner.AddValidationError(objectId, String.Format("Connected property {0} is not connection testable and was disconnected from the Input port", property.displayName), ShaderCompilerMessageSeverity.Warning);
                }
            }
        }
    }
}
