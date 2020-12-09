using UnityEditor.ShaderGraph;
using UnityEditor.VFX;

using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class VFXLitSubTarget : HDLitSubTarget, IVFXCompatibleTarget
    {
        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/ShaderPassVFX.template";

        public bool TryConfigureVFX(VFXContext context)
        {
            return true;
        }
    }
}
