using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace UnityEditor.VFX.UI
{
    class VFXAttachPanel : EditorWindow
    {
        TextField m_pickedObjectLabel;
        Button m_AttachButton;
        VisualElement m_VFXIcon;
        VFXView m_VFXView;

        public Vector2 WindowSize { get; } = new Vector2(250, 50);

        public void SetView(VFXView view)
        {
            m_VFXView = view;
        }

        protected void CreateGUI()
        {
            rootVisualElement.styleSheets.Add(VFXView.LoadStyleSheet("VFXAttachPanel"));

            var tpl = VFXView.LoadUXML("VFXAttachPanel");
            var mainContainer = tpl.CloneTree();
            m_AttachButton = mainContainer.Q<Button>("AttachButton");
            m_AttachButton.clicked += OnAttach;
            var button = mainContainer.Q<Button>("PickButton");
            button.clicked += OnPickObject;
            m_pickedObjectLabel = mainContainer.Q<TextField>("PickLabel");
            m_pickedObjectLabel.isReadOnly = true;
            m_VFXIcon = mainContainer.Q<VisualElement>("VFXIcon");
            UpdateAttachedLabel();
            rootVisualElement.Add(mainContainer);
        }

        void OnAttach()
        {
            if (m_VFXView.attachedComponent != null)
            {
                m_VFXView.Detach();
            }
            else
            {
                m_VFXView.AttachToSelection();
            }

            UpdateAttachedLabel();
        }

        void OnPickObject()
        {
            VFXPicker.Pick(m_VFXView.controller?.graph?.visualEffectResource.asset, SelectHandler);
        }

        void SelectHandler(VisualEffect vfx)
        {
            if (vfx != null)
            {
                m_VFXView.TryAttachTo(vfx, true);
            }
            else
            {
                m_VFXView.Detach();
            }

            UpdateAttachedLabel();
        }

        void UpdateAttachedLabel()
        {
            if (m_VFXView.controller?.graph != null)
            {
                var isAttached = m_VFXView.attachedComponent != null;
                VisualEffect selectedVisualEffect = null;
                Selection.activeGameObject?.TryGetComponent(out selectedVisualEffect);
                var isCompatible = selectedVisualEffect != null && selectedVisualEffect.visualEffectAsset == m_VFXView.controller.graph.visualEffectResource.asset;
                m_AttachButton.SetEnabled(isAttached || isCompatible);
                m_AttachButton.text = isAttached ? "Detach" : "Attach to selection";
                m_pickedObjectLabel.value = m_VFXView.attachedComponent?.name ?? "None (Visual Effect Asset)";

                if (isAttached)
                {
                    m_VFXIcon.style.display = DisplayStyle.Flex;
                    m_pickedObjectLabel[0].style.paddingLeft = 18;
                    m_VFXIcon.style.backgroundImage = VFXView.LoadImage(EditorGUIUtility.isProSkin ? "vfx_graph_icon_gray_dark" : "vfx_graph_icon_gray_light");
                }
                else
                {
                    m_pickedObjectLabel[0].style.paddingLeft = 3;
                    m_VFXIcon.style.display = DisplayStyle.None;
                }
            }
        }
    }
}
