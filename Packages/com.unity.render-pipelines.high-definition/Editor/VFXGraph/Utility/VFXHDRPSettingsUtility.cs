using System.Linq;
using UnityEditor.VFX.UI;

namespace UnityEditor.VFX.HDRP
{
    static class VFXHDRPSettingsUtility
    {
        public static void RefreshVfxErrorsIfNeeded(ref bool needRefreshVfxErrors)
        {
            if (needRefreshVfxErrors)
            {
                foreach (var vfxWindow in VFXViewWindow.GetAllWindows())
                {
                    if (vfxWindow != null && vfxWindow.graphView != null)
                    {
                        var vfxGraph = vfxWindow.graphView.controller.graph;
                        foreach (var output in vfxGraph.children.OfType<VFXDecalHDRPOutput>())
                            output.RefreshErrors();
                    }
                }
            }
            needRefreshVfxErrors = false;
        }
    }
}
