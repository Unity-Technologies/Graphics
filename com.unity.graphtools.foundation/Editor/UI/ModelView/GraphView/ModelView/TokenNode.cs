using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// UI for <see cref="ISingleInputPortNodeModel"/> and <see cref="ISingleOutputPortNodeModel"/>.
    /// </summary>
    public class TokenNode : Node
    {
        public static readonly string tokenModifierUssClassName = ussClassName.WithUssModifier("token");
        public static readonly string constantModifierUssClassName = ussClassName.WithUssModifier("constant-token");
        public static readonly string variableModifierUssClassName = ussClassName.WithUssModifier("variable-token");
        public static readonly string portalModifierUssClassName = ussClassName.WithUssModifier("portal");
        public static readonly string portalEntryModifierUssClassName = ussClassName.WithUssModifier("portal-entry");
        public static readonly string portalExitModifierUssClassName = ussClassName.WithUssModifier("portal-exit");
        public static readonly string highlightedModifierUssClassName = ussClassName.WithUssModifier("highlighted");

        public static readonly string titleIconContainerPartName = "title-icon-container";
        public static readonly string constantEditorPartName = "constant-editor";
        public static readonly string inputPortContainerPartName = "inputs";
        public static readonly string outputPortContainerPartName = "outputs";

        internal bool IsHighlighted() => ClassListContains(highlightedModifierUssClassName);

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(SinglePortContainerPart.Create(inputPortContainerPartName, ExtractInputPortModel(Model), this, ussClassName));
            PartList.AppendPart(IconTitleProgressPart.Create(titleIconContainerPartName, Model, this, ussClassName));
            PartList.AppendPart(ConstantNodeEditorPart.Create(constantEditorPartName, Model, this, ussClassName));
            PartList.AppendPart(SinglePortContainerPart.Create(outputPortContainerPartName, ExtractOutputPortModel(Model), this, ussClassName));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(tokenModifierUssClassName);
            this.AddStylesheet("TokenNode.uss");

            switch (Model)
            {
                case IEdgePortalEntryModel _:
                    AddToClassList(portalModifierUssClassName);
                    AddToClassList(portalEntryModifierUssClassName);
                    break;
                case IEdgePortalExitModel _:
                    AddToClassList(portalModifierUssClassName);
                    AddToClassList(portalExitModifierUssClassName);
                    break;
                case IConstantNodeModel _:
                    AddToClassList(constantModifierUssClassName);
                    break;
                case IVariableNodeModel _:
                    AddToClassList(variableModifierUssClassName);
                    break;
            }
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            if (GraphElementModel.GraphModel != null && GraphView.GraphViewModel?.SelectionState != null)
            {
                var declarationModel = NodeModel is IHasDeclarationModel hasDeclarationModel ? hasDeclarationModel.DeclarationModel : null;
                var highlight = GraphView.GraphTool.HighlighterState.GetDeclarationModelHighlighted(declarationModel);

                // Do not show highlight if selected.
                if (highlight && GraphView.GraphViewModel.SelectionState.IsSelected(NodeModel))
                {
                    highlight = false;
                }

                EnableInClassList(highlightedModifierUssClassName, highlight);
            }
        }

        protected static IModel ExtractInputPortModel(IModel model)
        {
            if (model is ISingleInputPortNodeModel inputPortHolder && inputPortHolder.InputPort != null)
            {
                Debug.Assert(inputPortHolder.InputPort.Direction == PortDirection.Input);
                return inputPortHolder.InputPort;
            }

            return null;
        }

        protected static IModel ExtractOutputPortModel(IModel model)
        {
            if (model is ISingleOutputPortNodeModel outputPortHolder && outputPortHolder.OutputPort != null)
            {
                Debug.Assert(outputPortHolder.OutputPort.Direction == PortDirection.Output);
                return outputPortHolder.OutputPort;
            }

            return null;
        }
    }
}
