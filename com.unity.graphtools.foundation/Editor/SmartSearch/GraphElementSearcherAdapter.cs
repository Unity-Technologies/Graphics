using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Searcher adapter for <see cref="IGraphElementModel"/>.
    /// </summary>
    public abstract class GraphElementSearcherAdapter : SearcherAdapter, ISearcherAdapter
    {
        protected Label m_DetailsDescriptionTitle;
        protected ScrollView m_DetailsPreviewContainer;

        protected const string k_BaseClassName = "ge-searcher-details";
        protected readonly string k_HidePreviewClassName = k_BaseClassName.WithUssModifier("hidden");
        static readonly string k_DetailsDescriptionTitleClassName = k_DetailsTitleClassName.WithUssModifier("description");
        const string k_DetailsNodeClassName = "ge-searcher-details-preview-container";
        const string k_DescriptionTitle = "Description";

        protected GraphElementSearcherAdapter(string title, string toolName = null) : base(title, toolName) {}

        /// <inheritdoc />
        public override void InitDetailsPanel(VisualElement detailsPanel)
        {
            base.InitDetailsPanel(detailsPanel);

            m_DetailsPreviewContainer = MakeDetailsPreviewContainer();
            detailsPanel.Insert(1, m_DetailsPreviewContainer);
            m_DetailsDescriptionTitle = MakeDetailsTitleLabel(k_DescriptionTitle);
            m_DetailsDescriptionTitle.AddToClassList(k_DetailsDescriptionTitleClassName);
            detailsPanel.Insert(2, m_DetailsDescriptionTitle);

            detailsPanel.AddStylesheet("SearcherAdapter.uss");
        }

        /// <summary>
        /// Creates a container for the preview in Details section.
        /// </summary>
        /// <returns>A container with uss class for a preview container in the details panel.</returns>
        protected virtual ScrollView MakeDetailsPreviewContainer()
        {
            var previewContainer = new ScrollView();
            previewContainer.StretchToParentSize();
            previewContainer.AddToClassList(k_DetailsNodeClassName);
            previewContainer.style.position = Position.Relative;

            return previewContainer;
        }

        /// <inheritdoc />
        public override void UpdateDetailsPanel(SearcherItem searcherItem)
        {
            base.UpdateDetailsPanel(searcherItem);

            var showPreview = ItemHasPreview(searcherItem);
            m_DetailsPreviewContainer.EnableInClassList(k_HidePreviewClassName, !showPreview);
            var hasDescriptionTitle = showPreview && !string.IsNullOrEmpty(m_DetailsTextLabel.text);
            m_DetailsDescriptionTitle.EnableInClassList(k_HidePreviewClassName, !hasDescriptionTitle);
        }

        [Obsolete("Please override `UpdateDetailsPanel` to create a preview graph element. See `GraphNodeSearcherAdapter`.")]
        protected virtual IEnumerable<IGraphElementModel> CreateGraphElements(SearcherItem item)
        {
            throw new NotImplementedException();
        }

        [Obsolete("Please override `UpdateDetailsPanel` to hook to the creation of the node in the preview panel.")]
        protected virtual void OnGraphElementsCreated(SearcherItem searcherItem,
            IEnumerable<IGraphElementModel> elements)
        {}


        float m_InitialSplitterDetailRatio = 1.0f;

        /// <inheritdoc />
        public override float InitialSplitterDetailRatio => m_InitialSplitterDetailRatio;

        public virtual bool ItemHasPreview(SearcherItem item)
        {
            return false;
        }

        /// <inheritdoc />
        public void SetInitialSplitterDetailRatio(float ratio)
        {
            m_InitialSplitterDetailRatio = ratio;
        }
    }

    /// <summary>
    /// Searcher adapter for <see cref="IGraphElementModel"/>.
    /// </summary>
    public class GraphNodeSearcherAdapter : GraphElementSearcherAdapter
    {
        readonly IGraphModel m_GraphModel;
        readonly Type m_GraphViewType;

        GraphView m_GraphView;
        GraphElement m_CurrentElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphNodeSearcherAdapter"/> class.
        /// </summary>
        /// <param name="graphModel">The graph in which this adapter is used.</param>
        /// <param name="title">The title to use when searching with this adapter.</param>
        /// <param name="toolName">Unique, human-readable name of the tool using this adapter.</param>
        /// <param name="graphViewType">The type of <see cref="GraphView"/> to create in the details panel.</param>
        public GraphNodeSearcherAdapter(IGraphModel graphModel, string title, string toolName = null, Type graphViewType = null)
            : base(title, toolName)
        {
            if (!typeof(GraphView).IsAssignableFrom(graphViewType))
            {
                throw new ArgumentException("Type must derive from GraphView.", nameof(graphViewType));
            }

            m_GraphModel = graphModel;
            m_GraphViewType = graphViewType ?? typeof(GraphView);
        }

        /// <inheritdoc />
        protected override ScrollView MakeDetailsPreviewContainer()
        {
            var scrollView = base.MakeDetailsPreviewContainer();
            m_GraphView = SearcherGraphView.CreateSearcherGraphView(m_GraphViewType);
            scrollView.Add(m_GraphView);
            return scrollView;
        }

        public override void UpdateDetailsPanel(SearcherItem searcherItem)
        {
            base.UpdateDetailsPanel(searcherItem);

            m_GraphView.RemoveElement(m_CurrentElement);

            if (ItemHasPreview(searcherItem))
            {
                var graphItem = searcherItem as GraphNodeModelSearcherItem;
                var model = CreateGraphElementModel(m_GraphModel, graphItem);
                m_CurrentElement = ModelViewFactory.CreateUI<GraphElement>(m_GraphView, model);
                if (m_CurrentElement != null)
                {
                    m_CurrentElement.style.position = Position.Relative;
                    m_GraphView.AddElement(m_CurrentElement);
                }
            }
        }

        public override bool ItemHasPreview(SearcherItem item)
        {
            return item is GraphNodeModelSearcherItem;
        }

        protected static IGraphElementModel CreateGraphElementModel(IGraphModel graphModel, GraphNodeModelSearcherItem item)
        {
            return item.CreateElement.Invoke(
                new GraphNodeCreationData(graphModel, Vector2.zero, SpawnFlags.Orphan));
        }
    }
}
