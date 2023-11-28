using UnityEditor.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering
{
    abstract class RendererStripper<T, S> : IRenderPipelineGraphicsSettingsStripper<T>
        where T : IRenderPipelineGraphicsSettings
        where S : ScriptableRenderer
    {
        public bool active => URPBuildData.instance.buildingPlayerForUniversalRenderPipeline;

        public bool CanRemoveSettings(T settings)
        {
            foreach (var urpAssetForBuild in URPBuildData.instance.renderPipelineAssets)
            {
                foreach(var renderer in urpAssetForBuild.renderers)
                    if (renderer is S)
                        return false;
            }

            return true;
        }
    }

    class UniversalRendererResourcesStripper : RendererStripper<UniversalRendererResources, UniversalRenderer> { }
    class Renderer2DResourcesStripper : RendererStripper<Renderer2DResources, Renderer2D> { }
}
