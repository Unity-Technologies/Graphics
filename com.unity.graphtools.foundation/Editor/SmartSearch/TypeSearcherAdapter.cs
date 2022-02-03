namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Searcher adapter for types.
    /// </summary>
    public class TypeSearcherAdapter : SimpleSearcherAdapter
    {
        /// <summary>
        /// Initializes a new instance of the TypeSearcherAdapter class.
        /// </summary>
        public TypeSearcherAdapter()
            : base("Pick a type") {}

        /// <summary>
        /// Initializes a new instance of the TypeSearcherAdapter class.
        /// </summary>
        /// <param name="title">Title to display when prompting the search.</param>
        public TypeSearcherAdapter(string title)
            : base(title) {}
    }
}
