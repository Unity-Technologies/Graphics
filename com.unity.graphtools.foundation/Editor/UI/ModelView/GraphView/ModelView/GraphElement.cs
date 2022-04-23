using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base UI class for models displayed in a <see cref="GraphView"/>.
    /// </summary>
    public abstract class GraphElement : ModelView
    {
        static readonly CustomStyleProperty<int> k_LayerProperty = new CustomStyleProperty<int>("--layer");
        static readonly CustomStyleProperty<Color> k_MinimapColorProperty = new CustomStyleProperty<Color>("--minimap-color");

        static Color DefaultMinimapColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(230/255f, 230/255f, 230/255f, 0.5f);
                }

                return new Color(200/255f, 200/255f, 200/255f, 1f);
            }
        }

        public static readonly string ussClassName = "ge-graph-element";
        public static readonly string selectableModifierUssClassName = ussClassName.WithUssModifier("selectable");

        int m_Layer;

        bool m_LayerIsInline;

        ClickSelector m_ClickSelector;

        public IGraphElementModel GraphElementModel => Model as IGraphElementModel;

        public GraphView GraphView => RootView as GraphView;

        public int Layer
        {
            get => m_Layer;
            set
            {
                m_LayerIsInline = true;
                m_Layer = value;
            }
        }

        public Color MinimapColor { get; protected set; } = DefaultMinimapColor;

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
            RegisterCallback<KeyDownEvent>(OnRenameKeyDown);
            focusable = true;

            ContextualMenuManipulator = new GraphViewContextualMenuManipulator(BuildContextualMenu);
        }

        public void ResetLayer()
        {
            int prevLayer = m_Layer;
            m_Layer = 0;
            m_LayerIsInline = false;
            customStyle.TryGetValue(k_LayerProperty, out m_Layer);
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
            this.AddStylesheet("GraphElement.uss");
        }

        /// <summary>
        /// Creates a <see cref="ClickSelector" /> for this element.
        /// </summary>
        /// <returns>A <see cref="ClickSelector" /> for this element.</returns>
        protected virtual ClickSelector CreateClickSelector()
        {
            return new GraphViewClickSelector();
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            if (GraphElementModel?.IsSelectable() ?? false)
                ClickSelector ??= CreateClickSelector();
            else
                ClickSelector = null;

            EnableInClassList(selectableModifierUssClassName, ClickSelector != null);

            this.SetCheckedPseudoState(IsSelected());
        }

        /// <summary>
        /// Set the visual appearance of the <see cref="GraphElement"/> and its parts depending on the current zoom.
        /// </summary>
        /// <param name="zoom">The current zoom.</param>
        public void SetLevelOfDetail(float zoom)
        {
            SetElementLevelOfDetail(zoom);
            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                (component as IGraphElementPart)?.SetLevelOfDetail(zoom);
            }
        }


        /// <summary>
        /// Can be overriden to set the visual appearance of the <see cref="GraphElement"/> depending on the current zoom.
        /// </summary>
        /// <param name="zoom">The current zoom.</param>
        public virtual void SetElementLevelOfDetail(float zoom)
        {
        }

        /// <inheritdoc />
        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            base.OnCustomStyleResolved(evt);

            if (evt.customStyle.TryGetValue(k_MinimapColorProperty, out var minimapColor))
                MinimapColor = minimapColor;

            int prevLayer = m_Layer;
            if (!m_LayerIsInline)
                evt.customStyle.TryGetValue(k_LayerProperty, out m_Layer);

            UpdateLayer(prevLayer);
        }

        public virtual bool IsMovable()
        {
            return GraphElementModel?.IsMovable() ?? false;
        }

        /// <summary>
        /// Sets the position of the element. This method has no effect if <see cref="ModelView.PositionIsOverriddenByManipulator"/> is set.
        /// </summary>
        /// <param name="position">The position.</param>
        public void SetPosition(Vector2 position)
        {
            if (!PositionIsOverriddenByManipulator)
            {
                SetPositionOverride(position);
            }
        }

        /// <summary>
        /// Unconditionally sets the position of the element.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <remarks>Use this method when the position from the model needs to be overriden during a manipulation.</remarks>
        public virtual void SetPositionOverride(Vector2 position)
        {
            style.left = position.x;
            style.top = position.y;
        }

        /// <summary>
        /// Checks if the underlying graph element model is selected.
        /// </summary>
        /// <returns>True if the model is selected, false otherwise.</returns>
        public bool IsSelected()
        {
            return GraphView?.GraphViewModel?.SelectionState?.IsSelected(GraphElementModel) ?? false;
        }

        /// <summary>
        /// Callback for the KeyDownEvent to handle renames.
        /// </summary>
        /// <param name="e">The event.</param>
        protected internal void OnRenameKeyDown(KeyDownEvent e)
        {
            if (IsRenameKey(e))
            {
                if (GraphElementModel.IsRenamable())
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
