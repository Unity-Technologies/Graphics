using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.Rendering.Fullscreen.ShaderGraph;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    static class CreateFullscreenShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/BuiltIn/Fullscreen Shader Graph", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateShaderMenuPriority + 2)]
        public static void CreateFullscreenGraph()
        {
            var target = (BuiltInTarget)Activator.CreateInstance(typeof(BuiltInTarget));
            target.TrySetActiveSubTarget(typeof(BuiltInFullscreenSubTarget));

            var blockDescriptors = new[]
            {
                FullscreenBlocks.color,
                BlockFields.SurfaceDescription.Alpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
