using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base UI class for models displayed in a <see cref="GraphView"/>.
    /// </summary>
    public abstract class GraphElement : ModelUI
    {
        static readonly CustomStyleProperty<int> s_LayerProperty = new CustomStyleProperty<int>("--layer");
        static readonly Color k_MinimapColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

        public static readonly string ussClassName = "ge-graph-element";
        public static readonly string selectableModifierUssClassName = ussClassName.WithUssModifier("selectable");

        int m_Layer;

        bool m_LayerIsInline;

        ClickSelector m_ClickSelector;

        public GraphView GraphView => View as GraphView;

        public int Layer
        {
            get => m_Layer;
            set
            {
                m_LayerIsInline = true;
                m_Layer = value;
            }
        }

        public Color MinimapColor { get; protected set; }

        public virtual bool ShowInMiniMap { get; set; } = true;

        protected ClickSelector ClickSelector
        {
            get => m_ClickSelector;
            set => this.ReplaceManipulator(ref m_ClickSelector, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphElement"/> class.
        /// </summary>
        protected GraphElement()
        {
            MinimapColor = k_MinimapColor;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<KeyDownEvent>(OnRenameKeyDown);
            focusable = true;
        }

        public void ResetLayer()
        {
            int prevLayer = m_Layer;
            m_Layer = 0;
            m_LayerIsInline = false;
            customStyle.TryGetValue(s_LayerProperty, out m_Layer);
            UpdateLayer(prevLayer);
        }

        void UpdateLayer(int prevLayer)
        {
            if (prevLayer != m_Layer)
                GraphView?.ChangeLayer(this);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            AddToClassList(ussClassName);
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            if (Model?.IsSelectable() ?? false)
                ClickSelector ??= new ClickSelector();
            else
                ClickSelector = null;

            EnableInClassList(selectableModifierUssClassName, ClickSelector != null);

            this.SetCheckedPseudoState(IsSelected());
        }

        protected new void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            int prevLayer = m_Layer;
            if (!m_LayerIsInline)
                evt.customStyle.TryGetValue(s_LayerProperty, out m_Layer);

            UpdateLayer(prevLayer);
        }

        public virtual bool IsMovable()
        {
            return Model?.IsMovable() ?? false;
        }

        // PF: remove
        internal Rect GetPosition()
        {
            return layout;
        }

        public virtual void SetPosition(Rect newPos)
        {
            style.left = newPos.x;
            style.top = newPos.y;
        }

        /// <summary>
        /// Checks if the underlying graph element model is selected.
        /// </summary>
        /// <returns>True if the model is selected, false otherwise.</returns>
        public bool IsSelected()
        {
            return GraphView?.SelectionState?.IsSelected(Model) ?? false;
        }

        /// <summary>
        /// Displays the UI to rename the graph element.
        /// </summary>
        /// <returns>True if the UI could be displayed. False otherwise.</returns>
        public virtual bool Rename()
        {
            var editableLabel = this.SafeQ<EditableLabel>();

            // Execute after
            schedule.Execute(() => editableLabel?.BeginEditing()).ExecuteLater(0);

            return editableLabel != null;
        }

        /// <summary>
        /// Returns whether the passed keyboard event is a rename event on this platform
        /// </summary>
        /// <param name="e">The event.</param>
        /// <return>Whether the event is a key rename event</return>
        public static bool IsRenameKey<T>(KeyboardEventBase<T> e) where T : KeyboardEventBase<T>, new()
        {
#if UNITY_STANDALONE_OSX
            return e.keyCode == KeyCode.Return && e.modifiers == EventModifiers.None;
#else
            return e.keyCode == KeyCode.F2 && (e.modifiers & ~EventModifiers.FunctionKey) == EventModifiers.None;
#endif
        }

        /// <summary>
        /// Callback for the KeyDownEvent to handle renames.
        /// </summary>
        /// <param name="e">The event.</param>
        protected internal void OnRenameKeyDown(KeyDownEvent e)
        {
            if (IsRenameKey(e))
            {
                if (Model.IsRenamable())
                {
                    if (!hierarchy.parent.ChangeCoordinatesTo(GraphView, layout).Overlaps(GraphView.layout))
                    {
                        GraphView.DispatchFrameAndSelectElementsCommand(false, this);
                    }

                    if (Rename())
                    {
                        e.StopPropagation();
                    }
                }
            }
        }
    }
}
