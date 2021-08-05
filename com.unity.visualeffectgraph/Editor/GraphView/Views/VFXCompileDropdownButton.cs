using UnityEditor.Experimental;

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXCompileDropdownButton : DropDownButtonBase
    {
        private readonly VFXView m_VFXView;

        public VFXCompileDropdownButton(VFXView vfxView) : base("VFXCompileDropdownPanel", "Compile", 2, EditorResources.iconsPath + "PlayButton.png")
        {
            m_VFXView = vfxView;

            var autoCompileToggle = m_PopupContent.Q<Toggle>("autoCompile");
            autoCompileToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleAutoCompile);

            var runtimeModeToggle = m_PopupContent.Q<Toggle>("runtimeMode");
            runtimeModeToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleRuntimeMode);

            var shaderValidationToggle = m_PopupContent.Q<Toggle>("shaderValidation");
            shaderValidationToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleShaderValidation);
        }

        
        protected override Vector2 GetPopupPosition() => this.m_VFXView.ViewToScreenPosition(worldBound.position);
        protected override Vector2 GetPopupSize() => new Vector2(150, 70);

        protected override void OnMainButton()
        {
            m_VFXView.Compile();
        }

        void OnToggleAutoCompile(ChangeEvent<bool> evt)
        {
            VFXViewWindow.currentWindow.autoCompile = !VFXViewWindow.currentWindow.autoCompile;
        }

        void OnToggleRuntimeMode(ChangeEvent<bool> evt)
        {
            m_VFXView.ToggleRuntimeMode();
        }

        void OnToggleShaderValidation(ChangeEvent<bool> evt)
        {
            m_VFXView.ToggleRuntimeMode();
        }
    }
}
