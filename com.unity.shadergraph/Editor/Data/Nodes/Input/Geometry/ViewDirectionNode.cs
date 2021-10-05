using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEngine.MaterialGraph.ViewDirectionNode")]
    [Title("Input", "Geometry", "View Direction")]
    class ViewDirectionNode : GeometryNode, IMayRequireViewDirection, IHasCustomDeprecationMessage
    {
        private const int kOutputSlotId = 0;
        public const string kOutputSlotName = "Out";

        public override int latestVersion => 1;
        public override IEnumerable<int> allowedNodeVersions => new int[] { 1 };

        public ViewDirectionNode()
        {
            name = "View Direction";
            synonyms = new string[] { "eye direction" };
            UpdateNodeAfterDeserialization();
            onAfterVersionChange += () => { if (sgVersion > 0) owner.ClearErrorsForNode(this); };
        }

        public override void ValidateNode()
        {
            base.ValidateNode();
            if (sgVersion == 0)
            {
                owner.AddValidationError(objectId, "Node behavior was changed. See inspector for details", Rendering.ShaderCompilerMessageSeverity.Warning);
            }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(
                kOutputSlotId,
                kOutputSlotName,
                kOutputSlotName,
                SlotType.Output,
                Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return string.Format("IN.{0}", space.ToVariableName(InterpolatorType.ViewDirection));
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability)
        {
            return space.ToNeededCoordinateSpace();
        }

        public void GetCustomDeprecationMessage(out string deprecationString, out string buttonText, out string labelText, out MessageType messageType)
        {
            deprecationString = null;
            buttonText = null;
            labelText = null;
            messageType = MessageType.Warning;
            if (sgVersion == 0)
            {
                deprecationString = "The View Direction node has changed behavior in 2021.2. Please see documentation for more info.";
                buttonText = "Dismiss";
                labelText = "UPDATED: Hover for info";
                messageType = MessageType.Info;
            }
        }

        public string GetCustomDeprecationLabel()
        {
            return name;
        }
    }
}
