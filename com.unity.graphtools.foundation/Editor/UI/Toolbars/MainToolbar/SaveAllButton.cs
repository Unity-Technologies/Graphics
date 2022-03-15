#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Toolbars;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Toolbar button to save all graphs.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    public class SaveAllButton : MainToolbarButton
    {
        public const string id = "GTF/Main/Save All";

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveAllButton"/> class.
        /// </summary>
        public SaveAllButton()
        {
            name = "SaveAll";
            tooltip = L10n.Tr("Save All");
            icon = EditorGUIUtility.FindTexture(AssetHelper.AssetPath + "UI/Stylesheets/Icons/MainToolbar_Overlay/Save.png");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
