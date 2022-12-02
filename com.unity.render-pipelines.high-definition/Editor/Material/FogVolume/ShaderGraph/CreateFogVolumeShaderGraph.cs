using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    internal static class CreateFogVolumeShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/HDRP/Fog Volume Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 8)]
        public static void Create()
        {
            var target = (HDTarget)Activator.CreateInstance(typeof(HDTarget));
            target.TrySetActiveSubTarget(typeof(FogVolumeSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
