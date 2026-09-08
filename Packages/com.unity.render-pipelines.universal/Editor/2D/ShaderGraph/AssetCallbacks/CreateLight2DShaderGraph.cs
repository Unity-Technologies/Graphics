using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class CreateLight2DShaderGraph
    {
        // "Light2D", not "Light 2D": the entry names the component the graph is authored for, and both
        // the component and the sibling Shadow2D entry spell it without the space.
        //
        // Priority nudged +2 so this sits after the Sprite entries in the 2D section, with
        // Shadow2D Shader Graph (+3) directly under it — the read order in the Assets/Create menu is
        // Unlit, Lit, Custom Lit, Light2D, Shadow2D.
        [MenuItem("Assets/Create/Shader Graph/URP/Light2D Shader Graph", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateShaderMenuPriority + 2)]
        public static void CreateLight2DGraph()
        {
            var target = (UniversalTarget)Activator.CreateInstance(typeof(UniversalTarget));
            target.TrySetActiveSubTarget(typeof(UniversalLight2DSubTarget));

            // Light2DColor is registered on UniversalBlockFields.SurfaceDescription (which
            // has [GenerateBlocks]) with a white default and a unique name — so it survives
            // GraphData deserialization's tag+name remap and stays reference-identical to
            // what the SubTarget's Fragment mask and GetActiveBlocks return. That's what
            // makes user edits to the block's color swatch actually reach codegen.
            var blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                UniversalBlockFields.SurfaceDescription.Light2DColor,
                UniversalBlockFields.SurfaceDescription.Light2DShadowColor,
                UniversalBlockFields.SurfaceDescription.Light2DVolumetricColor,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
