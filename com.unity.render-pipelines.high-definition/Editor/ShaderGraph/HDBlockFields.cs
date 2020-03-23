using UnityEngine;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    internal static class HDBlockFields
    {
        [GenerateBlocks]
        public struct SurfaceDescription
        {
            public static string name = "SurfaceDescription";
            public static BlockFieldDescriptor Distortion = new BlockFieldDescriptor(SurfaceDescription.name, "Distortion", "SURFACEDESCRIPTION_DISTORTION",
                new Vector2Control(Vector2.one), ShaderStage.Fragment);
            public static BlockFieldDescriptor DistortionBlur = new BlockFieldDescriptor(SurfaceDescription.name, "DistortionBlur", "SURFACEDESCRIPTION_DISTORTIONBLUR",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor ShadowTint = new BlockFieldDescriptor(SurfaceDescription.name, "ShadowTint", "SURFACEDESCRIPTION_SHADOWTINT",
                new ColorRGBAControl(Color.black), ShaderStage.Fragment);
        }
    }
}
