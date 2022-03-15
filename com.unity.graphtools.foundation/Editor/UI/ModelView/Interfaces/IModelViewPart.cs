using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The interface for UI part.
    /// </summary>
    public interface IModelViewPart
    {
        /// <summary>
        /// The part name.
        /// </summary>
        string PartName { get; }

        /// <summary>
        /// The root visual element of the part.
        /// </summary>
        VisualElement Root { get; }

        /// <summary>
        /// Builds the UI for the part.
        /// </summary>
        /// <param name="parent">The parent visual element to which the UI of the part will be attached.</param>
        void BuildUI(VisualElement parent);

        /// <summary>
        /// Called once the UI has been built.
        /// </summary>
        /// <remarks>This is a good place to add stylesheets that need to have a higher priority than the stylesheets of the children.</remarks>
        void PostBuildUI();

        /// <summary>
        /// Updates the part using the associated model.
        /// </summary>
        void UpdateFromModel();

        /// <summary>
        /// Called when the part owner is added to a <see cref="GraphView"/>.
        /// </summary>
        void OwnerAddedToView();

        /// <summary>
        /// Called when the part owner is removed from a <see cref="GraphView"/>.
        /// </summary>
        void OwnerRemovedFromView();
    }
}
