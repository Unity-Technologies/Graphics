using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    // TODO: support different modes
    // PaletteR8 (dx12, NVIDIA Turing)
    // DensityR8G8 (Oculus Quest)

    // TODO: handle caps for bigger tiles bigger than 2x2
    // TODO: handle asymetric rates ?

    // Important note: this should match the hard-coded palette at the C++ platform level
    public enum ShadingRatePalette : int
    {
        Culled = 0,
        Tile4x4,    // cap
        Tile2x4,    // cap
        Tile2x2,
        Tile1x2,
        Msaa1x,     // default shading rate
        Msaa2x,
        Msaa4x,
        Msaa8x
    }

    public static class VariableRateShading
    {
        static MaterialPropertyBlock matBlock = new MaterialPropertyBlock();

        // VRS Tiers 2
        public static uint textureTileSize
        {
            get
            {
                // TODO: connect to C++ via new API (on SystemInfo?)
                return 16;
            }
        }

        // VRS Tiers 2
        public static GraphicsFormat textureFormat
        {
            get
            {
                // TODO: connect to C++ via new API (on SystemInfo?)
                return GraphicsFormat.R8_UInt;
            }
        }

        public static void BindShaderParameters(MSAASamples msaaSamples, MaterialPropertyBlock matBlock)
        {
            ShadingRatePalette shadingRateMin = ShadingRatePalette.Tile4x4;
            ShadingRatePalette shadingRateMax = ShadingRatePalette.Msaa1x;

            switch (msaaSamples)
            {
                case MSAASamples.MSAA2x:
                    shadingRateMin = ShadingRatePalette.Tile2x4;
                    shadingRateMax = ShadingRatePalette.Msaa2x;
                    break;

                case MSAASamples.MSAA4x:
                    shadingRateMin = ShadingRatePalette.Tile2x2;
                    shadingRateMax = ShadingRatePalette.Msaa4x;
                    break;

                case MSAASamples.MSAA8x:
                    shadingRateMin = ShadingRatePalette.Tile1x2;
                    shadingRateMax = ShadingRatePalette.Msaa8x;
                    break;
            }

            // TODO: clamp depending on C++ caps

            matBlock.SetInt("_ShadingRateMin", (int)shadingRateMin);
            matBlock.SetInt("_ShadingRateMax", (int)shadingRateMax + 1);
        }
    }
}
