using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>Class containing style definition</summary>
    public static class CoreEditorStyles
    {
        /// <summary>Style for a small checkbox</summary>
        public static readonly GUIStyle smallTickbox;
        /// <summary>Style for a small checkbox in mixed state</summary>
        public static readonly GUIStyle smallMixedTickbox;
        /// <summary>Style for a minilabel button</summary>
        public static readonly GUIStyle miniLabelButton;

        static readonly Texture2D paneOptionsIconDark;
        static readonly Texture2D paneOptionsIconLight;

        /// <summary> PaneOption icon </summary>
        public static Texture2D paneOptionsIcon { get { return EditorGUIUtility.isProSkin ? paneOptionsIconDark : paneOptionsIconLight; } }

        /// <summary>Context Menu button icon</summary>
        public static readonly GUIContent contextMenuIcon;
        /// <summary>Context Menu button style</summary>
        public static readonly GUIStyle contextMenuStyle;

        static CoreEditorStyles()
        {
            smallTickbox = new GUIStyle("ShurikenToggle");
            smallMixedTickbox = new GUIStyle("ShurikenToggleMixed");

            var transparentTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            transparentTexture.SetPixel(0, 0, Color.clear);
            transparentTexture.Apply();

            miniLabelButton = new GUIStyle(EditorStyles.miniLabel);
            miniLabelButton.normal = new GUIStyleState
            {
                background = transparentTexture,
                scaledBackgrounds = null,
                textColor = Color.grey
            };
            var activeState = new GUIStyleState
            {
                background = transparentTexture,
                scaledBackgrounds = null,
                textColor = Color.white
            };
            miniLabelButton.active = activeState;
            miniLabelButton.onNormal = activeState;
            miniLabelButton.onActive = activeState;

            paneOptionsIconDark = (Texture2D)EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/pane options.png");
            paneOptionsIconLight = (Texture2D)EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/pane options.png");

            var contextTooltip = ""; // To be defined (see with UX)
            contextMenuIcon = new GUIContent(paneOptionsIcon, contextTooltip);
            contextMenuStyle = new GUIStyle("IconButton");
        }
    }
}
