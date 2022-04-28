using System.Linq;
using UnityEngine;
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
        public static readonly string nodeSettingsContainerPartName = "node-settings";

        /// <summary>
        /// The name of the port container part.
        /// </summary>
        public static readonly string portContainerPartName = "port-container";

        protected VisualElement m_ContentContainer;

        public INodeModel NodeModel => Model as INodeModel;

        /// <inheritdoc />
        public override VisualElement contentContainer => m_ContentContainer ?? this;

        /// The <see cref="DynamicBorder"/> used to display selection, hover and highlight.
        protected DynamicBorder Border { get; private set; }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(EditableTitlePart.Create(titleContainerPartName, Model, this, ussClassName));
            PartList.AppendPart(SerializedFieldsInspector.Create(nodeSettingsContainerPartName, Model, this, ussClassName, ModelInspectorView.BasicSettingsFilter));
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

            Border = CreateDynamicBorder();
            Border.AddToClassList(ussClassName.WithUssElement("dynamic-border"));
            hierarchy.Add(Border);
        }

        /// <summary>
        /// Creates a <see cref="DynamicBorder"/> for this node.
        /// </summary>
        /// <returns>A <see cref="DynamicBorder"/> for this node.</returns>
        protected virtual DynamicBorder CreateDynamicBorder()
        {
            return new DynamicBorder(this);
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

            SetPosition(NodeModel.Position);

            EnableInClassList(emptyModifierUssClassName, childCount == 0);
            EnableInClassList(disabledModifierUssClassName, NodeModel.State == ModelState.Disabled);

            if (NodeModel is IPortNodeModel portHolder && portHolder.Ports != null)
            {
                bool noPortConnected = portHolder.Ports.All(port => !port.IsConnected());
                EnableInClassList(notConnectedModifierUssClassName, noPortConnected);
            }

            if (Model is IVariableNodeModel variableModel)
            {
                EnableInClassList(readOnlyModifierUssClassName, variableModel.VariableDeclarationModel?.Modifiers == ModifierFlags.Read);
                EnableInClassList(writeOnlyModifierUssClassName, variableModel.VariableDeclarationModel?.Modifiers == ModifierFlags.Write);
            }

            tooltip = NodeModel.Tooltip;

            UpdateColorFromModel();
            Border.Selected = IsSelected();
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

        public override void ActivateRename()
        {
            if (!((PartList.GetPart(titleContainerPartName) as EditableTitlePart)?.TitleLabel is EditableLabel label))
                return;

            label.BeginEditing();
        }

        /// <inheritdoc />
        public override void SetElementLevelOfDetail(float zoom)
        {
            base.SetElementLevelOfDetail(zoom);

            Border.Zoom = zoom;
        }
    }
}
