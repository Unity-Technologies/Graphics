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

        public VFXSaveDropdownButton(VFXView vfxView) : base("VFXSaveDropDownPanel", "Save", EditorResources.iconsPath + "SaveActive.png", false, true)
        {
            m_VFXView = vfxView;

            var saveAsButton = m_PopupContent.Q<Button>("saveAs");
            saveAsButton.clicked += OnSaveAs;

            m_CheckoutButton = m_PopupContent.Q<Button>("checkout");
            m_CheckoutButton.clicked += OnCheckout;

            var selectButton = m_PopupContent.Q<Button>("showInInspector");
            selectButton.clicked += OnSelectAsset;
        }

        protected override Vector2 GetPopupPosition() => m_VFXView.ViewToScreenPosition(worldBound.position);
        protected override Vector2 GetPopupSize() => new Vector2(150, 80);

        protected override void OnOpenPopup()
        {
            // Disable checkout button if perforce is not available
            if (m_VFXView.controller?.model?.visualEffectObject != null)
            {
                var canCheckout = !this.m_VFXView.IsAssetEditable() && Provider.isActive && Provider.enabled;
                m_CheckoutButton.SetEnabled(canCheckout);
            }
        }

        protected override void OnMainButton()
        {
            m_VFXView.OnSave();
        }

        private void OnSaveAs()
        {
            Debug.Log("save as");
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

        void OnResyncMaterial()
        {
            //controller.graph.Invalidate(VFXModel.InvalidationCause.kMaterialChanged);
        }
    }
}
