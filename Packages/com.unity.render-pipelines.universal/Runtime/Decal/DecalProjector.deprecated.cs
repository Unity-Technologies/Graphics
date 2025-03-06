using System;

namespace UnityEngine.Rendering.Universal
{
    public partial class DecalProjector
    {
        [SerializeField, Obsolete("This field is only kept for migration purpose. Use m_RenderingLayersMask instead. #from(6000.2)", false)]
        uint m_DecalLayerMask = 1;
    }
}