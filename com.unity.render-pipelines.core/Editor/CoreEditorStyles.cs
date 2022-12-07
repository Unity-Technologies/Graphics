using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering
{
    /// <summary>Class containing constants</summary>
    public static class CoreEditorConstants
    {
        /// <summary>Speed of additional properties highlight.</summary>
        public static readonly float additionalPropertiesHightLightSpeed = 0.3f;

        /// <summary>Standard UI spacing</summary>
        public static float standardHorizontalSpacing => 5f;
    }

    /// <summary>Class containing style definition</summary>
    public static class CoreEditorStyles
    {
        #region Styles

        static System.Lazy<GUIStyle> m_SmallTickbox = new(() => new GUIStyle("ShurikenToggle"));
        /// <summary>Style for a small checkbox</summary>
        public static GUIStyle smallTickbox => m_SmallTickbox.Value;

        static System.Lazy<GUIStyle> m_SmallMixedTickbox = new(() => new GUIStyle("ShurikenToggleMixed"));
        /// <summary>Style for a small checkbox in mixed state</summary>
        public static GUIStyle smallMixedTickbox => m_SmallMixedTickbox.Value;

        static GUIStyle m_MiniLabelButton;
        /// <summary>Style for a minilabel button</summary>
        public static GUIStyle miniLabelButton
        {
            get
            {
                if (m_MiniLabelButton == null)
                {
                    m_MiniLabelButton = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = new GUIStyleState
                        {
                            background = m_TransparentTexture,
                            scaledBackgrounds = null,
                            textColor = Color.grey
                        }
                    };
                    var activeState = new GUIStyleState
                    {
                        background = m_TransparentTexture,
                        scaledBackgrounds = null,
                        textColor = Color.white
                    };
                    m_MiniLabelButton.active = activeState;
                    m_MiniLabelButton.onNormal = activeState;
                    m_MiniLabelButton.onActive = activeState;
                    return m_MiniLabelButton;
                }

                return m_MiniLabelButton;
            }
        }

        static System.Lazy<GUIStyle> m_ContextMenuStyle = new(() => new GUIStyle("IconButton"));
        /// <summary>Context Menu button style</summary>
        public static GUIStyle contextMenuStyle => m_ContextMenuStyle.Value;

        static System.Lazy<GUIStyle> m_AdditionalPropertiesHighlightStyle = new(() => new GUIStyle { normal = { background = Texture2D.whiteTexture } });
        /// <summary>Style of a additional properties highlighted background.</summary>
        public static GUIStyle additionalPropertiesHighlightStyle => m_AdditionalPropertiesHighlightStyle.Value;

        /// <summary>Help icon style</summary>
        public static GUIStyle iconHelpStyle => GUI.skin.FindStyle("IconButton") ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("IconButton");

        static System.Lazy<GUIStyle> m_SectionHeaderStyle = new(() => new GUIStyle(EditorStyles.largeLabel) { richText = true, fontSize = 18, fixedHeight = 42 });
        /// <summary>Style of Section Headers.</summary>
        public static GUIStyle sectionHeaderStyle => m_SectionHeaderStyle.Value;

        static System.Lazy<GUIStyle> m_SubSectionHeaderStyle = new(() => new GUIStyle(EditorStyles.boldLabel));
        /// <summary>Style of Sub-Section Headers.</summary>
        public static GUIStyle subSectionHeaderStyle => m_SubSectionHeaderStyle.Value;


        static System.Lazy<GUIStyle> m_HelpBox = new(() =>
        {
            var style = new GUIStyle() { imagePosition = ImagePosition.ImageLeft, fontSize = 10, wordWrap = true };
            style.normal.textColor = EditorStyles.helpBox.normal.textColor;
            return style;
        });
        internal static GUIStyle helpBox => m_HelpBox.Value;

        #endregion

        #region Textures 2D

        static Texture2D m_TransparentTexture;

        /// <summary><see cref="Texture2D"/> 1x1 pixel with red color</summary>
        public static readonly Texture2D redTexture;
        /// <summary><see cref="Texture2D"/> 1x1 pixel with green color</summary>
        public static readonly Texture2D greenTexture;
        /// <summary><see cref="Texture2D"/> 1x1 pixel with blue color</summary>
        public static readonly Texture2D blueTexture;

        /// <summary> PaneOption icon for dark skin</summary>
        static readonly Texture2D paneOptionsIconDark;
        /// <summary> PaneOption icon for light skin</summary>
        static readonly Texture2D paneOptionsIconLight;

        /// <summary> PaneOption icon </summary>
        public static Texture2D paneOptionsIcon => EditorGUIUtility.isProSkin ? paneOptionsIconDark : paneOptionsIconLight;

        /// <summary> Warning icon </summary>
        public static readonly Texture2D iconWarn;
        /// <summary> Help icon </summary>
        public static readonly Texture2D iconHelp;
        /// <summary> Fail icon </summary>
        public static readonly Texture2D iconFail;
        /// <summary> Success icon </summary>
        public static readonly Texture2D iconSuccess;
        /// <summary> Complete icon </summary>
        public static readonly Texture2D iconComplete;
        /// <summary> Pending icon </summary>
        public static readonly Texture2D iconPending;

        /// <summary>RenderPipeline Global Settings icon</summary>
        public static readonly Texture2D globalSettingsIcon;

        /// <summary>
        /// Gets the icon that describes the <see cref="MessageType"/>
        /// </summary>
        /// <param name="messageType">The <see cref="MessageType"/> to obtain the icon from</param>
        /// <returns>a <see cref="Texture2D"/> with the icon for the <see cref="MessageType"/></returns>
        internal static Texture2D GetMessageTypeIcon(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.None:
                    return null;
                case MessageType.Info:
                    return iconHelp;
                case MessageType.Warning:
                    return iconWarn;
                case MessageType.Error:
                    return iconFail;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
        }

        #endregion

        #region Colors

        static readonly Color m_LightThemeBackgroundColor;
        static readonly Color m_LightThemeBackgroundHighlightColor;
        static readonly Color m_DarkThemeBackgroundColor;
        static readonly Color m_DarkThemeBackgroundHighlightColor;

        /// <summary>Regular background color.</summary>
        public static Color backgroundColor => EditorGUIUtility.isProSkin ? m_DarkThemeBackgroundColor : m_LightThemeBackgroundColor;

        /// <summary>Hightlited background color.</summary>
        public static Color backgroundHighlightColor => EditorGUIUtility.isProSkin ? m_DarkThemeBackgroundHighlightColor : m_LightThemeBackgroundHighlightColor;

        #endregion

        #region GUIContents

        /// <summary>Context Menu button icon</summary>
        public static readonly GUIContent contextMenuIcon;

        /// <summary>Reset Content</summary>
        public static readonly GUIContent resetButtonLabel = EditorGUIUtility.TrTextContent("Reset");

        /// <summary>Reset All content</summary>
        public static readonly GUIContent resetAllButtonLabel = EditorGUIUtility.TrTextContent("Reset All");

        /// <summary>
        /// Empty space content in case that you want to keep the indentation but have nothing to write
        /// </summary>
        public static readonly GUIContent empty = EditorGUIUtility.TrTextContent(" ");

        #endregion

        static CoreEditorStyles()
        {
            m_TransparentTexture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None)
            {
                name = "transparent"
            };
            m_TransparentTexture.SetPixel(0, 0, Color.clear);
            m_TransparentTexture.Apply();

            paneOptionsIconDark = CoreEditorUtils.LoadIcon("Builtin Skins/DarkSkin/Images", "pane options", ".png");
            paneOptionsIconDark.name = "pane options dark skin";
            paneOptionsIconLight = CoreEditorUtils.LoadIcon("Builtin Skins/LightSkin/Images", "pane options", ".png");
            paneOptionsIconLight.name = "pane options light skin";

            m_LightThemeBackgroundColor = new Color(0.7843138f, 0.7843138f, 0.7843138f, 1.0f);
            m_LightThemeBackgroundHighlightColor = new Color32(174, 174, 174, 255);
            m_DarkThemeBackgroundColor = new Color(0.2196079f, 0.2196079f, 0.2196079f, 1.0f);
            m_DarkThemeBackgroundHighlightColor = new Color32(77, 77, 77, 255);

            const string contextTooltip = ""; // To be defined (see with UX)
            contextMenuIcon = new GUIContent(paneOptionsIcon, contextTooltip);

            redTexture = CoreEditorUtils.CreateColoredTexture2D(Color.red, "Red 1x1");
            greenTexture = CoreEditorUtils.CreateColoredTexture2D(Color.green, "Green 1x1");
            blueTexture = CoreEditorUtils.CreateColoredTexture2D(Color.blue, "Blue 1x1");

            iconHelp = CoreEditorUtils.FindTexture("_Help");
            iconWarn = CoreEditorUtils.LoadIcon("icons", "console.warnicon", ".png");
            iconFail = CoreEditorUtils.LoadIcon("icons", "console.erroricon", ".png");
            iconSuccess = EditorGUIUtility.FindTexture("TestPassed");
            iconComplete = CoreEditorUtils.LoadIcon("icons", "GreenCheckmark", ".png");
            iconPending = EditorGUIUtility.FindTexture("Toolbar Minus");

            globalSettingsIcon = EditorGUIUtility.FindTexture("ScriptableObject Icon");

            // Make sure that textures are unloaded on domain reloads.
            void OnBeforeAssemblyReload()
            {
                Object.DestroyImmediate(redTexture);
                Object.DestroyImmediate(greenTexture);
                Object.DestroyImmediate(blueTexture);
                Object.DestroyImmediate(m_TransparentTexture);
                AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            }

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }
    }
}
