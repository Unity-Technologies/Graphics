#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Overlays;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The main toolbar.
    /// </summary>
    [Overlay(typeof(GraphViewEditorWindow), toolbarId, "Asset Management", ussName = "AssetManagement",
        defaultDisplay = true, defaultDockZone = DockZone.TopToolbar, defaultDockPosition = DockPosition.Top,
        defaultDockIndex = 0, defaultLayout = Layout.HorizontalToolbar)]
    [Icon("Icons/Overlays/ToolsToggle.png")]
    public sealed class MainOverlayToolbar : OverlayToolbar
    {
        public const string toolbarId = "gtf-asset-management";
    }
}
#endif
