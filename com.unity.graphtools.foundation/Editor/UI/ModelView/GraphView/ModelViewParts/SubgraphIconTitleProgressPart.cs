// ReSharper disable once RedundantUsingDirective : needed by 2020.3
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A part to build the UI for the <see cref="IconTitleProgressPart"/> of a <see cref="ISubgraphNodeModel"/>.
    /// </summary>
    public class SubgraphIconTitleProgressPart : IconTitleProgressPart
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubgraphIconTitleProgressPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="SubgraphIconTitleProgressPart"/>.</returns>
        public new static SubgraphIconTitleProgressPart Create(string name, IModel model, IModelView ownerElement, string parentClassName)
        {
            return model is ISubgraphNodeModel subgraphNode ? new SubgraphIconTitleProgressPart(name, subgraphNode, ownerElement, parentClassName) : null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubgraphIconTitleProgressPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        SubgraphIconTitleProgressPart(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is ISubgraphNodeModel subgraphNodeModel)
            {
                base.BuildPartUI(container);

                m_Icon.AddToClassList(ussClassName.WithUssElement("asset-graph-icon"));
                m_Icon.AddToClassList(m_ParentClassName.WithUssElement("asset-graph-icon"));

                if (subgraphNodeModel.SubgraphAssetModel == null)
                {
                    var warningIcon = new Image { name = "missing-graph-icon" };
                    warningIcon.AddToClassList(ussClassName.WithUssElement("icon"));
                    warningIcon.AddToClassList(ussClassName.WithUssElement("missing-graph-icon"));
                    TitleContainer.Add(warningIcon);
                    TitleContainer.Add(TitleLabel);
                }
            }
        }
    }
}
