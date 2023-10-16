using UnityEditor.Experimental;
using UnityEditor.VFX.UI;

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX
{
    class CreateFromTemplateDropDownButton : DropDownButtonBase
    {
        private const string mainButtonName = "create-button";

        private Button m_InsertButton;

        public CreateFromTemplateDropDownButton(VFXView vfxView)
            : base(
        vfxView,
        "VFXCreateFromTemplateDropDownPanel",
        "Insert a template into current asset\nHold CTRL key and click to create a new VFX",
            mainButtonName,
        EditorResources.iconsPath + "CreateAddNew.png",
        true,
        false)
        {
            var createNew = m_PopupContent.Q<Button>("createNew");
            createNew.clicked += OnCreateNew;

            var mainButton = this.Q<Button>(mainButtonName);
            mainButton.RegisterCallback<MouseUpEvent>(OnMainButtonMouseUp);

            m_InsertButton = m_PopupContent.Q<Button>("insert");
            m_InsertButton.clicked += OnInsert;
        }

        private void OnMainButtonMouseUp(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse || evt.clickCount > 1)
                return;

            // If the current asset is a subgraph do not call "OnInsert" callback and rather open the dropdown
            if (m_VFXView.controller.model.isSubgraph)
            {
                OnTogglePopup();
            }
            else if (evt.modifiers.HasFlag(EventModifiers.Control))
            {
                OnCreateNew();
            }
            else
            {
                OnInsert();
            }
        }

        protected override Vector2 GetPopupSize() => new(200, 54);

        protected override void OnOpenPopup()
        {
            m_InsertButton.SetEnabled(!m_VFXView.controller.model.isSubgraph);
            base.OnOpenPopup();
        }

        private void OnCreateNew()
        {
            VFXTemplateWindow.ShowCreateFromTemplate(null, null);
        }

        private void OnInsert()
        {
            VFXTemplateWindow.ShowInsertTemplate(m_VFXView);
        }
    }
}
