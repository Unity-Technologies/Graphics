using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    abstract class NoiseBase : VFXOperator
    {
        public class InputProperties1D
        {
            [Tooltip("Sets the coordinate in the noise field to take the sample from.")]
            public float coordinate = 0.0f;
        }

        public class InputProperties2D
        {
            [Tooltip("Sets the coordinate in the noise field to take the sample from.")]
            public Vector2 coordinate = Vector2.zero;
        }

        public class InputProperties3D
        {
            [Tooltip("Sets the coordinate in the noise field to take the sample from.")]
            public Vector3 coordinate = Vector3.zero;
        }

        public class InputPropertiesCommon
        {
            [Tooltip("Sets the period in which the noise is sampled. Higher frequencies result in more frequent noise change.")]
            public float frequency = 1.0f;
            [/*Range(1, 8),*/ Tooltip("Sets the number of layers of noise. More octaves create a more varied look, but are also more expensive to calculate.")]
            public int octaves = 1;
            [Range(0, 1), Tooltip("Sets the scaling factor applied to each octave (also known as persistence.) ")]
            public float roughness = 0.5f;
            [Min(0), Tooltip("Sets the rate of change of the frequency for each successive octave. A lacunarity value of 1 results in each octave having the same frequency. Higher values result in more details, and values below 1 produce less details.")]
            public float lacunarity = 2.0f;
        }

        public enum NoiseType
        {
            Value,
            Perlin,
            Cellular
        }

        [VFXSetting, Tooltip("Specifies the algorithm used to generate the noise field.")]
        public NoiseType type = NoiseType.Perlin;
    }
}
