#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Toolbars;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Toolbar element to display the option menu built by <see cref="GraphView.BuildOptionMenu"/>.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    sealed class OptionDropDownMenu : EditorToolbarDropdown, IAccessContainerWindow
    {
        public const string id = "GTF/Main/Options";

        /// <inheritdoc />
        public EditorWindow containerWindow { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionDropDownMenu"/> class.
        /// </summary>
        public OptionDropDownMenu()
        {
            name = "Options";
            tooltip = L10n.Tr("Options");
            clicked += OnClick;
            icon = EditorGUIUtility.FindTexture(AssetHelper.AssetPath + "UI/Stylesheets/Icons/OptionsToolbar/Options.png");
        }

        void OnClick()
        {
            var graphViewWindow = containerWindow as GraphViewEditorWindow;

            if (graphViewWindow == null)
                return;

            GenericMenu menu = new GenericMenu();
            graphViewWindow.GraphView?.BuildOptionMenu(menu);
            menu.ShowAsContext();
        }
    }
}
#endif
