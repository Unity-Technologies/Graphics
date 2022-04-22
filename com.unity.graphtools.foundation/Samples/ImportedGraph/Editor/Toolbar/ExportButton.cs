using System;
using UnityEditor.Toolbars;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.ImportedGraph
{
    [EditorToolbarElement(id, typeof(ImportedGraphWindow))]
    public class ExportButton : SaveButton
    {
        public new const string id = "ImportedGraphSample/Main/Export";

        public ExportButton()
        {
            name = "Export";
            tooltip = L10n.Tr("Export To Grr File");
        }
    }
}
