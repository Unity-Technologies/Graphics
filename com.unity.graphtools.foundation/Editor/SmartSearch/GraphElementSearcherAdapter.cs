using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Searcher adapter for <see cref="IGraphElementModel"/>.
    /// </summary>
    public abstract class GraphElementSearcherAdapter : SearcherAdapter, IGTFSearcherAdapter
    {
        protected Label m_DetailsDescriptionTitle;
        protected ScrollView m_DetailsPreviewContainer;

        static readonly string k_DetailsDescriptionTitleClassName = k_DetailsTitleClassName.WithUssModifier("description");
        const string k_DetailsNodeClassName = "ge-searcher-details-preview-container";
        const string k_HidePreviewClassName = "ge-searcher-details--hidden";
        const string k_DescriptionTitle = "Description";

        protected GraphElementSearcherAdapter(string title, string toolName = null) : base(title, toolName) {}

        /// <inheritdoc />
        public override void InitDetailsPanel(VisualElement detailsPanel)
        {
            base.InitDetailsPanel(detailsPanel);

            m_DetailsPreviewContainer = MakeDetailsPreviewContainer();
            detailsPanel.Insert(1, m_DetailsPreviewContainer);
            m_DetailsDescriptionTitle = MakeDetailsTitleLabel();
            m_DetailsDescriptionTitle.text = k_DescriptionTitle;
            m_DetailsDescriptionTitle.AddToClassList(k_DetailsDescriptionTitleClassName);
            detailsPanel.Insert(2, m_DetailsDescriptionTitle);

            detailsPanel.AddStylesheet("SearcherAdapter.uss");
        }

        /// <summary>
        /// Creates a container for the preview in Details section.
        /// </summary>
        /// <returns>A container with uss class for a preview container in the details panel.</returns>
        protected static ScrollView MakeDetailsPreviewContainer()
        {
            var previewContainer = new ScrollView();
            previewContainer.StretchToParentSize();
            previewContainer.AddToClassList(k_DetailsNodeClassName);

            var eventCatcher = new VisualElement();
            eventCatcher.RegisterCallback<MouseDownEvent>(e => e.StopImmediatePropagation());
            eventCatcher.RegisterCallback<MouseMoveEvent>(e => e.StopImmediatePropagation());
            previewContainer.Add(eventCatcher);
            eventCatcher.StretchToParentSize();

            previewContainer.Add(SearcherService.GraphView);
            previewContainer.style.position = Position.Relative;

            return previewContainer;
        }

        /// <inheritdoc />
        protected override void UpdateDetailsPanel(SearcherItem searcherItem)
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

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphNodeSearcherAdapter"/> class.
        /// </summary>
        /// <param name="graphModel">The graph in which this adapter is used.</param>
        /// <param name="title">The title to use when searching with this adapter.</param>
        /// <param name="toolName">Unique, human-readable name of the tool using this adapter.</param>
        public GraphNodeSearcherAdapter(IGraphModel graphModel, string title, string toolName = null)
            : base(title, toolName)
        {
            m_GraphModel = graphModel;
        }

        protected override void UpdateDetailsPanel(SearcherItem searcherItem)
        {
            base.UpdateDetailsPanel(searcherItem);

            var graphView = SearcherService.GraphView;
            graphView.ClearGraph();

            var elements = CreateGraphElementModels(m_GraphModel, searcherItem).ToList();
            foreach (var element in elements.Where(element => element is INodeModel || element is IStickyNoteModel))
            {
                var node = GraphElementFactory.CreateUI<GraphElement>(graphView, element);
                if (node != null)
                {
                    node.style.position = Position.Relative;
                    graphView.AddElement(node);
                }
            }
        }

        public override bool ItemHasPreview(SearcherItem item)
        {
            return item is GraphNodeModelSearcherItem;
        }

        protected static IGraphElementModel[] CreateGraphElementModels(IGraphModel mGraphModel, SearcherItem item)
        {
            return item is GraphNodeModelSearcherItem graphItem
                ? graphItem.CreateElements.Invoke(
                new GraphNodeCreationData(mGraphModel, Vector2.zero, SpawnFlags.Orphan))
                : Array.Empty<IGraphElementModel>();
        }
    }
}
