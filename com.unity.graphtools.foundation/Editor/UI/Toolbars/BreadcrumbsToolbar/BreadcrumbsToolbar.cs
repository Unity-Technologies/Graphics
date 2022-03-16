#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Overlays;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The toolbar that displays the breadcrumbs.
    /// </summary>
    [Overlay(typeof(GraphViewEditorWindow), toolbarId, "Breadcrumbs", true,
        defaultDockZone = DockZone.TopToolbar, defaultDockPosition = DockPosition.Top,
        defaultDockIndex = 1000, defaultLayout = Layout.HorizontalToolbar)]
    [Icon(AssetHelper.AssetPath + "UI/Stylesheets/Icons/BreadcrumbsToolbar/Breadcrumb.png")]
    public sealed class BreadcrumbsToolbar : OverlayToolbar
    {
        public const string toolbarId = "gtf-breadcrumbs";

        /// <inheritdoc />
        protected override Layout supportedLayouts => Layout.HorizontalToolbar;
    }
}
#endif
