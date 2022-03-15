using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for model UI in the blackboard.
    /// </summary>
    public abstract class BlackboardElement : ModelView
    {
        public static readonly string ussClassName = "ge-blackboard-element";
        public static readonly string selectableModifierUssClassName = ussClassName.WithUssModifier("selectable");

        ClickSelector m_ClickSelector;

        /// <summary>
        /// The view holding this element.
        /// </summary>
        public BlackboardView BlackboardView => RootView as BlackboardView;

        /// <summary>
        /// The model that backs the UI.
        /// </summary>
        public IGraphElementModel GraphElementModel => Model as IGraphElementModel;

        /// <summary>
        /// The click selector for this element.
        /// </summary>
        protected ClickSelector ClickSelector
        {
            get => m_ClickSelector;
            set => this.ReplaceManipulator(ref m_ClickSelector, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardElement"/> class.
        /// </summary>
        public BlackboardElement()
        {
            RegisterCallback<KeyDownEvent>(OnRenameKeyDown);
            focusable = true;

            ContextualMenuManipulator = new BlackboardContextualMenuManipulator(BuildContextualMenu);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            AddToClassList(ussClassName);

            base.PostBuildUI();
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            if (GraphElementModel?.IsSelectable() ?? false)
                ClickSelector ??= CreateClickSelector();
            else
                ClickSelector = null;

            EnableInClassList(selectableModifierUssClassName, ClickSelector != null);

            this.SetCheckedPseudoState(IsSelected());
        }

        /// <summary>
        /// Creates a <see cref="ClickSelector" /> for this element.
        /// </summary>
        /// <returns>A <see cref="ClickSelector" /> for this element.</returns>
        protected virtual ClickSelector CreateClickSelector()
        {
            return new BlackboardClickSelector();
        }

        /// <summary>
        /// Checks if the underlying graph element model is selected.
        /// </summary>
        /// <returns>True if the model is selected, false otherwise.</returns>
        public bool IsSelected()
        {
            return BlackboardView?.BlackboardViewModel.SelectionState?.IsSelected(GraphElementModel) ?? false;
        }

        /// <summary>
        /// Callback for the KeyDownEvent to handle renames.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnRenameKeyDown(KeyDownEvent e)
        {
            if (IsRenameKey(e) && GraphElementModel.IsRenamable() && IsSelected())
            {
                if (Rename())
                {
                    e.StopPropagation();
                }
            }
        }
    }
}
