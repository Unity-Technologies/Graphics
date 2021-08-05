using UnityEditor.Experimental;

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXCompileDropdownButton : DropDownButtonBase
    {
        readonly VFXView m_VFXView;
        readonly Toggle m_AutoCompileToggle;
        readonly Toggle m_RuntimeModeToggle;
        readonly Toggle m_shaderValidationToggle;

        public VFXCompileDropdownButton(VFXView vfxView) : base("VFXCompileDropdownPanel", "Compile", 2, EditorResources.iconsPath + "PlayButton.png")
        {
            m_VFXView = vfxView;

            m_AutoCompileToggle = m_PopupContent.Q<Toggle>("autoCompile");
            m_AutoCompileToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleAutoCompile);

            m_RuntimeModeToggle = m_PopupContent.Q<Toggle>("runtimeMode");
            m_RuntimeModeToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleRuntimeMode);

            m_shaderValidationToggle = m_PopupContent.Q<Toggle>("shaderValidation");
            m_shaderValidationToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleShaderValidation);
        }

        
        protected override Vector2 GetPopupPosition() => this.m_VFXView.ViewToScreenPosition(worldBound.position);
        protected override Vector2 GetPopupSize() => new Vector2(150, 70);

        protected override void OnOpenPopup()
        {
            m_AutoCompileToggle.value = VFXViewWindow.currentWindow.autoCompile;
            m_RuntimeModeToggle.value = m_VFXView.GetIsRuntimeMode();
            m_shaderValidationToggle.value = m_VFXView.GetShaderValidation();
        }

        protected override void OnMainButton()
        {
            m_VFXView.Compile();
        }

        void OnToggleAutoCompile(ChangeEvent<bool> evt)
        {
            VFXViewWindow.currentWindow.autoCompile = evt.newValue;
        }

        void OnToggleRuntimeMode(ChangeEvent<bool> evt)
        {
            m_VFXView.ToggleRuntimeMode();
        }

        void OnToggleShaderValidation(ChangeEvent<bool> evt)
        {
            m_VFXView.ToggleShaderValidationChanged();
        }
    }
}
