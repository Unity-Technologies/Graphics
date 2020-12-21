using UnityEngine;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalBlockFields
    {
        [GenerateBlocks("Universal Render Pipeline")]
        public struct SurfaceDescription
        {
            public static string name = "SurfaceDescription";
            public static BlockFieldDescriptor SpriteMask = new BlockFieldDescriptor(SurfaceDescription.name, "SpriteMask", "Sprite Mask", "SURFACEDESCRIPTION_SPRITEMASK",
                new ColorRGBAControl(new Color(1, 1, 1, 1)), ShaderStage.Fragment);
        }
    }
}
