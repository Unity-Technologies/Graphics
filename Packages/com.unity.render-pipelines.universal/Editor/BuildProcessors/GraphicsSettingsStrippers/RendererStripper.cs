using UnityEditor.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering
{
    abstract class RendererStripper<T, S> : IRenderPipelineGraphicsSettingsStripper<T>
        where T : IRenderPipelineGraphicsSettings
        where S : ScriptableRendererData
    {
        public bool active => URPBuildData.instance.buildingPlayerForUniversalRenderPipeline;

        public bool CanRemoveSettings(T settings)
        {
            foreach (var urpAssetForBuild in URPBuildData.instance.renderPipelineAssets)
            {
                // UUM-57954: Use RendererData rather than Renderer which may be null during the build in some circumstances
                foreach(var rendererData in urpAssetForBuild.m_RendererDataList)
                    if (rendererData is S)
                        return false;
            }

            return true;
        }
    }

    class UniversalRendererResourcesStripper : RendererStripper<UniversalRendererResources, UniversalRendererData> { }
    class Renderer2DResourcesStripper : RendererStripper<Renderer2DResources, Renderer2DData> { }
}
