using Unity.GraphToolsFoundation.Editor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Toolbar button to toggle the display of the blackboard.
    /// </summary>
    [EditorToolbarElement(id, typeof(ShaderGraphEditorWindow))]
    sealed class SGInspectorPanelToggle : PanelToggle
    {
        public const string id = "ShaderGraph/Overlay Windows/Inspector";

        /// <inheritdoc />
        protected override string WindowId => SGInspectorOverlay.k_OverlayID;

        /// <summary>
        /// Initializes a new instance of the <see cref="SGInspectorPanelToggle"/> class.
        /// </summary>
        public SGInspectorPanelToggle()
        {
            name = "Inspector";
            tooltip = L10n.Tr("Inspector");
            icon = EditorGUIUtility.isProSkin ? Resources.Load<Texture2D>("Icons/d_Inspector") : Resources.Load<Texture2D>("Icons/Inspector");
        }
    }
}
