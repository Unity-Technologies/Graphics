using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    static class CreateCanvasShadergraph
    {
        [MenuItem("Assets/Create/Shader Graph/BuiltIn/Canvas Shader Graph", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateShaderMenuPriority + 2)]
        public static void CreateUnlitGraph()
        {
            var target = (BuiltInTarget)Activator.CreateInstance(typeof(BuiltInTarget));
            target.TrySetActiveSubTarget(typeof(BuiltInCanvasSubTarget));

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
