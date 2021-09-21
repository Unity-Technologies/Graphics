using System.IO;

using UnityEditor.Experimental;
using UnityEditor.VersionControl;

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXSaveDropdownButton : DropDownButtonBase
    {
        private readonly VFXView m_VFXView;
        private readonly Button m_CheckoutButton;

        public VFXSaveDropdownButton(VFXView vfxView, VFXViewWindow parentWindow)
            : base(
                parentWindow,
                "VFXSaveDropDownPanel",
                "Save",
                "save-button",
                EditorResources.iconsPath + "SaveActive.png",
                false,
                true)
        {
            m_VFXView = vfxView;

            var saveAsButton = m_PopupContent.Q<Button>("saveAs");
            saveAsButton.clicked += OnSaveAs;

            m_CheckoutButton = m_PopupContent.Q<Button>("checkout");
            m_CheckoutButton.clicked += OnCheckout;

            var selectButton = m_PopupContent.Q<Button>("showInInspector");
            selectButton.clicked += OnSelectAsset;
        }

        protected override Vector2 GetPopupSize() => new Vector2(150, CanCheckout() ? 76 : 56);

        protected override void OnOpenPopup()
        {
            if (m_VFXView.controller?.model?.visualEffectObject != null)
            {
                // Hide checkout button if perforce is not available and disable it if the asset is already checked out
                if (CanCheckout())
                {
                    var isAllReadyCheckedOut = m_VFXView.IsAssetEditable();
                    m_CheckoutButton.SetEnabled(!isAllReadyCheckedOut);
                }
                else
                {
                    m_CheckoutButton.style.display = DisplayStyle.None;
                }
            }
        }

        protected override void OnMainButton()
        {
            m_VFXView.OnSave();
        }

        bool CanCheckout() => Provider.isActive && Provider.enabled;

        void OnSaveAs()
        {
            var originalPath = AssetDatabase.GetAssetPath(m_VFXView.controller.model);
            var extension = Path.GetExtension(originalPath).Trim('.');
            var newFilePath = EditorUtility.SaveFilePanelInProject("Save VFX Graph As...", Path.GetFileNameWithoutExtension(originalPath), extension, "", Path.GetDirectoryName(originalPath));
            if (!string.IsNullOrEmpty(newFilePath))
            {
                m_VFXView.SaveAs(newFilePath);
            }

            ClosePopup();
        }

        void OnCheckout()
        {
            m_VFXView.Checkout();
        }

        void OnSelectAsset()
        {
            m_VFXView.SelectAsset();
        }
    }
}
