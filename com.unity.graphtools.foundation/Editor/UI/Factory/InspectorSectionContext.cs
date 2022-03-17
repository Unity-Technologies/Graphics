using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A UI creation context for inspector section content.
    /// </summary>
    public class InspectorSectionContext : IViewContext
    {
        /// <summary>
        /// The inspector section.
        /// </summary>
        public IInspectorSectionModel Section { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InspectorSectionContext"/> class.
        /// </summary>
        /// <param name="sectionContext">The section to use as context.</param>
        public InspectorSectionContext(IInspectorSectionModel sectionContext)
        {
            Section = sectionContext;
        }

        /// <inheritdoc />
        public bool Equals(IViewContext other)
        {
            if (other is InspectorSectionContext inspectorSectionContext)
                return ReferenceEquals(Section, inspectorSectionContext.Section);
            return false;
        }
    }
}
