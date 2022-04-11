using System.Reflection;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Error")]
    class ErrorNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public enum ErrorType
        {
            NodeError,
            NodeWarning,
            Syntax
        };

        const int OutputSlotId = 0;
        const string OutputSlotName = "Output";

        [SerializeField]
        public ErrorType m_ErrorType = ErrorType.NodeError;

        [EnumControl]
        public ErrorType errorType
        {
            get => m_ErrorType;
            set
            {
                m_ErrorType = value;
                (owner as GraphData).ClearErrorsForNode(this);
                Dirty(ModificationScope.Topological);
            }
        }

        public ErrorNode()
        {
            name = "Error";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview => false;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, OutputSlotName, OutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, });
        }

        public override void ValidateNode()
        {
            base.ValidateNode();
            if (errorType == ErrorType.NodeError)
                ((GraphData)owner).AddValidationError(objectId, "Error from Error Node (" + this.objectId + ")");
            else if (errorType == ErrorType.NodeWarning)
                ((GraphData)owner).AddValidationError(objectId, "Warning from Error Node (" + this.objectId + ")", Rendering.ShaderCompilerMessageSeverity.Warning);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (errorType == ErrorType.Syntax)
                sb.AppendLine("#error Syntax error from Error Node (" + this.objectId + ")");

            sb.AppendLine($"$precision4 {GetVariableNameForSlot(OutputSlotId)} = 0.0f;");
        }
    }
}
