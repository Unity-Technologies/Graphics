#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Toolbars;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Toolbar button to toggle the display of the blackboard.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    sealed class BlackboardPanelToggle : PanelToggle
    {
        public const string id = "GTF/Overlay Windows/Blackboard";

        /// <inheritdoc />
        protected override string WindowId => BlackboardOverlay.idValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardPanelToggle"/> class.
        /// </summary>
        public BlackboardPanelToggle()
        {
            name = "Blackboard";
            tooltip = L10n.Tr("Blackboard");
            icon = EditorGUIUtility.FindTexture(AssetHelper.AssetPath + "UI/Stylesheets/Icons/PanelsToolbar/Blackboard.png");
        }
    }
}
#endif
