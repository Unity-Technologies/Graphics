#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Toolbars;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Toolbar button to create a new graph.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    public class NewGraphButton : MainToolbarButton
    {
        public const string id = "GTF/Main/New Graph";

        /// <summary>
        /// Initializes a new instance of the <see cref="NewGraphButton"/> class.
        /// </summary>
        public NewGraphButton()
        {
            name = "NewGraph";
            tooltip = L10n.Tr("New Graph");
            icon = EditorGUIUtility.FindTexture(AssetHelper.AssetPath + "UI/Stylesheets/Icons/MainToolbar/CreateNew.png");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            GraphTool?.Dispatch(new UnloadGraphAssetCommand());
        }
    }
}
#endif
