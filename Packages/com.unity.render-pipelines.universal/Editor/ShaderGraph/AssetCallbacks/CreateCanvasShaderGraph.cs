using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.Rendering.Fullscreen.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class CreateCanvasShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/URP/Canvas Shader Graph", priority = CoreUtils.Sections.section5 + CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateCanvasGraph()
        {
            var target = (UniversalTarget)Activator.CreateInstance(typeof(UniversalTarget));
            target.TrySetActiveSubTarget(typeof(UniversalCanvasSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
