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
            bool usesURP = URPBuildData.instance.buildingPlayerForUniversalRenderPipeline;

            if (usesURP)
            {
                GameObject[] roots = scene.GetRootGameObjects();
#if XR_MANAGEMENT_4_0_1_OR_NEWER && ENABLE_VR && ENABLE_XR_MODULE
                var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                var buildTargetSettings = XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
#endif
                foreach (GameObject root in roots)
                {
                    Light[] lights = root.GetComponentsInChildren<Light>();
                    foreach (Light light in lights)
                    {
                        if (light.type != LightType.Directional &&
                            light.type != LightType.Point &&
                            light.type != LightType.Spot &&
                            light.type != LightType.Rectangle &&
                            light.type != LightType.Disc)
                        {
                            Debug.LogWarning(
                                $"The {light.type} light type on the GameObject '{light.gameObject.name}' is unsupported by URP, and will not be rendered."
                            );
                        }
                        else if ((light.type == LightType.Rectangle || light.type == LightType.Disc) && light.lightmapBakeType != LightmapBakeType.Baked)
                        {
                            Debug.LogWarning(
                                $"The GameObject '{light.gameObject.name}' is an area light type, but the mode is not set to baked. URP only supports baked area lights, not realtime or mixed ones."
                            );
                        }
                    }

#if XR_MANAGEMENT_4_0_1_OR_NEWER && ENABLE_VR && ENABLE_XR_MODULE
                    if (buildTargetSettings != null && buildTargetSettings.AssignedSettings != null && buildTargetSettings.AssignedSettings.activeLoaders.Count > 0)
                    {
                        Camera[] cameras = root.GetComponentsInChildren<Camera>();
                        foreach (Camera camera in cameras)
                        {
                            if (camera.TryGetComponent<UniversalAdditionalCameraData>(out UniversalAdditionalCameraData cameraData))
                            {
                                if (camera.orthographic && cameraData.allowXRRendering)
                                {
                                    Debug.LogWarning($"One or more cameras have their projection set as Orthographic. This is not supported on XR and may produce artifacts at runtime. Please change the projection setting to Perspective to avoid issues.");
                                    break;
                                }
                            }
                        }
                    }
#endif
                }
            }
        }
    }
}
