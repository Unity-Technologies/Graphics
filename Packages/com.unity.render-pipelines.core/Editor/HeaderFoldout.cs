using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    /// <summary> UITK component to display header styled foldout</summary>
    public class HeaderFoldout : Foldout
    {
        const string k_StylesheetPathFormat = "Packages/com.unity.render-pipelines.core/Editor/StyleSheets/HeaderFoldout{0}.uss";
        const string k_Class = "header-foldout";

        private string m_DocumentationURL;
        private Func<GenericMenu> m_ContextMenuGenerator;
        private VisualElement m_HelpButton;
        private VisualElement m_ContextMenuButton;

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
        
        /// <summary>Context menu to show on clic of the context button. If null, button don't show.</summary>
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

        /// <summary>Constructor</summary>
        public HeaderFoldout() : base()
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(string.Format(k_StylesheetPathFormat, "")));
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(string.Format(k_StylesheetPathFormat, EditorGUIUtility.isProSkin ? "Dark" : "Light")));
            AddToClassList(k_Class);

            RegisterCallback<AttachToPanelEvent>(DelayedInit);

            var line = hierarchy[0][0]; //pass by herarchy to ignore content redirection
            
            m_HelpButton = new Button(Background.FromTexture2D(CoreEditorStyles.iconHelp), () => Help.BrowseURL(m_DocumentationURL));
            m_HelpButton.SetEnabled(!string.IsNullOrEmpty(m_DocumentationURL));
            line.Add(m_HelpButton);

            m_ContextMenuButton = new Button(Background.FromTexture2D(CoreEditorStyles.paneOptionsIcon), () => ShowMenu());
            m_ContextMenuButton.SetEnabled(m_ContextMenuGenerator != null);
            line.Add(m_ContextMenuButton);
        }

        void DelayedInit(AttachToPanelEvent evt)
        {
            //Only show top line if previous item is not a HeaderFoldout to avoid bolder border
            bool shouldShowTopLine = true;
            var parent = hierarchy.parent;
            int posInParent = parent.hierarchy.IndexOf(this);
            if (posInParent > 0 && parent[posInParent - 1].ClassListContains(k_Class))
                shouldShowTopLine = false;

            style.borderTopWidth = shouldShowTopLine ? 1 : 0;
        }

        void ShowMenu()
        {
            var menu = m_ContextMenuGenerator.Invoke();
            menu.DropDown(new Rect(m_ContextMenuButton.worldBound.position + m_ContextMenuButton.worldBound.size.y * Vector2.up, Vector2.zero));
        }
    }
    
    /// <summary> UITK component to display header styled foldout. This variant have an enable checkbox.</summary>
    public class HeaderToggleFoldout : HeaderFoldout
    {
        private Toggle m_Toggle;

        /// <summary>Property to get the enablement state</summary>
        public bool enabled
        {
            get => m_Toggle.value;
            set => m_Toggle.value = value;
        }

        /// <summary>Quick access to the enable toggle if one need to register events</summary>
        public Toggle enableToggle => m_Toggle;
        
        /// <summary>Constructor</summary>
        public HeaderToggleFoldout() : base()
        {
            var line = hierarchy[0][0]; //pass by herarchy to ignore content redirection
            m_Toggle = new Toggle() 
            { 
                value = true,
                name = "enable-checkbox",
            };

            //Need to delay insertion as foldout will be constructed after and we need to squeeze rigth after
            RegisterCallbackOnce<AttachToPanelEvent>(evt => line.Insert(1, m_Toggle));

            m_Toggle.RegisterValueChangedCallback(HandleDisabling);
        }

        void HandleDisabling(ChangeEvent<bool> evt)
            => contentContainer.SetEnabled(evt.newValue);
    }
}