using System.Collections.Generic;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Toolbars
{
    class ShaderGraphMainToolbarProvider : OverlayToolbarProvider
    {
        public override IEnumerable<string> GetElementIds()
        {
            return new[] { ShaderGraphSaveButton.id, ShaderGraphSaveAsButton.id, ShaderGraphShowInProjectButton.id };
        }
    }
}
