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
            return CreateInstance<VFXViewPresenter>();
        }
    }
}