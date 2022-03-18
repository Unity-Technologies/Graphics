using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A section in the inspector.
    /// </summary>
    public class InspectorSection : ModelView
    {
        public static readonly string ussClassName = "ge-inspector-section";
        public static readonly string contentContainerUssClassName = ussClassName.WithUssElement("content-container");

        /// <summary>
        /// The content container.
        /// </summary>
        protected VisualElement m_ContentContainer;

        /// <inheritdoc />
        public override VisualElement contentContainer => m_ContentContainer ?? base.contentContainer;

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            m_ContentContainer = new VisualElement { name = "content-container" };
            m_ContentContainer.AddToClassList(contentContainerUssClassName);
            hierarchy.Add(m_ContentContainer);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
            this.AddStylesheet("InspectorSection.uss");
        }
    }
}
