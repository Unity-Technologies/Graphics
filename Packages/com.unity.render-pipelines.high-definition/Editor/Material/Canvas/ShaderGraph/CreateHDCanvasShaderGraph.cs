using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class CreateCanvasShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/HDRP/Canvas Shader Graph", priority = CoreUtils.Sections.section3 +  CoreUtils.Priorities.assetsCreateShaderMenuPriority + 8)]
        public static void CreateCanvasGraph()
        {
            var target = (HDTarget)Activator.CreateInstance(typeof(HDTarget));
            target.TrySetActiveSubTarget(typeof(HDCanvasSubTarget));

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
