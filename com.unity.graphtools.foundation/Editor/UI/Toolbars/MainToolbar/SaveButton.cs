#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Toolbars;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Toolbar button to save all graphs.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    public class SaveButton : MainToolbarButton
    {
        public const string id = "GTF/Main/Save";

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveButton"/> class.
        /// </summary>
        public SaveButton()
        {
            name = "Save";
            tooltip = L10n.Tr("Save");
            icon = EditorGUIUtility.FindTexture(AssetHelper.AssetPath + "UI/Stylesheets/Icons/MainToolbar_Overlay/Save.png");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            var graphEditorWindow = containerWindow as GraphViewEditorWindow;
            if (graphEditorWindow != null)
            {
                var graphAsset = graphEditorWindow.GraphView.GraphViewModel.GraphModelState.GraphModel.Asset;
                if (graphAsset is ISerializedGraphAsset serializedGraphAsset)
                {
                    serializedGraphAsset.Save();
                }
                else
                {
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}
#endif
