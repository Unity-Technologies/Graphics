using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class BilateralUpsample
    {
        // This is the representation of the half resolution neighborhood
        // |-----|-----|-----|
        // |     |     |     |
        // |-----|-----|-----|
        // |     |     |     |
        // |-----|-----|-----|
        // |     |     |     |
        // |-----|-----|-----|

        // This is the representation of the full resolution neighborhood
        // |-----|-----|-----|
        // |     |     |     |
        // |-----|--|--|-----|
        // |     |--|--|     |
        // |-----|--|--|-----|
        // |     |     |     |
        // |-----|-----|-----|

        // The base is centered at (0, 0) at the center of the center pixel:
        // The 4 full res pixels are centered {L->R, T->B} at {-0.25, -0.25}, {0.25, -0.25}
        //                                                    {-0.25, 0.25}, {0.25, 0.25}
        //
        // The 9 half res pixels are placed {L->R, T->B} at {-1.0, -1.0}, {0.0, -1.0}, {1.0, -1.0}
        //                                                  {-1.0, 0.0}, {0.0, 0.0}, {1.0, 0.0}
        //                                                  {-1.0, 1.0}, {0.0, 1.0}, {1.0, 1.0}

        // Set of pre-generated weights (L->R, T->B). After experimentation, the final weighting function is exp(-distance^2)
        static internal float[] distanceBasedWeights_3x3 = new float[] { 0.324652f, 0.535261f, 0.119433f, 0.535261f, 0.882497f, 0.196912f, 0.119433f, 0.196912f, 0.0439369f,
                                                              0.119433f, 0.535261f, 0.324652f, 0.196912f, 0.882497f, 0.535261f, 0.0439369f, 0.196912f, 0.119433f,
                                                              0.119433f, 0.196912f, 0.0439369f, 0.535261f, 0.882497f, 0.196912f, 0.324652f, 0.535261f, 0.119433f,
                                                              0.0439369f, 0.196912f, 0.119433f, 0.196912f, 0.882497f, 0.535261f, 0.119433f, 0.535261f, 0.324652f};

        // Set of pre-generated weights (L->R, T->B). After experimentation, the final weighting function is exp(-distance^2)
        static internal float[] distanceBasedWeights_2x2 = new float[] {  0.324652f, 0.535261f, 0.535261f, 0.882497f,
                                                                0.535261f, 0.324652f, 0.882497f, 0.535261f,
                                                                0.535261f, 0.882497f, 0.324652f, 0.535261f,
                                                                0.882497f, 0.535261f, 0.535261f, 0.324652f};

        static internal float[] tapOffsets_2x2 = new float[] { -1.0f, -1.0f, 0.0f, -1.0f, -1.0f, 0.0f, 0.0f, 0.0f,
                                                                0.0f, -1.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f,
                                                                -1.0f, 0.0f, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 1.0f,
                                                                0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f};
    }


    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesBilateralUpsample
    {
        // Half resolution we are up sampling from
        public Vector4 _HalfScreenSize;

        // Weights used for the bilateral up sample
        [HLSLArray(3 * 4, typeof(Vector4))]
        public fixed float _DistanceBasedWeights[12 * 4];

        // Offsets used to tap into the half resolution neighbors
        [HLSLArray(2 * 4, typeof(Vector4))]
        public fixed float _TapOffsets[8 * 4];
    }
}
