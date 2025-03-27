using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    /// <summary> UITK component to display header styled foldout</summary>
    [UxmlElement]
    public partial class HeaderFoldout : Foldout
    {
        const string k_StylesheetPathFormat = "Packages/com.unity.render-pipelines.core/Editor/StyleSheets/HeaderFoldout{0}.uss";
        const string k_MainClass = "header-foldout";
        const string k_EnableClass = k_MainClass + "__enable";
        const string k_IconClass = k_MainClass + "__icon";
        const string k_LabelClass = k_MainClass + "__label";
        const string k_HelpButtonClass = k_MainClass + "__help-button";
        const string k_ContextButtonClass = k_MainClass + "__context-button";

        private string m_DocumentationURL;
        private Texture2D m_Icon;
        private Func<GenericMenu> m_ContextMenuGenerator;
        private VisualElement m_HelpButton;
        private VisualElement m_ContextMenuButton;
        private VisualElement m_IconElement;
        private Toggle m_Toggle;
        private Label m_Text;

        /// <summary>URL to use on documentation icon. If null, button don't show.</summary>
        public string documentationURL
        {
            get => m_DocumentationURL;
            set
            {
                if (m_DocumentationURL == value)
                    return;

                m_DocumentationURL = value;
                m_HelpButton?.SetEnabled(!string.IsNullOrEmpty(m_DocumentationURL));
            }
        }
        
        /// <summary>Context menu to show on click of the context button. If null, button don't show.</summary>
        public Func<GenericMenu> contextMenuGenerator //Use ImGUI for now
        {
            get => m_ContextMenuGenerator;
            set
            {
                if (m_ContextMenuGenerator == value)
                    return;

                m_ContextMenuGenerator = value;
                m_ContextMenuButton?.SetEnabled(m_ContextMenuGenerator != null);
            }
        }
        
        /// <summary>Optional icon image. If not set, no icon is shown.</summary>
        public Texture2D icon
        {
            get => m_Icon;
            set
            {
                if (m_Icon == value)
                    return;

                m_Icon = value;
                m_IconElement.style.backgroundImage = Background.FromTexture2D(m_Icon);
                m_IconElement.style.display = m_Icon != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        /// <summary>Property to get the enablement state</summary>
        public bool enabled
        {
            get => m_Toggle.value;
            set => m_Toggle.value = value;
        }
        
        /// <summary>Property to get the enablement visibility state</summary>
        public bool showEnableCheckbox
        {
            get => m_Toggle.style.display == DisplayStyle.Flex;
            set => m_Toggle.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Quick access to the enable toggle if one need to register events</summary>
        public Toggle enableToggle => m_Toggle;

        /// <summary>Property to get the title</summary>
        public new string text
        {
            get => m_Text.text;
            set => m_Text.text = value;
        }

        /// <summary>Constructor</summary>
        public HeaderFoldout() : base()
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(string.Format(k_StylesheetPathFormat, "")));
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(string.Format(k_StylesheetPathFormat, EditorGUIUtility.isProSkin ? "Dark" : "Light")));
            AddToClassList(k_MainClass);

            RegisterCallback<AttachToPanelEvent>(DelayedInit);

            var line = hierarchy[0][0]; //pass by herarchy to ignore content redirection
            
            m_IconElement = new Image()
            {
                style =
                {
                    display = DisplayStyle.None // hidden by default, will be enabled if icon is set
                }
            };
            m_IconElement.AddToClassList(k_IconClass);
            line.Add(m_IconElement);

            m_Toggle = new Toggle() 
            {
                value = true
            };
            m_Toggle.AddToClassList(k_EnableClass);
            m_Toggle.RegisterValueChangedCallback(HandleDisabling);
            m_Toggle.style.display = DisplayStyle.None; // hidden by default
            line.Add(m_Toggle);

            m_Text = new Label();
            m_Text.AddToClassList(k_LabelClass);
            line.Add(m_Text);

            m_HelpButton = new Button(Background.FromTexture2D(CoreEditorStyles.iconHelp), () => Help.BrowseURL(m_DocumentationURL));
            m_HelpButton.AddToClassList(k_HelpButtonClass);
            m_HelpButton.SetEnabled(!string.IsNullOrEmpty(m_DocumentationURL));
            line.Add(m_HelpButton);

            m_ContextMenuButton = new Button(Background.FromTexture2D(CoreEditorStyles.paneOptionsIcon), () => ShowMenu());
            m_ContextMenuButton.AddToClassList(k_ContextButtonClass);
            m_ContextMenuButton.SetEnabled(m_ContextMenuGenerator != null);
            line.Add(m_ContextMenuButton);
        }

        void DelayedInit(AttachToPanelEvent evt)
        {
            //Only show top line if previous item is not a HeaderFoldout to avoid bolder border
            var parent = hierarchy.parent;
            int posInParent = parent.hierarchy.IndexOf(this);
            if (posInParent == 0 || !parent[posInParent - 1].ClassListContains(k_MainClass))
                AddToClassList("first-in-collection");

            //fix to transfer label assigned in UXML from base label to new label
            if (!string.IsNullOrEmpty(base.text))
            {
                if (string.IsNullOrEmpty(m_Text.text))
                    m_Text.text = base.text;
                base.text = null;
            }
        }

        void ShowMenu()
        {
            var menu = m_ContextMenuGenerator.Invoke();
            menu.DropDown(new Rect(m_ContextMenuButton.worldBound.position + m_ContextMenuButton.worldBound.size.y * Vector2.up, Vector2.zero));
        }

        void HandleDisabling(ChangeEvent<bool> evt)
            => contentContainer.SetEnabled(evt.newValue);
    }
    
    /// <summary> UITK component to display header styled foldout. This variant have an enable checkbox.</summary>
    [Obsolete("Please directly use HeaderFoldout now #from(6000.2) (UnityUpgradable) -> HeaderFoldout", false)]
    public class HeaderToggleFoldout : HeaderFoldout
    {
        /// <summary>Constructor</summary>
        public HeaderToggleFoldout() : base()
            => showEnableCheckbox = true;
    }
}