using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    public static class CoreEditorStyles
    {
        public static readonly GUIStyle smallTickbox;
        public static readonly GUIStyle miniLabelButton;

        public static readonly Texture2D paneOptionsIconDark;
        public static readonly Texture2D paneOptionsIconLight;

        static CoreEditorStyles()
        {
            smallTickbox = new GUIStyle("ShurikenCheckMark");

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
        }
    }
}
