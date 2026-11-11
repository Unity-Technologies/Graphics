using UnityEditor.VFX.UI;

namespace UnityEditor.VFX.Test
{
    internal static class VFXCopyPasteTestHelper
    {
        internal static VFXViewController CreateTestAsset()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();

            VFXTestCommon.CloseAllUnecessaryWindows();
            while (EditorWindow.HasOpenInstances<VFXViewWindow>())
                EditorWindow.GetWindow<VFXViewWindow>().Close();

            var window = VFXTestCommon.GetWindow(graph, true, true);
            window.LoadAsset(graph.GetResource().asset, null);

            return window.graphView.controller;
        }

        internal static void DestroyTestAsset()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
            while (EditorWindow.HasOpenInstances<VFXViewWindow>())
                EditorWindow.GetWindow<VFXViewWindow>().Close();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }
    }
}
