using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Toolbars
{
    public class ShaderGraphMainToolbarProvider : IOverlayToolbarProvider, IToolbarProvider
    {
        public IEnumerable<string> GetElementIds()
        {
            return new[] { ShaderGraphSaveButton.id, ShaderGraphSaveAsButton.id, ShaderGraphShowInProjectButton.id };
        }

        public bool ShowButton(string buttonName)
        {
            return buttonName != MainToolbar.BuildAllButton && buttonName != MainToolbar.NewGraphButton && buttonName != MainToolbar.ShowMiniMapButton;
        }
    }
}
