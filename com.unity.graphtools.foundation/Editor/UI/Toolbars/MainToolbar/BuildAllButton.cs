#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Toolbar button to build the graph.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    public class BuildAllButton : MainToolbarButton
    {
        public const string id = "GTF/Main/Build All";

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildAllButton"/> class.
        /// </summary>
        public BuildAllButton()
        {
            name = "BuildAll";
            tooltip = L10n.Tr("Build All");
            icon = EditorGUIUtility.FindTexture(AssetHelper.AssetPath + "UI/Stylesheets/Icons/MainToolbar_Overlay/BuildAll.png");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            try
            {
                GraphTool?.Dispatch(new BuildAllEditorCommand());
            }
            catch (Exception e) // so the button doesn't get stuck
            {
                Debug.LogException(e);
            }
        }
    }
}
#endif
