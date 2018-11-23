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
            [Tooltip("The coordinate in the noise field to take the sample from.")]
            public float coordinate = 0.0f;
        }

        public class InputProperties2D
        {
            [Tooltip("The coordinate in the noise field to take the sample from.")]
            public Vector2 coordinate = Vector2.zero;
        }

        public class InputProperties3D
        {
            [Tooltip("The coordinate in the noise field to take the sample from.")]
            public Vector3 coordinate = Vector3.zero;
        }

        public class InputPropertiesCommon
        {
            [Min(0.0f), Tooltip("The frequency of the noise.")]
            public float frequency = 1.0f;
            [/*Range(1, 8),*/ Tooltip("The number of layers of noise.")]
            public int octaves = 1;
            [Range(0.0f, 1.0f), Tooltip("The scaling factor applied to each octave.")]
            public float persistence = 0.5f;
            [Tooltip("The rate of change of the frequency for each successive octave.")]
            public float lacunarity = 2.0f;
        }

        public enum NoiseType
        {
            Value,
            Perlin,
            Cellular
        }

        [VFXSetting, Tooltip("The noise algorithm.")]
        public NoiseType type = NoiseType.Perlin;
    }
}
