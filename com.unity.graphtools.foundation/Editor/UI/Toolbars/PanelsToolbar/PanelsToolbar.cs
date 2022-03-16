#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Overlays;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The panel toggle toolbar.
    /// </summary>
    [Overlay(typeof(GraphViewEditorWindow), toolbarId, "Panel Toggles", ussName = "PanelToggles",
        defaultDisplay = true, defaultDockZone = DockZone.TopToolbar, defaultDockPosition = DockPosition.Bottom,
        defaultDockIndex = 1, defaultLayout = Layout.HorizontalToolbar)]
    [Icon(AssetHelper.AssetPath + "UI/Stylesheets/Icons/PanelsToolbar/Panels.png")]
    public sealed class PanelsToolbar : OverlayToolbar
    {
        public const string toolbarId = "gtf-panel-toggles-toolbar";
    }
}
#endif
