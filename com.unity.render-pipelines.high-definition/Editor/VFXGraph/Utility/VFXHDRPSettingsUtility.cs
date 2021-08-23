using System.Linq;
using UnityEditor.VFX.UI;

namespace UnityEditor.VFX.HDRP
{
    internal static class VFXHDRPSettingsUtility
    {
        internal static void RefreshVfxErrorsIfNeeded(ref bool needRefreshVfxErrors)
        {
            if (needRefreshVfxErrors)
            {
                var vfxWindow = VFXViewWindow.currentWindow;
                if (vfxWindow != null)
                {
                    var vfxGraph = vfxWindow.graphView.controller.graph;
                    foreach (var output in vfxGraph.children.OfType<VFXDecalHDRPOutput>())
                        output.RefreshErrors(vfxGraph);
                }
            }
            needRefreshVfxErrors = false;
        }
    }
}
