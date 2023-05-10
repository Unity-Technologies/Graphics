using System;

using UnityEditor.Experimental;
using UnityEditor.VFX.UI;

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX
{
    class CreateFromTemplateDropDownButton : DropDownButtonBase
    {
        private Action m_LastAction;
        private Button m_InsertButton;

        public CreateFromTemplateDropDownButton(VFXView vfxView)
            : base(
        vfxView,
        "VFXCreateFromTemplateDropDownPanel",
        "Create or Insert from a template",
        "create-button",
        EditorResources.iconsPath + "CreateAddNew.png",
        true,
        false)
        {
            var createNew = m_PopupContent.Q<Button>("createNew");
            createNew.clicked += OnCreateNew;

            m_InsertButton = m_PopupContent.Q<Button>("insert");
            m_InsertButton.clicked += OnInsert;
        }

        protected override Vector2 GetPopupSize() => new(200, 54);

        protected override void OnOpenPopup()
        {
            m_InsertButton.SetEnabled(!m_VFXView.controller.model.isSubgraph);
            base.OnOpenPopup();
        }

        protected override void OnMainButton()
        {
            // If the current asset is a subgraph do not call "OnInsert" callback and rather open the dropdown
            if (m_LastAction != null && (!m_VFXView.controller.model.isSubgraph || m_LastAction != OnInsert))
                m_LastAction();
            else
            {
                OnTogglePopup();
            }
        }

        private void OnCreateNew()
        {
            m_LastAction = OnCreateNew;
            VFXTemplateWindow.ShowCreateFromTemplate(null, null);
            SetMainButtonTooltip("Create new VFX asset from a template");
        }

        private void OnInsert()
        {
            m_LastAction = OnInsert;
            VFXTemplateWindow.ShowInsertTemplate(m_VFXView);
            SetMainButtonTooltip("Insert VFX into current asset from template");
        }
    }
}
