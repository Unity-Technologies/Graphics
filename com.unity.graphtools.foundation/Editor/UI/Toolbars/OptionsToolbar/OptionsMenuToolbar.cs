#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Overlays;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The toolbar for the option menu.
    /// </summary>
    [Overlay(typeof(GraphViewEditorWindow), toolbarId, "Options", true,
        defaultDockZone = DockZone.TopToolbar, defaultDockPosition = DockPosition.Bottom,
        defaultDockIndex = 1000, defaultLayout = Layout.HorizontalToolbar)]
    [Icon(AssetHelper.AssetPath + "UI/Stylesheets/Icons/OptionsToolbar/Options.png")]
    public sealed class OptionsMenuToolbar : OverlayToolbar
    {
        public const string toolbarId = "gtf-options";
    }
}
#endif
