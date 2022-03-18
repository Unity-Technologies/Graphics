using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    public sealed class ShaderGraphShowInProjectButton : MainToolbarButton
    {
        public new const string id = "ShaderGraph/Main/ShowInProject";

        public ShaderGraphShowInProjectButton()
        {
            tooltip = L10n.Tr("Show In Project");
            icon = EditorGUIUtility.isProSkin ? Resources.Load<Texture2D>("Icons/d_sg_showInProject_Icon") : Resources.Load<Texture2D>("Icons/sg_showInProject_Icon");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            // If no currently opened graph, early out
            if (GraphTool.ToolState.AssetModel == null)
                return;

            var path = GraphTool.ToolState.CurrentGraph.GetGraphAssetModelPath();
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            EditorGUIUtility.PingObject(asset);
        }
    }
}
