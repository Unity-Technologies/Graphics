using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXIconBadge : VisualElement
    {
        public VFXIconBadge(string description, VFXErrorType vfxErrorType)
        {
            Add(new VisualElement { name = "tip"});
            tooltip = description;

            AddToClassList(vfxErrorType == VFXErrorType.Error ? "badge-error" : vfxErrorType == VFXErrorType.Warning ? "badge-warning" : "badge-perf");
        }
    }
}
