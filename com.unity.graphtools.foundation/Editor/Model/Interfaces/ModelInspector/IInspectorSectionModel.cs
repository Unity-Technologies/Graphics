using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The section type. The inspector only supports those three types.
    /// </summary>
    [Serializable]
    public enum SectionType
    {
        Settings,
        Properties,
        Advanced
    }

    /// <summary>
    /// Interface for inspector section view model.
    /// </summary>
    public interface IInspectorSectionModel: IModel, IHasTitle, ICollapsible
    {
        /// <summary>
        /// The section type.
        /// </summary>
        SectionType SectionType { get; }
        /// <summary>
        /// Whether the section is collapsible.
        /// </summary>
        bool Collapsible { get; }
    }
}
