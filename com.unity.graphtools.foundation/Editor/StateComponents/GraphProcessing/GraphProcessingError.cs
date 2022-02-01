using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Definition of a graph processing error.
    /// </summary>
    public class GraphProcessingError
    {
        /// <summary>
        /// Description of the error.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Node that is the source of error.
        /// </summary>
        public INodeModel SourceNode { get; set; }

        /// <summary>
        /// Unique ID of the node that is the source of the error.
        /// </summary>
        public SerializableGUID SourceNodeGuid { get; set; }

        /// <summary>
        /// QuickFix to address the error.
        /// </summary>
        public QuickFix Fix { get; set; }

        /// <summary>
        /// Whether this is an error or a warning.
        /// </summary>
        public bool IsWarning { get; set; }

        /// <summary>
        /// Returns a string that represents the current error.
        /// </summary>
        /// <returns>A string that represents the current error.</returns>
        public override string ToString()
        {
            return $"Graph Processing Error: {Description}";
        }
    }
}
