using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    public sealed class ShaderGraphSaveAsButton : MainToolbarButton
    {
        public new const string id = "ShaderGraph/Main/SaveAs";

        public ShaderGraphSaveAsButton()
        {
            tooltip = L10n.Tr("Save Asset As");
            icon = EditorGUIUtility.isProSkin ? Resources.Load<Texture2D>("Icons/d_sg_saveAs_Icon") : Resources.Load<Texture2D>("Icons/sg_saveAs_Icon");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            if (GraphTool.ToolState.AssetModel is ShaderGraphAssetModel { IsSubGraph: true })
            {
                GraphAssetUtils.SaveAsSubgraphImplementation(GraphTool);
            }
            else
            {
                GraphAssetUtils.SaveAsGraphImplementation(GraphTool);
            }
        }
    }
}
