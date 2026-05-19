using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering
{
    class UniversalDebugShaderStripper : IRenderPipelineGraphicsSettingsStripper<UniversalRenderPipelineDebugShaders>
    {
        public bool active => true;

        public bool CanRemoveSettings(UniversalRenderPipelineDebugShaders settings)
        {
            if (!CoreBuildData.instance.useDiagnosticChecks)
                return true;

            return GraphicsSettings.GetRenderPipelineSettings<ShaderStrippingSetting>()?.stripRuntimeDebugShaders ?? false;
        }
    }
}
