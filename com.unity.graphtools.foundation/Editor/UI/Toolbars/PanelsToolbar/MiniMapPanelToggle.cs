#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Toolbars;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Toolbar button to toggle the display of the minimap.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    sealed class MiniMapPanelToggle : PanelToggle
    {
        public const string id = "GTF/Overlay Windows/MiniMap";

        /// <inheritdoc />
        protected override string WindowId => MiniMapOverlay.idValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniMapPanelToggle"/> class.
        /// </summary>
        public MiniMapPanelToggle()
        {
            name = "MiniMap";
            tooltip = L10n.Tr("MiniMap");
            icon = EditorGUIUtility.FindTexture(AssetHelper.AssetPath + "UI/Stylesheets/Icons/PanelsToolbar/MiniMap.png");
        }
    }
}
#endif
