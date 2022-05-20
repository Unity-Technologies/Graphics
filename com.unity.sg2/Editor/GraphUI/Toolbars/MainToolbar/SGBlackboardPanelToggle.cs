using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.Toolbars;
using UnityEngine;

#if UNITY_2022_2_OR_NEWER
namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Toolbar button to toggle the display of the blackboard.
    /// </summary>
    [EditorToolbarElement(id, typeof(ShaderGraphEditorWindow))]
    sealed class SGBlackboardPanelToggle : PanelToggle
    {
        public const string id = "ShaderGraph/Overlay Windows/Blackboard";

        /// <inheritdoc />
        protected override string WindowId => SGBlackboardOverlay.k_OverlayID;

        /// <summary>
        /// Initializes a new instance of the <see cref="SGBlackboardPanelToggle"/> class.
        /// </summary>
        public SGBlackboardPanelToggle()
        {
            name = "Blackboard";
            tooltip = L10n.Tr("Blackboard");
            icon = EditorGUIUtility.isProSkin ? Resources.Load<Texture2D>("Icons/Blackboard") : Resources.Load<Texture2D>("Icons/d_Blackboard");
        }
    }
}
#endif
