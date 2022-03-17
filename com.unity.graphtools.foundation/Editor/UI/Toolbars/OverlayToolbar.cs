#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for overlay toolbars.
    /// </summary>
    public class OverlayToolbar : Overlay, ICreateToolbar
    {
        /// <summary>
        /// The graph tool.
        /// </summary>
        protected BaseGraphTool GraphTool => (containerWindow as GraphViewEditorWindow)?.GraphTool;

        /// <inheritdoc />
        public virtual IEnumerable<string> toolbarElements => GraphTool?.GetToolbarProvider(this)?.GetElementIds() ?? Enumerable.Empty<string>();

        /// <inheritdoc />
        public override VisualElement CreatePanelContent()
        {
            return GraphViewStaticBridge.CreateEditorToolbar(toolbarElements, containerWindow);
        }

        /// <summary>
        /// Adds a stylesheet to the toolbar root visual element.
        /// </summary>
        /// <param name="stylesheet"></param>
        protected void AddStylesheet(string stylesheet)
        {
            var root = GraphViewStaticBridge.GetOverlayRoot(this);
            root.AddStylesheet(stylesheet);
        }
    }
}
#endif
