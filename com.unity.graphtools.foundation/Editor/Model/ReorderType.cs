namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// How to reorder an item in a collection.
    /// </summary>
    public enum ReorderType
    {
        /// <summary>
        /// Make the item the first.
        /// </summary>
        MoveFirst,
        /// <summary>
        /// Move the item one position towards the beginning.
        /// </summary>
        MoveUp,
        /// <summary>
        /// Move the item one position towards the end.
        /// </summary>
        MoveDown,
        /// <summary>
        /// Make the item the last.
        /// </summary>
        MoveLast,
    }
}
