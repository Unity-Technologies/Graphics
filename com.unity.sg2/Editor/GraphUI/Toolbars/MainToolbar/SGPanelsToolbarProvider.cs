using System.Collections.Generic;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// ShaderGraphs implementation of <see cref="OverlayToolbarProvider"/> for the toggle toolbar.
    /// </summary>
    class SGPanelsToolbarProvider : OverlayToolbarProvider
    {
        /// <inheritdoc />
        public override IEnumerable<string> GetElementIds()
        {
            return new[] { SGBlackboardPanelToggle.id, SGInspectorPanelToggle.id, PreviewPanelToggle.id };
        }
    }
}

