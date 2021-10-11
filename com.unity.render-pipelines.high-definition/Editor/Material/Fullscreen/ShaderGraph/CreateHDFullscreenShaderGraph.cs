using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.Rendering.Fullscreen.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class CreateHDFullscreenShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/HDRP/Fullscreen Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 7)]
        public static void CreateHDFullscreenGraph()
        {
            var target = (HDTarget)Activator.CreateInstance(typeof(HDTarget));
            target.TrySetActiveSubTarget(typeof(HDFullscreenSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
