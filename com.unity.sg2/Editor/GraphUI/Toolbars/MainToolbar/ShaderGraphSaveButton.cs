using Unity.GraphToolsFoundation.Editor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    sealed class ShaderGraphSaveButton : MainToolbarButton
    {
        public const string id = "ShaderGraph/Main/Save";

        public ShaderGraphSaveButton()
        {
            tooltip = L10n.Tr("Save Asset");
            icon = EditorGUIUtility.isProSkin ? Resources.Load<Texture2D>("Icons/d_sg_save_Icon") : Resources.Load<Texture2D>("Icons/sg_save_Icon");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            GraphAssetUtils.SaveOpenGraphAsset(GraphTool);
        }
    }
}
