using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;

#if UNITY_2022_2_OR_NEWER
namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// ShaderGraphs implementation of <see cref="IToolbarProvider"/> for the toggle toolbar.
    /// </summary>
    public class SGPanelsToolbarProvider : IOverlayToolbarProvider
    {
        /// <inheritdoc />
        public virtual IEnumerable<string> GetElementIds()
        {
            return new[] { SGBlackboardPanelToggle.id, SGInspectorPanelToggle.id, PreviewPanelToggle.id };
        }
    }
}
#endif

