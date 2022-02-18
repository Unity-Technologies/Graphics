#if HAS_VFX_GRAPH
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    internal class VFXShaderGraphLitGUI : ShaderGraphLitGUI
    {
        protected override uint materialFilter => uint.MaxValue & ~(uint)Expandable.SurfaceInputs;
    }

    internal class VFXShaderGraphUnlitGUI : ShaderGraphUnlitGUI
    {
        protected override uint materialFilter => uint.MaxValue & ~(uint)Expandable.SurfaceInputs;
    }
}
#endif
