#if ENABLE_VR && ENABLE_XR_MANAGEMENT
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPProcessScene : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report)
        {
            if (!HDRPBuildData.instance.buildingPlayerForHDRenderPipeline)
                return;

            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var buildTargetSettings = XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);

            if(buildTargetSettings == null || buildTargetSettings.AssignedSettings == null || buildTargetSettings.AssignedSettings.activeLoaders.Count <= 0)
                return;

            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                Camera[] cameras = root.GetComponentsInChildren<Camera>();
                foreach (Camera camera in cameras)
                {
                    if (camera.TryGetComponent<HDAdditionalCameraData>(out HDAdditionalCameraData cameraData))
                    {
                        if (camera.orthographic && cameraData.xrRendering)
                        {
                            Debug.LogWarning($"One or more cameras have their projection set as Orthographic. This is not supported on XR and may produce artifacts at runtime. Please change the projection setting to Perspective to avoid issues.");
                            break;
                        }
                    }
                }
            }
        }
    }
}
#endif
