using System.Text;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace UnityEditor.Rendering.Universal
{
    class URPProcessScene : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset rpAsset)
                return;

            var builder = new StringBuilder();
            int warningCount = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Light light in root.GetComponentsInChildren<Light>(includeInactive: true))
                    CheckLights(builder, ref warningCount, light);

                if (rpAsset.intermediateTextureMode != IntermediateTextureMode.Never)
                    continue;

                foreach (UniversalAdditionalCameraData cameraData in root.GetComponentsInChildren<UniversalAdditionalCameraData>(includeInactive: true))
                    CheckCameraData(builder, ref warningCount, cameraData);
            }

            if (warningCount > 0)
                Debug.LogWarning($"Scene '{scene.path}' has {warningCount} incompatibilit{(warningCount > 1 ? "ies" : "y")}:\n{builder}");
        }

        private void CheckLights(StringBuilder builder, ref int count, Light light)
        {
            switch (light.type)
            {
                case LightType.Rectangle when light.lightmapBakeType != LightmapBakeType.Baked:
                    AddWarning(builder, ref count,
                        $"The GameObject '{GetHierarchyPath(light.transform)}' is an area light type, but the mode is not set to baked. URP only supports baked area lights, not realtime or mixed ones.");
                    break;
                case LightType.Directional:
                case LightType.Point:
                case LightType.Spot:
                case LightType.Rectangle:
                    break; // Supported types.
                default:
                    AddWarning(builder, ref count, $"The {light.type} light type on the GameObject '{GetHierarchyPath(light.transform)}' is unsupported by URP and will not be rendered.");
                    break;
            }
        }

        private void CheckCameraData(StringBuilder builder, ref int count, UniversalAdditionalCameraData camData)
        {
            CheckCameraSetting(builder, ref count, camData, camData.renderPostProcessing, "Post-processing");
            CheckCameraSetting(builder, ref count, camData, camData.allowHDROutput, "HDR Output");
            CheckCameraSetting(builder, ref count, camData, camData.requiresColorTexture, "_CameraOpaqueTexture");
            CheckCameraSetting(builder, ref count, camData, camData.requiresDepthTexture, "_CameraDepthTexture");
        }

        void CheckCameraSetting(StringBuilder builder, ref int count, UniversalAdditionalCameraData data, bool isEnabled, string featureName)
        {
            if (isEnabled)
                AddWarning(builder, ref count,
                    $"Camera '{GetHierarchyPath(data.transform)}': '{featureName}' is enabled, but the URP Asset disables the required intermediate texture. The feature will be skipped.");
        }

        void AddWarning(StringBuilder builder, ref int currentCount, string message)
        {
            currentCount++;
            builder.AppendLine($"  - {currentCount}: {message}");
        }

        string GetHierarchyPath(Transform transform)
        {
            using (ListPool<string>.Get(out var pathSegments))
            {
                for (var t = transform; t != null; t = t.parent)
                    pathSegments.Add(t.name);
                pathSegments.Reverse();
                return string.Join("/", pathSegments);
            }
        }
    }
}