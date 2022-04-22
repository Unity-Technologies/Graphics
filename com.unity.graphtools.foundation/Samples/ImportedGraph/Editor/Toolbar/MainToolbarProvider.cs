using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.ImportedGraph
{
    public class MainToolbarProvider : UnityEditor.GraphToolsFoundation.Overdrive.MainToolbarProvider
    {
        /// <inheritdoc />
        public override IEnumerable<string> GetElementIds()
        {
            return base.GetElementIds().Where(id => id != SaveButton.id).Append(ExportButton.id);
        }
    }
}
