using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base <see cref="SearcherAdapter"/> used in Graph Tools foundation.
    /// </summary>
    public class SimpleSearcherAdapter : SearcherAdapter, ISearcherAdapter
    {
        /// <summary>
        /// Initializes a new instance of the SimpleSearcherAdapter class.
        /// </summary>
        /// <param name="title">Title to display when prompting the search.</param>
        public SimpleSearcherAdapter(string title)
            : base(title)
        {
        }

        // TODO: Disable details panel for now
        /// <inheritdoc />
        public override bool HasDetailsPanel => false;

        float m_InitialSplitterDetailRatio = 1.0f;

        /// <inheritdoc />
        public override float InitialSplitterDetailRatio => m_InitialSplitterDetailRatio;

        /// <inheritdoc />
        public void SetInitialSplitterDetailRatio(float ratio)
        {
            m_InitialSplitterDetailRatio = ratio;
        }
    }
}
