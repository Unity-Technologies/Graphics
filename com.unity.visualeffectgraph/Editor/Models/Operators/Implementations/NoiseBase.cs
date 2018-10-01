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
            public float o = 0.0f;
        }

        public enum DimensionCount
        {
            One,
            Two,
            Three
        }

        [VFXSetting, Tooltip("Controls whether particles are spawned on the base of the cone, or throughout the entire volume.")]
        public DimensionCount dimensions = DimensionCount.One;

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                IEnumerable<VFXPropertyWithValue> properties = null;
                if (dimensions == DimensionCount.One)
                    properties = PropertiesFromType("InputProperties1D");
                else if (dimensions == DimensionCount.Two)
                    properties = PropertiesFromType("InputProperties2D");
                else
                    properties = PropertiesFromType("InputProperties3D");

                properties = properties.Concat(PropertiesFromType("InputPropertiesCommon"));
                return properties;
            }
        }
    }
}
