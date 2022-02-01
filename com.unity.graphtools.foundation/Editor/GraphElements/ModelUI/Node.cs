using System.Linq;
using UnityEngine.UIElements;
// ReSharper disable InconsistentNaming

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// UI for a <see cref="INodeModel"/>.
    /// </summary>
    public class Node : GraphElement
    {
        public new static readonly string ussClassName = "ge-node";
        public static readonly string notConnectedModifierUssClassName = ussClassName.WithUssModifier("not-connected");
        public static readonly string emptyModifierUssClassName = ussClassName.WithUssModifier("empty");
        public static readonly string disabledModifierUssClassName = ussClassName.WithUssModifier("disabled");
        public static readonly string unusedModifierUssClassName = ussClassName.WithUssModifier("unused");
        public static readonly string readOnlyModifierUssClassName = ussClassName.WithUssModifier("read-only");
        public static readonly string writeOnlyModifierUssClassName = ussClassName.WithUssModifier("write-only");

        public static readonly string selectionBorderElementName = "selection-border";
        public static readonly string disabledOverlayElementName = "disabled-overlay";
        public static readonly string titleContainerPartName = "title-container";

        /// <summary>
        /// The name of the port container part.
        /// </summary>
        public static readonly string portContainerPartName = "port-container";

        protected VisualElement m_ContentContainer;

        public INodeModel NodeModel => Model as INodeModel;

        /// <inheritdoc />
        public override VisualElement contentContainer => m_ContentContainer ?? this;

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(EditableTitlePart.Create(titleContainerPartName, Model, this, ussClassName));
            PartList.AppendPart(PortContainerPart.Create(portContainerPartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            var selectionBorder = new SelectionBorder { name = selectionBorderElementName };
            selectionBorder.AddToClassList(ussClassName.WithUssElement(selectionBorderElementName));
            Add(selectionBorder);
            m_ContentContainer = selectionBorder.ContentContainer;

            base.BuildElementUI();

            var disabledOverlay = new VisualElement { name = disabledOverlayElementName, pickingMode = PickingMode.Ignore };
            hierarchy.Add(disabledOverlay);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            usageHints = UsageHints.DynamicTransform;
            AddToClassList(ussClassName);
            this.AddStylesheet("Node.uss");
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            var newPos = NodeModel.Position;
            style.left = newPos.x;
            style.top = newPos.y;

            EnableInClassList(emptyModifierUssClassName, childCount == 0);
            EnableInClassList(disabledModifierUssClassName, NodeModel.State == ModelState.Disabled);

            if (NodeModel is IPortNodeModel portHolder && portHolder.Ports != null)
            {
                bool noPortConnected = portHolder.Ports.All(port => !port.IsConnected());
                EnableInClassList(notConnectedModifierUssClassName, noPortConnected);
            }

            if (Model is IVariableNodeModel variableModel)
            {
                EnableInClassList(readOnlyModifierUssClassName, variableModel.VariableDeclarationModel?.Modifiers == ModifierFlags.ReadOnly);
                EnableInClassList(writeOnlyModifierUssClassName, variableModel.VariableDeclarationModel?.Modifiers == ModifierFlags.WriteOnly);
            }

            tooltip = NodeModel.Tooltip;

            UpdateColorFromModel();
        }

        /// <summary>
        /// Updates the Node color based its model custom color.
        /// </summary>
        protected virtual void UpdateColorFromModel()
        {
            if (NodeModel.HasUserColor)
            {
                var border = this.MandatoryQ(SelectionBorder.contentContainerElementName);
                border.style.backgroundColor = NodeModel.Color;
                border.style.backgroundImage = null;
            }
            else
            {
                var border = this.MandatoryQ(SelectionBorder.contentContainerElementName);
                border.style.backgroundColor = StyleKeyword.Null;
                border.style.backgroundImage = StyleKeyword.Null;
            }
        }

        public virtual void EditTitle()
        {
            if (!((PartList.GetPart(titleContainerPartName) as EditableTitlePart)?.TitleLabel is EditableLabel label))
                return;

            label.BeginEditing();
        }
    }
}
