using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    abstract class NoiseBase : VFXOperator
    {
        public class InputPropertiesCommon
        {
            [Tooltip("The magnitude of the noise.")]
            public float amplitude = 1.0f;
            [Min(0.0f), Tooltip("The frequency of the noise.")]
            public float frequency = 1.0f;
            [/*Range(1, 8),*/ Tooltip("The number of layers of noise.")]
            public int octaves = 1;
            [Range(0.0f, 1.0f), Tooltip("The scaling factor applied to each octave.")]
            public float persistence = 0.5f;
        }

        public class OutputProperties
        {
            public float o;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = PropertiesFromType("InputProperties");
                properties = properties.Concat(PropertiesFromType("InputPropertiesCommon"));
                return properties;
            }
        }
    }
}
