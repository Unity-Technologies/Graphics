using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class CreateFilterShaderGraph
    {
        const string k_MenuPath = "Assets/Create/Shader Graph/URP/Filter Shader Graph";

        // Gates this Assets/Create entry point only, since UniversalFilterSubTarget itself never sets
        // isHidden. Filter still appears in an existing graph's subtarget popup regardless of the flag.
        const string k_EnableFilterShaderGraphKey = "UIToolkit.EnableFilterShaderGraph";

        [MenuItem(k_MenuPath, validate = true)]
        public static bool ValidateCreateFilterGraph()
        {
            return bool.TryParse(EditorUserSettings.GetConfigValue(k_EnableFilterShaderGraphKey), out var enabled) && enabled;
        }

        [MenuItem(k_MenuPath, priority = CoreUtils.Sections.section5 + CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateFilterGraph()
        {
            var target = (UniversalTarget)Activator.CreateInstance(typeof(UniversalTarget));
            target.TrySetActiveSubTarget(typeof(UniversalFilterSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
