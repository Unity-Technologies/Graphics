using RMGUI.GraphView;

namespace UnityEditor.VFX.UI
{
    class VFXViewWindow : GraphViewEditorWindow
    {
        [MenuItem("Window/VFXEditorNew")]
        public static void ShowWindow()
        {
            GetWindow<VFXViewWindow>();
        }

        protected override GraphView BuildView()
        {
            return new VFXView();
        }

        protected override GraphViewPresenter BuildPresenters()
        {
            m_ViewPresenter = CreateInstance<VFXViewPresenter>();
            return m_ViewPresenter;
        }

        protected new void OnEnable()
        {
            base.OnEnable();
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged(); // Call when enabled to retrieve the current selection
        }

        protected new void OnDisable()
        {
            m_ViewPresenter.SetModelContainer(null);
            Selection.selectionChanged -= OnSelectionChanged;
            base.OnDisable();
        }

        void OnSelectionChanged()
        {
            var assets = Selection.assetGUIDs;
            if (assets.Length == 1)
            {
                var selected = AssetDatabase.LoadAssetAtPath<VFXModelContainer>(AssetDatabase.GUIDToAssetPath(assets[0]));
                if (selected != null)
                    m_ViewPresenter.SetModelContainer(selected);
            }
        }

        private VFXViewPresenter m_ViewPresenter;
    }
}