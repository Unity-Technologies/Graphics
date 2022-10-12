using System;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    sealed class ShaderGraphSaveAsButton : MainToolbarButton
    {
        public const string id = "ShaderGraph/Main/SaveAs";

        public ShaderGraphSaveAsButton()
        {
            tooltip = L10n.Tr("Save Asset As");
            icon = EditorGUIUtility.isProSkin ? Resources.Load<Texture2D>("Icons/d_sg_saveAs_Icon") : Resources.Load<Texture2D>("Icons/sg_saveAs_Icon");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            GraphAssetUtils.SaveOpenGraphAssetAs(GraphTool);
        }
    }
}
