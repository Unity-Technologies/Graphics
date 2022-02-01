using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A part to build the UI for the <see cref="IconTitleProgressPart"/> of a <see cref="ISubgraphNodeModel"/>.
    /// </summary>
    public class SubgraphIconTitleProgressPart : IconTitleProgressPart
    {
        public static readonly string graphTypeLabelUssClassName = "graph-type-label";
        public static readonly string graphTypeIconUssClassName = "graph-type-icon";

        /// <summary>
        /// Initializes a new instance of the <see cref="SubgraphIconTitleProgressPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="SubgraphIconTitleProgressPart"/>.</returns>
        public new static SubgraphIconTitleProgressPart Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            return model is ISubgraphNodeModel ? new SubgraphIconTitleProgressPart(name, model, ownerElement, parentClassName) : null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubgraphIconTitleProgressPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        SubgraphIconTitleProgressPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is ISubgraphNodeModel subgraphNodeModel)
            {
                m_Root = new VisualElement { name = PartName };
                m_Root.AddToClassList(ussClassName);
                m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                TitleContainer = new VisualElement();
                TitleContainer.AddToClassList(ussClassName.WithUssElement("title-container"));
                TitleContainer.AddToClassList(ussClassName.WithUssElement("title-container"));
                TitleContainer.AddToClassList(m_ParentClassName.WithUssElement("title-container"));
                m_Root.Add(TitleContainer);

                var leftContainer = new VisualElement();
                leftContainer.AddToClassList(ussClassName.WithUssElement("left-container"));
                TitleContainer.Add(leftContainer);

                var icon = new VisualElement();
                icon.AddToClassList(ussClassName.WithUssElement("icon"));
                icon.AddToClassList(m_ParentClassName.WithUssElement("icon"));
                if (!string.IsNullOrEmpty(subgraphNodeModel.IconTypeString))
                {
                    icon.AddToClassList(ussClassName.WithUssElement("icon").WithUssModifier(subgraphNodeModel.IconTypeString));
                    icon.AddToClassList(m_ParentClassName.WithUssElement("icon").WithUssModifier(subgraphNodeModel.IconTypeString));
                }
                leftContainer.Add(icon);

                var labelsContainer = new VisualElement();
                leftContainer.Add(labelsContainer);

                if (subgraphNodeModel.ReferenceGraphAssetModel != null)
                {
                    var graphTypeLabel = new Label("Asset Graph") { name = graphTypeLabelUssClassName };
                    graphTypeLabel.AddToClassList(ussClassName.WithUssElement(graphTypeLabelUssClassName));
                    labelsContainer.Add(graphTypeLabel);

                    var rightContainer = new VisualElement();
                    TitleContainer.Add(rightContainer);

                    var graphTypeIcon = new Image { name = graphTypeIconUssClassName };
                    graphTypeIcon.AddToClassList(ussClassName.WithUssElement(graphTypeIconUssClassName));
                    graphTypeIcon.AddToClassList(ussClassName.WithUssElement("asset-graph-icon"));
                    rightContainer.Add(graphTypeIcon);
                }

                if (HasEditableLabel)
                {
                    TitleLabel = new EditableLabel { name = titleLabelName };
                    TitleLabel.RegisterCallback<ChangeEvent<string>>(OnRename);
                }
                else
                {
                    TitleLabel = new Label { name = titleLabelName };
                }

                TitleLabel.AddToClassList(ussClassName.WithUssElement("title"));
                TitleLabel.AddToClassList(m_ParentClassName.WithUssElement("title"));
                labelsContainer.Add(TitleLabel);

                if (subgraphNodeModel is IHasProgress hasProgress && hasProgress.HasProgress)
                {
                    CoroutineProgressBar = new ProgressBar();
                    CoroutineProgressBar.AddToClassList(ussClassName.WithUssElement("progress-bar"));
                    CoroutineProgressBar.AddToClassList(m_ParentClassName.WithUssElement("progress-bar"));
                    TitleContainer.Add(CoroutineProgressBar);
                }

                container.Add(m_Root);
            }
        }

        /// <inheritdoc />
        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            m_Root.AddStylesheet("SubgraphIconTitleProgressPart.uss");
        }
    }
}
