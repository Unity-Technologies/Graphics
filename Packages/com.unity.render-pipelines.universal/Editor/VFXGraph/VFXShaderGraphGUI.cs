#if HAS_VFX_GRAPH
using UnityEditor.ShaderGraph;
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

    internal class VFXGenericShaderGraphMaterialGUI : GenericShaderGraphMaterialGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            //When material used in VFX, all properties are converted to input slots.
            //This fallback is used with sprite output.
        }
    }

}
#endif
