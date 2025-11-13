using System;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Converter
{
    [UxmlElement]
    partial class RenderPipelineConverterVisualElementListFilter : VisualElement
    {
        const string k_Uxml = "Packages/com.unity.render-pipelines.core/Editor-PrivateShared/Tools/Converter/Window/RenderPipelineConverterVisualElementListFilter.uxml";
        const string k_Uss = "Packages/com.unity.render-pipelines.core/Editor-PrivateShared/Tools/Converter/Window/RenderPipelineConverterVisualElementListFilter.uss";

        static Lazy<VisualTreeAsset> s_VisualTreeAsset = new Lazy<VisualTreeAsset>(() => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_Uxml));
        static Lazy<StyleSheet> s_StyleSheet = new Lazy<StyleSheet>(() => AssetDatabase.LoadAssetAtPath<StyleSheet>(k_Uss));

        Button m_Pending;
        Button m_Warning;
        Button m_Error;
        Button m_Success;

        ConverterState m_State;

        public RenderPipelineConverterVisualElementListFilter()
        {
            var rootVisualElement = new VisualElement();
            s_VisualTreeAsset.Value.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(s_StyleSheet.Value);

            m_Pending = rootVisualElement.Q<Button>("pendingToggle");
            m_Warning = rootVisualElement.Q<Button>("warningToggle");
            m_Error = rootVisualElement.Q<Button>("errorToggle");
            m_Success = rootVisualElement.Q<Button>("successToggle");

            m_Pending.Q<Image>("icon").image = CoreEditorStyles.iconPending;
            m_Warning.Q<Image>("icon").image = CoreEditorStyles.iconWarn;
            m_Error.Q<Image>("icon").image = CoreEditorStyles.iconFail;
            m_Success.Q<Image>("icon").image = CoreEditorStyles.iconComplete;

            Add(rootVisualElement);
        }

        public Action onFilterChanged;

        void UpdateToggleVisualState(Button toggle, bool isActive)
        {
            if (isActive)
                toggle.AddToClassList("toggle-button-checked");
            else
                toggle.RemoveFromClassList("toggle-button-checked");
        }

        void Bind(DisplayFilter displayFilter, Button toggle)
        {
            toggle.RegisterCallback<ClickEvent>(evt => OnTogglePressed(displayFilter, toggle));

            UpdateToggleVisualState(toggle, (m_State.currentFilter & displayFilter) != 0);
        }

        void OnTogglePressed(DisplayFilter displayFilter, Button toggle)
        {
            if ((m_State.currentFilter & displayFilter) != 0)
            {
                m_State.currentFilter &= ~displayFilter;
                UpdateToggleVisualState(toggle, false);
            }
            else
            {
                m_State.currentFilter |= displayFilter;
                UpdateToggleVisualState(toggle, true);
            }

            onFilterChanged?.Invoke();
        }

        public void Bind(ConverterState state)
        {
            m_State = state;
            Bind(DisplayFilter.Pending, m_Pending);
            Bind(DisplayFilter.Warnings, m_Warning);
            Bind(DisplayFilter.Errors, m_Error);
            Bind(DisplayFilter.Success, m_Success);
        }

        public void Update(ConverterState state)
        {
            m_Pending.Q<Label>("label").text = $"{state.pending}";
            m_Warning.Q<Label>("label").text = $"{state.warnings}";
            m_Error.Q<Label>("label").text = $"{state.errors}";
            m_Success.Q<Label>("label").text = $"{state.success}";
        }
    }
}
