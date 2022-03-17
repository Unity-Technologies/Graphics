#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Toolbars;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Toolbar button to toggle the display of the inspector.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    sealed class InspectorPanelToggle : PanelToggle
    {
        public const string id = "GTF/Overlay Windows/Inspector";

        /// <inheritdoc />
        protected override string WindowId => ModelInspectorOverlay.idValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="InspectorPanelToggle"/> class.
        /// </summary>
        public InspectorPanelToggle()
        {
            name = "Inspector";
            tooltip = L10n.Tr("Inspector");
            icon = EditorGUIUtility.FindTexture(AssetHelper.AssetPath + "UI/Stylesheets/Icons/PanelsToolbar/Inspector.png");
        }
    }
}
#endif
