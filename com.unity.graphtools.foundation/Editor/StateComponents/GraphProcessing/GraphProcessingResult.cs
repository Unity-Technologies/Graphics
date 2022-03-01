using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A container for graph processing errors and warnings.
    /// </summary>
    public class GraphProcessingResult
    {
        readonly List<GraphProcessingError> m_Errors = new List<GraphProcessingError>();

        /// <summary>
        /// The errors.
        /// </summary>
        public IReadOnlyList<GraphProcessingError> Errors => m_Errors;

        /// <summary>
        /// The graph processing status.
        /// </summary>
        public GraphProcessingStatuses Status => Errors.Any(e => e.IsWarning == false) ?
        GraphProcessingStatuses.Failed : GraphProcessingStatuses.Succeeded;

        /// <summary>
        /// Adds an error.
        /// </summary>
        /// <param name="description">Error description.</param>
        /// <param name="node">The node associated with the error.</param>
        /// <param name="quickFix">How to fix this error.</param>
        public void AddError(string description, INodeModel node = null, QuickFix quickFix = null)
        {
            AddError(description, node, false, quickFix);
        }

        /// <summary>
        /// Adds a warning.
        /// </summary>
        /// <param name="description">Warning description.</param>
        /// <param name="node">The node associated with the warning.</param>
        /// <param name="quickFix">How to fix this warning.</param>
        public void AddWarning(string description, INodeModel node = null, QuickFix quickFix = null)
        {
            AddError(description, node, true, quickFix);
        }

        void AddError(string desc, INodeModel node, bool isWarning, QuickFix quickFix)
        {
            m_Errors.Add(new GraphProcessingError { Description = desc, SourceNode = node, SourceNodeGuid = node == null ? default : node.Guid, IsWarning = isWarning, Fix = quickFix });
        }
    }
}
