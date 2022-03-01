using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A part to build the UI for the blackboard section list.
    /// </summary>
    public class BlackboardSectionListPart : BaseModelUIPart
    {
        public static readonly string ussClassName = "ge-blackboard-section-list-part";

        /// <summary>
        /// Creates a new instance of the <see cref="BlackboardSectionListPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="BlackboardSectionListPart"/>.</returns>
        public static BlackboardSectionListPart Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            if (model is IBlackboardGraphModel)
            {
                return new BlackboardSectionListPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// The blackboard containing this part.
        /// </summary>
        public Blackboard Blackboard => m_OwnerElement as Blackboard;

        /// <summary>
        /// The root of this part.
        /// </summary>
        protected VisualElement m_Root;

        /// <summary>
        /// A dictionary associating section names with the related <see cref="BlackboardSection"/>.
        /// </summary>
        protected Dictionary<string, BlackboardSection> m_Sections;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardSectionListPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected BlackboardSectionListPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            if (m_Model is IBlackboardGraphModel graphProxyElement)
            {
                m_Sections = new Dictionary<string, BlackboardSection>();
                foreach (var sectionName in m_OwnerElement.Model.GraphModel.Stencil.SectionNames)
                {
                    var section = GraphElementFactory.CreateUI<BlackboardSection>(m_OwnerElement.View, m_Model.GraphModel.GetSectionModel(sectionName));
                    section.name = sectionName;
                    section.AddToView(Blackboard.GraphView);
                    m_Root.Add(section);
                    m_Sections.Add(sectionName, section);
                }
            }

            parent.Add(m_Root);
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
        }

        /// <inheritdoc />
        protected override void PartOwnerAddedToView()
        {
            foreach (var section in m_Sections.Values)
            {
                section.AddToView(m_OwnerElement.View);
            }

            base.PartOwnerAddedToView();
        }

        /// <inheritdoc />
        protected override void PartOwnerRemovedFromView()
        {
            foreach (var section in m_Sections.Values)
            {
                section.RemoveFromView();
            }

            base.PartOwnerRemovedFromView();
        }
    }
}
