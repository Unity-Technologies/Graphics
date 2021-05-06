using System;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Rendering;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Utility", "Logic", "Branch On Input Connection")]
    class BranchOnInputConnectionNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
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
            var functionName = GetFunctionName(out string dynamicType);

            // declare output variables
            sb.AppendLine(dynamicType + " " + GetVariableNameForSlot(OutSlotId) + ";");

            // call function
            sb.AppendIndentation();
            sb.Append(functionName);
            sb.Append("(");
            sb.Append(GetSlotValue(InputSlotId, generationMode));
            sb.Append(",");
            sb.Append(GetSlotValue(ConnectedSlotId, generationMode));
            sb.Append(",");
            sb.Append(GetSlotValue(NotConnectedSlotId, generationMode));
            sb.Append(",");
            sb.Append(GetVariableNameForSlot(OutSlotId));
            sb.Append(");");
            sb.AppendNewLine();
        }

        string GetFunctionName(out string dynamicType)
        {
            // inspect one of the dynamic slots to figure out what type we are actually using
            var dynSlot = this.FindInputSlot<DynamicVectorMaterialSlot>(ConnectedSlotId);
            string dynamicDimension = NodeUtils.GetSlotDimension(dynSlot.concreteValueType);
            dynamicType = "$precision" + dynamicDimension;

            return $"Unity_BranchOnInputConnection_{dynamicType}";
        }

        public virtual void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            var functionName = GetFunctionName(out string dynamicType);

            // build the function
            registry.ProvideFunction(functionName, sb =>
            {
                sb.AppendLine($"void {functionName}(bool Input, {dynamicType} Connected, {dynamicType} NotConnected, out {dynamicType} Out)");
                using (sb.BlockScope())
                {
                    if (generationMode == GenerationMode.Preview)
                        sb.AppendLine("Out = NotConnected;");
                    else
                        sb.AppendLine("Out = Input ? Connected : NotConnected;");
                }
            });
        }

        public override void ValidateNode()
        {
            base.ValidateNode();
            var slot = FindInputSlot<MaterialSlot>(InputSlotId);
            if (slot.isConnected)
            {
                var property = GetSlotProperty(InputSlotId);
                if (property == null || !property.isConnectionTestable)
                {
                    var edges = owner.GetEdges(GetSlotReference(InputSlotId));
                    owner.RemoveElements(new AbstractMaterialNode[] { }, edges.ToArray(), new GroupData[] { }, new StickyNoteData[] { });
                    if (property != null)
                        owner.AddValidationError(objectId, String.Format("Connected property {0} is not connection testable and was disconnected from the Input port", property.displayName), ShaderCompilerMessageSeverity.Warning);
                }
            }
        }
    }
}
