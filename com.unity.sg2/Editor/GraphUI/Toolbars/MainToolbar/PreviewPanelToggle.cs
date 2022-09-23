using Unity.GraphToolsFoundation.Editor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Toolbar button to toggle the display of the blackboard.
    /// </summary>
    [EditorToolbarElement(id, typeof(ShaderGraphEditorWindow))]
    sealed class PreviewPanelToggle : PanelToggle
    {
        public const string id = "ShaderGraph/Overlay Windows/Preview";

        /// <inheritdoc />
        protected override string WindowId => PreviewOverlay.k_OverlayID;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewPanelToggle"/> class.
        /// </summary>
        public PreviewPanelToggle()
        {
            name = "Preview";
            tooltip = L10n.Tr("Preview");
            icon = EditorGUIUtility.isProSkin ? Resources.Load<Texture2D>("Icons/d_Preview") : Resources.Load<Texture2D>("Icons/Preview");
        }
    }
}
