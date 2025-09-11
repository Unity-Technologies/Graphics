using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Universal
{
    internal class RenderPipelineConverterItemVisualElement : VisualElement
    {
        const string k_Uxml = "Packages/com.unity.render-pipelines.universal/Editor/Converter/converter_widget_item.uxml";

        static Lazy<VisualTreeAsset> s_VisualTreeAsset = new Lazy<VisualTreeAsset>(() => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_Uxml));

        VisualElement m_RootVisualElement;
        Toggle m_ItemSelectedToggle;
        ConverterItemState m_ConverterItemState;

        public Action itemSelectionChanged;

        public RenderPipelineConverterItemVisualElement()
        {
            m_RootVisualElement = new VisualElement();
            s_VisualTreeAsset.Value.CloneTree(m_RootVisualElement);

            m_ItemSelectedToggle = m_RootVisualElement.Q<Toggle>("converterItemActive");
            m_ItemSelectedToggle.RegisterCallback<ClickEvent>(evt =>
            {
                if (m_ConverterItemState != null)
                {
                    m_ConverterItemState.isActive = !m_ConverterItemState.isActive;
                    itemSelectionChanged?.Invoke();
                }
            });

            Add(m_RootVisualElement);
        }

        public void Bind(ConverterItemState itemState)
        {
            m_ConverterItemState = itemState;

            m_ItemSelectedToggle.SetValueWithoutNotify(m_ConverterItemState.isActive);

            var desc = m_ConverterItemState.descriptor;
            m_RootVisualElement.Q<Label>("converterItemName").text = desc.name;
            m_RootVisualElement.Q<Label>("converterItemPath").text = desc.info;

            if (!string.IsNullOrEmpty(desc.helpLink))
            {
                m_RootVisualElement.Q<Image>("converterItemHelpIcon").image = CoreEditorStyles.iconHelp;
                m_RootVisualElement.Q<Image>("converterItemHelpIcon").tooltip = desc.helpLink;
            }

            // Changing the icon here depending on the status.
            Texture2D icon = null;
            Status status = m_ConverterItemState.status;
            switch (status)
            {
                case Status.Pending:
                    icon = CoreEditorStyles.iconPending;
                    break;
                case Status.Error:
                    icon = CoreEditorStyles.iconFail;
                    break;
                case Status.Warning:
                    icon = CoreEditorStyles.iconWarn;
                    break;
                case Status.Success:
                    icon = CoreEditorStyles.iconComplete;
                    break;
            }

            m_RootVisualElement.Q<Image>("converterItemStatusIcon").image = icon;
            m_RootVisualElement.Q<Image>("converterItemStatusIcon").tooltip = m_ConverterItemState.message;
        }

    }
}
