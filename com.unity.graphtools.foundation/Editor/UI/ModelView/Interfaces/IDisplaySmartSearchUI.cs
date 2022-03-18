using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for ModelView that will display a custom smart search.
    /// </summary>
    interface IDisplaySmartSearchUI
    {
        /// <summary>
        /// Display the smart search.
        /// </summary>
        /// <param name="mousePosition">The position of the mouse in window coordinates.</param>
        /// <returns>True if the searcher could be displayed.</returns>
        bool DisplaySmartSearch(Vector2 mousePosition);
    }
}
