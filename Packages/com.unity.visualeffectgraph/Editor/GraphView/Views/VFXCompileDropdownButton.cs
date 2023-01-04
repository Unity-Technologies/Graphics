using System.IO;

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXCompileDropdownButton : DropDownButtonBase
    {
        readonly Toggle m_AutoCompileToggle;
        readonly Toggle m_AutoReinitToggle;
        readonly Slider m_ReinitPrewarmTime;
        readonly FloatField m_ReinitPrewarmTimeField;
        readonly Toggle m_RuntimeModeToggle;
        readonly Toggle m_ShaderDebugSymbolsToggle;
        readonly Toggle m_ShaderValidationToggle;
        readonly Button m_ResyncMaterial;

        public VFXCompileDropdownButton(VFXView vfxView)
            : base(
                vfxView,
                "VFXCompileDropdownPanel",
                "Compile",
                "compile-button",
                Path.Combine(VisualEffectGraphPackageInfo.assetPackagePath, "Editor/UIResources/VFX/compile.png"))
        {
            m_AutoCompileToggle = m_PopupContent.Q<Toggle>("autoCompile");
            m_AutoCompileToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleAutoCompile);

            m_AutoReinitToggle = m_PopupContent.Q<Toggle>("autoReinit");
            m_AutoReinitToggle.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                VFXViewWindow.GetWindowNoShow(vfxView).autoReinit = evt.newValue;
                m_ReinitPrewarmTime.SetEnabled(evt.newValue);
                m_ReinitPrewarmTimeField.SetEnabled(evt.newValue);
            });

            m_ReinitPrewarmTime = m_PopupContent.Q<Slider>("prewarmTime");
            m_ReinitPrewarmTime.lowValue = 0;
            m_ReinitPrewarmTime.highValue = VFXViewPreference.authoringPrewarmMaxTime;
            
            m_ReinitPrewarmTime.RegisterValueChangedCallback(evt =>
            {
                m_ReinitPrewarmTimeField.SetValueWithoutNotify(m_ReinitPrewarmTime.value);
                VFXViewWindow.GetWindowNoShow(vfxView).autoReinitPrewarmTime = evt.newValue;
            });

            m_ReinitPrewarmTimeField = m_PopupContent.Q<FloatField>("prewarmTimeField");
            m_ReinitPrewarmTimeField.RegisterValueChangedCallback(evt =>
            {
                float clampedValue = Mathf.Clamp(evt.newValue, 0, VFXViewPreference.authoringPrewarmMaxTime);
                m_ReinitPrewarmTime.SetValueWithoutNotify(clampedValue);
                m_ReinitPrewarmTimeField.SetValueWithoutNotify(clampedValue);
                VFXViewWindow.GetWindowNoShow(vfxView).autoReinitPrewarmTime = clampedValue;
            });

            m_RuntimeModeToggle = m_PopupContent.Q<Toggle>("runtimeMode");
            m_RuntimeModeToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleRuntimeMode);

            m_ShaderDebugSymbolsToggle = m_PopupContent.Q<Toggle>("shaderDebugSymbols");
            m_ShaderDebugSymbolsToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleShaderDebugSymbols);

            m_ShaderValidationToggle = m_PopupContent.Q<Toggle>("shaderValidation");
            m_ShaderValidationToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleShaderValidation);

            m_ResyncMaterial = m_PopupContent.Q<Button>("resyncMaterial");
            m_ResyncMaterial.clicked += OnResyncMaterial;
        }

        protected override Vector2 GetPopupSize() => new Vector2(250, 140);

        protected override void OnOpenPopup()
        {
            bool enableToggles = (!m_VFXView.controller.graph.GetResource()?.isSubgraph) ?? false; // for subgraph we disable auto reinit

            m_AutoCompileToggle.value = VFXViewWindow.GetWindowNoShow(m_VFXView).autoCompile;

            m_AutoReinitToggle.value = enableToggles ? VFXViewWindow.GetWindowNoShow(m_VFXView).autoReinit : false;
            m_AutoReinitToggle.SetEnabled(enableToggles);

            m_ReinitPrewarmTime.value = m_ReinitPrewarmTimeField.value = VFXViewWindow.GetWindowNoShow(m_VFXView).autoReinitPrewarmTime;
            m_ReinitPrewarmTime.SetEnabled(m_AutoReinitToggle.value);
            m_ReinitPrewarmTimeField.SetEnabled(m_AutoReinitToggle.value);

            m_RuntimeModeToggle.value = m_VFXView.GetIsRuntimeMode();
            m_RuntimeModeToggle.SetEnabled(enableToggles);

            m_ShaderDebugSymbolsToggle.value = enableToggles && (m_VFXView.GetForceShaderDebugSymbols() || VFXViewPreference.generateShadersWithDebugSymbols);
            m_ShaderDebugSymbolsToggle.SetEnabled(enableToggles && !VFXViewPreference.generateShadersWithDebugSymbols);

            m_ShaderValidationToggle.value = m_VFXView.GetShaderValidation();
            m_ShaderValidationToggle.SetEnabled(enableToggles);
        }

        protected override void OnMainButton()
        {
            m_VFXView.Compile();
        }

        void OnToggleAutoCompile(ChangeEvent<bool> evt)
        {
            VFXViewWindow.GetWindowNoShow(m_VFXView).autoCompile = evt.newValue;
        }

        void OnToggleRuntimeMode(ChangeEvent<bool> evt)
        {
            m_VFXView.ToggleRuntimeMode();
        }

        void OnToggleShaderDebugSymbols(ChangeEvent<bool> evt)
        {
            m_VFXView.ToggleForceShaderDebugSymbols();
        }

        void OnToggleShaderValidation(ChangeEvent<bool> evt)
        {
            m_VFXView.ToggleShaderValidationChanged();
        }

        void OnResyncMaterial()
        {
            m_VFXView.controller.graph.Invalidate(VFXModel.InvalidationCause.kMaterialChanged);
        }
    }
}
