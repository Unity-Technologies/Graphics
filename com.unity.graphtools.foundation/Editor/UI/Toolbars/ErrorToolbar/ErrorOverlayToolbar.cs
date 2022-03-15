#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Overlays;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The toolbar that displays the error count and buttons to navigate the errors.
    /// </summary>
    [Overlay(typeof(GraphViewEditorWindow), toolbarId, "Error Notifications", ussName = "ErrorNotifications",
        defaultDisplay = true, defaultDockZone = DockZone.BottomToolbar, defaultDockPosition = DockPosition.Bottom,
        defaultLayout = Layout.HorizontalToolbar)]
    [Icon(AssetHelper.AssetPath + "UI/Stylesheets/Icons/ErrorToolbar/ErrorNotification.png")]
    public sealed class ErrorOverlayToolbar : OverlayToolbar
    {
        public const string toolbarId = "gtf-error-notifications";

        /// <inheritdoc />
        protected override Layout supportedLayouts => Layout.HorizontalToolbar;

        public ErrorOverlayToolbar()
        {
            AddStylesheet("ErrorOverlayToolbar.uss");
        }
    }
}
#endif
