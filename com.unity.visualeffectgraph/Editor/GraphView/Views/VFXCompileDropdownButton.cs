using System.IO;

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXCompileDropdownButton : DropDownButtonBase
    {
        readonly Toggle m_AutoCompileToggle;
        readonly Toggle m_RuntimeModeToggle;
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

            m_RuntimeModeToggle = m_PopupContent.Q<Toggle>("runtimeMode");
            m_RuntimeModeToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleRuntimeMode);

            m_ShaderValidationToggle = m_PopupContent.Q<Toggle>("shaderValidation");
            m_ShaderValidationToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleShaderValidation);

            m_ResyncMaterial = m_PopupContent.Q<Button>("resyncMaterial");
            m_ResyncMaterial.clicked += OnResyncMaterial;
        }

        protected override Vector2 GetPopupSize() => new Vector2(150, 68);

        protected override void OnOpenPopup()
        {
            m_AutoCompileToggle.value = VFXViewWindow.GetWindow(m_VFXView).autoCompile;
            m_RuntimeModeToggle.value = m_VFXView.GetIsRuntimeMode();
            m_ShaderValidationToggle.value = m_VFXView.GetShaderValidation();
        }

        protected override void OnMainButton()
        {
            m_VFXView.Compile();
        }

        void OnToggleAutoCompile(ChangeEvent<bool> evt)
        {
            VFXViewWindow.GetWindow(m_VFXView).autoCompile = evt.newValue;
        }

        void OnToggleRuntimeMode(ChangeEvent<bool> evt)
        {
            m_VFXView.ToggleRuntimeMode();
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
