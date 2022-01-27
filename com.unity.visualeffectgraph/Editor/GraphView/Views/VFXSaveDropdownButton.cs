using System.IO;

using UnityEditor.Experimental;
using UnityEditor.VersionControl;

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXSaveDropdownButton : DropDownButtonBase
    {
        public VFXSaveDropdownButton(VFXView vfxView)
            : base(
                vfxView,
                "VFXSaveDropDownPanel",
                "Save",
                "save-button",
                EditorResources.iconsPath + "SaveActive.png",
                false,
                true)
        {
            var saveAsButton = m_PopupContent.Q<Button>("saveAs");
            saveAsButton.clicked += OnSaveAs;

            var selectButton = m_PopupContent.Q<Button>("showInInspector");
            selectButton.clicked += OnSelectAsset;
        }

        protected override Vector2 GetPopupSize() => new(150, 56);

        protected override void OnMainButton()
        {
            m_VFXView.OnSave();
        }

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

        void OnSelectAsset()
        {
            m_VFXView.SelectAsset();
        }
    }
}
