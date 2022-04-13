using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.Rendering.Fullscreen.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class CreateFullscreenShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/URP/Fullscreen Shader Graph", priority = CoreUtils.Sections.section5 + CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateFullscreenGraph()
        {
            var target = (UniversalTarget)Activator.CreateInstance(typeof(UniversalTarget));
            target.TrySetActiveSubTarget(typeof(UniversalFullscreenSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
