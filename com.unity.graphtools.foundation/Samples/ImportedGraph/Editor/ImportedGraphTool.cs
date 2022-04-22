using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.ImportedGraph
{
    class ImportedGraphTool : BaseGraphTool
    {
        public static readonly string toolName = "Imported Graph Editor";

        public ImportedGraphTool()
        {
            Name = toolName;
        }

        /// <inheritdoc />
        protected override void InitState()
        {
            base.InitState();
            Preferences.SetInitialSearcherSize(SearcherService.Usage.CreateNode, new Vector2(375, 300), 2.0f);
        }

        /// <inheritdoc />
        protected override IOverlayToolbarProvider CreateToolbarProvider(string toolbarId)
        {
            switch (toolbarId)
            {
                case MainOverlayToolbar.toolbarId:
                    return new MainToolbarProvider();
                default:
                    return base.CreateToolbarProvider(toolbarId);
            }
        }
    }
}
