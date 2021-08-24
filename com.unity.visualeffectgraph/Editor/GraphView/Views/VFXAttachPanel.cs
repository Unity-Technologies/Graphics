using UnityEditor.Search;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace UnityEditor.VFX.UI
{
    class VFXAttachPanel : EditorWindow
    {
        public VFXView m_vfxView;

        SearchContext m_searchContext;
        TextField m_pickedObjectLabel;
        Button m_AttachButton;

        public Vector2 WindowSize { get; } = new Vector2(250, 60);

        protected void CreateGUI()
        {
            m_searchContext = Search.SearchService.CreateContext("scene", string.Empty, SearchFlags.None);

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
            var path = AssetDatabase.GetAssetPath(m_vfxView.controller?.graph?.visualEffectResource.asset);
            if (!string.IsNullOrEmpty(path))
            {
                m_searchContext.searchText = $"ref=\"{path}\"";
            }
            var view = Search.SearchService.ShowPicker(m_searchContext, SelectHandler, TrackingHandler, FilterHandler, null, "Visual Effect");
            view.itemIconSize = 0f;
        }

        private void TrackingHandler(SearchItem obj)
        {
        }

        private void SelectHandler(SearchItem arg1, bool arg2)
        {
            if (arg1?.ToObject<GameObject>() is { } go)
            {
                var vfx = go.GetComponent<VisualEffect>();
                if (m_vfxView.TryAttachTo(vfx))
                {
                    UpdateAttachedLabel();
                }
            }
        }

        private bool FilterHandler(SearchItem arg)
        {
            if (arg.ToObject<GameObject>().TryGetComponent(typeof(VisualEffect), out var component) && component is VisualEffect vfx)
            {
                return vfx.visualEffectAsset == m_vfxView.controller?.graph?.visualEffectResource.asset;
            }

            return false;
        }

        private void UpdateAttachedLabel()
        {
            var isAttached = m_vfxView.attachedComponent != null;
            var selectedVisualEffect = Selection.activeGameObject?.GetComponent<VisualEffect>();
            var isCompatible = selectedVisualEffect != null && selectedVisualEffect.visualEffectAsset == m_vfxView.controller.graph.visualEffectResource.asset;
            if (!isCompatible && !isAttached)
            {
                m_AttachButton.SetEnabled(false);
            }
            m_AttachButton.text = isAttached ? "Detach" : "Attach to selection";
            m_pickedObjectLabel.value = m_vfxView.attachedComponent?.name;
        }
    }
}
