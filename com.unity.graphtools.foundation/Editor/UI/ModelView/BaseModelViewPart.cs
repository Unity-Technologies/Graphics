using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Abstract base class for UI parts.
    /// </summary>
    public abstract class BaseModelViewPart : IModelViewPart
    {
        /// <inheritdoc />
        public string PartName { get; }

        public ModelViewPartList PartList { get; } = new ModelViewPartList();

        /// <inheritdoc />
        public abstract VisualElement Root { get; }

        protected IModel m_Model;

        protected IModelView m_OwnerElement;

        protected string m_ParentClassName;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseModelViewPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected BaseModelViewPart(string name, IModel model, IModelView ownerElement, string parentClassName)
        {
            PartName = name;
            m_Model = model;
            m_OwnerElement = ownerElement;
            m_ParentClassName = parentClassName;
        }

        /// <inheritdoc />
        public void BuildUI(VisualElement parent)
        {
            BuildPartUI(parent);

            if (Root != null)
            {
                for (var i = 0; i < PartList.Parts.Count; i++)
                {
                    var component = PartList.Parts[i];
                    component.BuildUI(Root);
                }
            }
        }

        /// <inheritdoc />
        public void PostBuildUI()
        {
            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.PostBuildUI();
            }

            PostBuildPartUI();
        }

        /// <inheritdoc />
        public void UpdateFromModel()
        {
            UpdatePartFromModel();

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.UpdateFromModel();
            }
        }

        /// <inheritdoc />
        public void OwnerAddedToView()
        {
            PartOwnerAddedToView();

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.OwnerAddedToView();
            }
        }

        /// <inheritdoc />
        public void OwnerRemovedFromView()
        {
            PartOwnerRemovedFromView();

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.OwnerRemovedFromView();
            }
        }

        /// <summary>
        /// Creates the UI for this part.
        /// </summary>
        /// <param name="parent">The parent element to attach the created UI to.</param>
        protected abstract void BuildPartUI(VisualElement parent);

        /// <summary>
        /// Finalizes the building of the UI.
        /// </summary>
        /// <remarks>This is a good place to add stylesheets that need to have a higher priority than the stylesheets of the children.</remarks>
        protected virtual void PostBuildPartUI() {}

        /// <summary>
        /// Updates the part to reflect the assigned model.
        /// </summary>
        protected abstract void UpdatePartFromModel();

        protected virtual void PartOwnerAddedToView() {}
        protected virtual void PartOwnerRemovedFromView() {}
    }
}
