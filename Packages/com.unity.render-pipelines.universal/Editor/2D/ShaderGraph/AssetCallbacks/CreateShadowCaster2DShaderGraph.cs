using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class CreateShadowCaster2DShaderGraph
    {
        // "<Name> Shader Graph" -- two words, matching all eleven siblings under this submenu.
        // Asserted by ShadowCaster2DSubTargetTests so a typo here cannot silently hide the menu entry.
        internal const string k_MenuPath = "Assets/Create/Shader Graph/URP/Shadow2D Shader Graph";

        // Greys the menu entry out in a project that builds hard shadow geometry.
        //
        // This SubTarget emits "Shadow2DGenerators" = "Unity.SoftShadow" and nothing else, so a graph
        // it generates can only read the enhanced geometry layout. In a project on the Legacy
        // generation there is no caster whose mesh it could draw, and the failure would be a silent
        // one -- semantics the mesh does not carry arrive as zero rather than erroring -- so the
        // entry point is closed instead.
        //
        // Validate rather than removing the MenuItem: a greyed entry the user can see and hover is
        // what makes the setting discoverable, where a missing one just looks like the feature does
        // not exist.
        [MenuItem(k_MenuPath, true)]
        public static bool ValidateCreateShadowCaster2DGraph()
        {
            return Shadow2DGeometry.enhancedGeometryEnabled;
        }

        // Priority nudged +3 so this sits directly under Light2D Shader Graph (+2), keeping the pair
        // that authors a light and its shadows together at the end of the 2D section.
        [MenuItem(k_MenuPath, priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateShaderMenuPriority + 3)]
        public static void CreateShadowCaster2DGraph()
        {
            var target = (UniversalTarget)Activator.CreateInstance(typeof(UniversalTarget));
            target.TrySetActiveSubTarget(typeof(UniversalShadowCaster2DSubTarget));

            // Position/Normal/Tangent are deliberately absent: nothing on the 2D shadow path reads a
            // normal or a tangent, and on the projected passes TANGENT is the geometry generator's
            // payload rather than a tangent at all.
            //
            // This list must stay in step with UniversalShadowCaster2DSubTarget.GetActiveBlocks -- a
            // block seeded here but not registered there is a port the user can wire that codegen then
            // ignores, and one registered there but missing here is a port absent from a freshly created
            // asset. CreateMenuSeedsExactlyTheRegisteredBlocks asserts the equivalence.
            var blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                UniversalBlockFields.VertexDescription.ShadowDisplacement,
                UniversalBlockFields.VertexDescription.ShadowSideSoftness,
                UniversalBlockFields.VertexDescription.ShadowBackSoftness,
                BlockFields.SurfaceDescription.Alpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
