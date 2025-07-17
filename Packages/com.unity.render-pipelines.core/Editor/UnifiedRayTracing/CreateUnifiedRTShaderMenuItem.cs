using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;

namespace UnityEditor.Rendering.UnifiedRayTracing
{
    internal class ShaderTemplates
    {
        [MenuItem("Assets/Create/Shader/Unified Ray Tracing Shader", false, 1)]
        internal static void CreateNewUnifiedRayTracingShader()
        {
            var action = ScriptableObject.CreateInstance<DoCreateUnifiedRayTracingShader>();
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, action, "NewUnifiedRayTracingShader.urtshader", null, null);
        }

        internal class DoCreateUnifiedRayTracingShader : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                string fullPath = Path.GetFullPath(pathName);
                File.WriteAllText(fullPath, shaderContent);

                AssetDatabase.ImportAsset(pathName);
                var shader = AssetDatabase.LoadAssetAtPath(pathName, typeof(Object));

                ProjectWindowUtil.ShowCreatedAsset(shader);
            }
        }

const string shaderContent =
@"#include ""Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/TraceRayAndQueryHit.hlsl""

UNIFIED_RT_DECLARE_ACCEL_STRUCT(_AccelStruct);

void RayGenExecute(UnifiedRT::DispatchInfo dispatchInfo)
{
    // Example code:
    UnifiedRT::Ray ray;
    ray.origin = 0;
    ray.direction = float3(0, 0, 1);
    ray.tMin = 0;
    ray.tMax = 1000.0f;
    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(_AccelStruct);
    UnifiedRT::Hit hitResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, ray, 0);
    if (hitResult.IsValid())
    {
        // Handle found intersection
    }

}
";
    }
}


