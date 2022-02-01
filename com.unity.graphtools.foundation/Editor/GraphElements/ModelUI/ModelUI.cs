using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for all UI element that displays a <see cref="IGraphElementModel"/>.
    /// </summary>
    public abstract class ModelUI : VisualElement, IModelUI
    {
        /// <inheritdoc />
        public IGraphElementModel Model { get; private set; }

        /// <inheritdoc />
        public IModelView View { get; protected set; }

        /// <inheritdoc />
        public IUIContext Context { get; private set; }

        public ModelUIPartList PartList { get; private set; }

        protected UIDependencies Dependencies { get; }

        ContextualMenuManipulator m_ContextualMenuManipulator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelUI"/> class.
        /// </summary>
        protected ModelUI()
        {
            Dependencies = new UIDependencies(this);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

            ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);
        }

        protected ContextualMenuManipulator ContextualMenuManipulator
        {
            get => m_ContextualMenuManipulator;
            set => this.ReplaceManipulator(ref m_ContextualMenuManipulator, value);
        }

        /// <summary>
        /// Builds the list of parts for this UI Element.
        /// </summary>
        protected virtual void BuildPartList() {}

        /// <inheritdoc />
        public void SetupBuildAndUpdate(IGraphElementModel model, IModelView view, IUIContext context = null)
        {
            Setup(model, view, context);
            BuildUI();
            UpdateFromModel();
        }

        /// <inheritdoc />
        public void Setup(IGraphElementModel model, IModelView view, IUIContext context)
        {
            Model = model;
            View = view;
            Context = context;

            PartList = new ModelUIPartList();
            BuildPartList();
        }

        /// <inheritdoc />
        public void BuildUI()
        {
            ClearElementUI();
            BuildElementUI();

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.BuildUI(this);
            }

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.PostBuildUI();
            }

            PostBuildUI();
        }

        /// <inheritdoc />
        public void UpdateFromModel()
        {
            if (View?.GraphTool?.Preferences.GetBool(BoolPref.LogUIUpdate) ?? false)
            {
                Debug.Log($"Rebuilding {this}");
                if (View == null)
                {
                    Debug.LogWarning($"Updating a model UI that is not attached to a view: {this}");
                }
            }

            UpdateElementFromModel();

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.UpdateFromModel();
            }

            Dependencies.UpdateDependencyLists();
        }

        /// <summary>
        /// Removes all children VisualElements.
        /// </summary>
        protected virtual void ClearElementUI()
        {
            Clear();
        }

        /// <summary>
        /// Build the UI for this instance: instantiates VisualElements, sets USS classes.
        /// </summary>
        protected virtual void BuildElementUI()
        {
        }

        /// <summary>
        /// Finalizes the building of the UI. Stylesheets are typically added here.
        /// </summary>
        protected virtual void PostBuildUI()
        {
        }

        /// <summary>
        /// Update the element to reflect the state of the attached model.
        /// </summary>
        protected virtual void UpdateElementFromModel()
        {
        }

        protected void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            Dependencies.OnCustomStyleResolved(evt);
        }

        protected void OnGeometryChanged(GeometryChangedEvent evt)
        {
            Dependencies.OnGeometryChanged(evt);
        }

        protected void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            Dependencies.OnDetachedFromPanel(evt);
        }

        /// <inheritdoc />
        public virtual bool HasBackwardsDependenciesChanged() => false;
        /// <inheritdoc />
        public virtual bool HasForwardsDependenciesChanged() => false;
        /// <inheritdoc />
        public virtual bool HasModelDependenciesChanged() => false;

        /// <inheritdoc />
        public virtual void AddForwardDependencies()
        {
        }

        /// <inheritdoc />
        public virtual void AddBackwardDependencies()
        {
        }

        /// <inheritdoc />
        public virtual void AddModelDependencies()
        {
        }

        /// <summary>
        /// Callback to add menu items to the contextual menu.
        /// </summary>
        /// <param name="evt">The <see cref="ContextualMenuPopulateEvent"/>.</param>
        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }

        /// <inheritdoc />
        public virtual void AddToView(IModelView view)
        {
            View = view;
            UIForModel.AddOrReplaceGraphElement(this);

            if (PartList != null)
            {
                for (var i = 0; i < PartList.Parts.Count; i++)
                {
                    var component = PartList.Parts[i];
                    component.OwnerAddedToView();
                }
            }
        }

        /// <inheritdoc />
        public virtual void RemoveFromView()
        {
            if (PartList != null)
            {
                for (var i = 0; i < PartList.Parts.Count; i++)
                {
                    var component = PartList.Parts[i];
                    component.OwnerRemovedFromView();
                }
            }

            Dependencies.ClearDependencyLists();
            UIForModel.RemoveGraphElement(this);
            View = null;
        }

        /// <inheritdoc/>
        public virtual bool PasteIn(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData)
        {
            return false;
        }
    }
}
