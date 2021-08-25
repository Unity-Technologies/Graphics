using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace UnityEditor.VFX.UI
{
    class VFXAttachPanel : EditorWindow
    {
        public VFXView m_vfxView;

        TextField m_pickedObjectLabel;
        Button m_AttachButton;

        public Vector2 WindowSize { get; } = new Vector2(250, 60);

        protected void CreateGUI()
        {
            var tpl = VFXView.LoadUXML("VFXAttachPanel");
            var mainContainer = tpl.CloneTree();
            m_AttachButton = mainContainer.Q<Button>("AttachButton");
            m_AttachButton.clicked += OnAttach;
            var button = mainContainer.Q<Button>("PickButton");
            button.clicked += OnPickObject;
            m_pickedObjectLabel = mainContainer.Q<TextField>("PickLabel");
            m_pickedObjectLabel.isReadOnly = true;
            UpdateAttachedLabel();
            var icon = mainContainer.Q<Image>("PickIcon");
            icon.image = EditorGUIUtility.LoadIcon("UIPackageResources/Images/" + "pick.png");
            rootVisualElement.Add(mainContainer);
        }

        private void OnAttach()
        {
            if (m_vfxView.attachedComponent != null)
            {
                m_vfxView.Detach();
            }
            else
            {
                m_vfxView.AttachToSelection();
            }

            UpdateAttachedLabel();
        }

        private void OnPickObject()
        {
            VFXPicker.Pick(m_vfxView.controller?.graph?.visualEffectResource.asset, SelectHandler);
        }

        private void SelectHandler(VisualEffect vfx)
        {
            if (m_vfxView.TryAttachTo(vfx))
            {
                UpdateAttachedLabel();
            }
        }

        private void UpdateAttachedLabel()
        {
            var isAttached = m_vfxView.attachedComponent != null;
            var selectedVisualEffect = Selection.activeGameObject?.GetComponent<VisualEffect>();
            var isCompatible = selectedVisualEffect != null && selectedVisualEffect.visualEffectAsset == m_vfxView.controller.graph.visualEffectResource.asset;
            m_AttachButton.SetEnabled(isAttached || isCompatible);
            m_AttachButton.text = isAttached ? "Detach" : "Attach to selection";
            m_pickedObjectLabel.value = m_vfxView.attachedComponent?.name;
        }
    }
}
