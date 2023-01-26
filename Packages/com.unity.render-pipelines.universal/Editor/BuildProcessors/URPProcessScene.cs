using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    class URPProcessScene : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report)
        {
            bool usesURP = false;
            if (GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset != null)
            {
                // ^ The global pipeline is set to URP
                usesURP = true;
            }
            else
            {
                // ^ The global pipeline isn't set to URP, but a quality setting could still use it
                for (int i = 0; i < QualitySettings.count; i++)
                {
                    if (QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset != null)
                    {
                        // ^ This quality setting uses URP
                        usesURP = true;
                        break;
                    }
                }
            }

            if (usesURP)
            {
                GameObject[] roots = scene.GetRootGameObjects();

                foreach (GameObject root in roots)
                {
                    Light[] lights = root.GetComponentsInChildren<Light>();
                    foreach (Light light in lights)
                    {
                        if (light.type != LightType.Directional &&
                            light.type != LightType.Point &&
                            light.type != LightType.Spot &&
                            light.type != LightType.Rectangle)
                        {
                            Debug.LogWarning(
                                $"The {light.type} light type on the GameObject '{light.gameObject.name}' is unsupported by URP, and will not be rendered."
                            );
                        }
                        else if (light.type == LightType.Rectangle && light.lightmapBakeType != LightmapBakeType.Baked)
                        {
                            Debug.LogWarning(
                                $"The GameObject '{light.gameObject.name}' is an area light type, but the mode is not set to baked. URP only supports baked area lights, not realtime or mixed ones."
                            );
                        }
                    }
                }
            }
        }
    }
}
