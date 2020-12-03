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

            // TODO: temporarily here, merge with HDRP into "core" BlockFields.cs
            public static BlockFieldDescriptor CoatMask       = new BlockFieldDescriptor(SurfaceDescription.name, "CoatMask", "Coat Mask", "SURFACEDESCRIPTION_COATMASK",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatSmoothness = new BlockFieldDescriptor(SurfaceDescription.name, "CoatSmoothness", "Coat Smoothness", "SURFACEDESCRIPTION_COATSMOOTHNESS",
                new FloatControl(1.0f), ShaderStage.Fragment);
        }
    }
}
