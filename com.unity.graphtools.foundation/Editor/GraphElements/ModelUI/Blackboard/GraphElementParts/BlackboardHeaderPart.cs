using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A part to build the UI for the blackboard header.
    /// </summary>
    public class BlackboardHeaderPart : BaseModelUIPart
    {
        public static readonly string ussClassName = "ge-blackboard-header-part";
        public static readonly string titleUssClassName = ussClassName.WithUssElement("title");
        public static readonly string subTitleUssClassName = ussClassName.WithUssElement("subtitle");

        protected static readonly string defaultTitle = "Blackboard";
        protected static readonly string defaultSubTitle = "";

        /// <summary>
        /// Creates a new instance of the <see cref="BlackboardHeaderPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="BlackboardHeaderPart"/>.</returns>
        public static BlackboardHeaderPart Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            if (model is IBlackboardGraphModel)
            {
                return new BlackboardHeaderPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected VisualElement m_Root;

        protected Label m_TitleLabel;
        protected Label m_SubTitleLabel;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardHeaderPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected BlackboardHeaderPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_TitleLabel = new Label { name = "title-label" };
            m_TitleLabel.AddToClassList(titleUssClassName);
            m_SubTitleLabel = new Label { name = "sub-title-label" };
            m_SubTitleLabel.AddToClassList(subTitleUssClassName);

            m_Root.Add(m_TitleLabel);
            m_Root.Add(m_SubTitleLabel);

            parent.Add(m_Root);
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (m_Model is IBlackboardGraphModel graphProxyElement && graphProxyElement.Valid)
            {
                m_TitleLabel.text = graphProxyElement.GetBlackboardTitle();
                m_SubTitleLabel.text = graphProxyElement.GetBlackboardSubTitle();
            }
            else
            {
                m_TitleLabel.text = defaultTitle;
                m_SubTitleLabel.text = defaultSubTitle;
            }
        }
    }
}
