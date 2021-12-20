using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace WaterSystem.Data
{
    /// <summary>
    /// This scriptable object contains setting for how the water looks visually
    /// </summary>
    [System.Serializable][CreateAssetMenu(fileName = "WaterSurfaceData", menuName = "WaterSystem/Surface Data", order = 0)]
    public class WaterSurfaceData : ScriptableObject
    {
        public float _waterMaxVisibility = 40.0f;
        public Gradient _absorptionRamp;
        public Gradient _scatterRamp;
        public List<Wave> _waves = new List<Wave>();
        public bool _customWaves = false;
        public int randomSeed = 3234;
        public BasicWaves _basicWaveSettings = new BasicWaves(1.5f, 45.0f, 5.0f);
        public FoamSettings _foamSettings = new FoamSettings();
        [SerializeField]
        public bool _init = false;
    }

    [System.Serializable]
    public struct Wave
    {
        public float amplitude; // height of the wave in units(m)
        public float direction; // direction the wave travels in degrees from Z+
        public float wavelength; // distance between crest>crest
        public float2 origin; // Omi directional point of origin
        public float onmiDir; // Is omni?

        public Wave(float amp, float dir, float length, float2 org, bool omni)
        {
            amplitude = amp;
            direction = dir;
            wavelength = length;
            origin = org;
            onmiDir = omni ? 1 : 0;
        }
    }

    [System.Serializable]
    public class BasicWaves
    {
        public int numWaves = 6;
        public float amplitude;
        public float direction;
        public float wavelength;

        public BasicWaves(float amp, float dir, float len)
        {
            numWaves = 6;
            amplitude = amp;
            direction = dir;
            wavelength = len;
        }
    }

    [System.Serializable]
    public class FoamSettings
    {
        public int foamType; // 0=default, 1=simple, 3=custom
        public AnimationCurve basicFoam;
        public AnimationCurve liteFoam;
        public AnimationCurve mediumFoam;
        public AnimationCurve denseFoam;

        // Foam curves
        public FoamSettings()
        {
            foamType = 0;
            basicFoam = new AnimationCurve(new Keyframe[2]{new Keyframe(0.25f, 0f),
                                                                    new Keyframe(1f, 1f)});
            liteFoam = new AnimationCurve(new Keyframe[3]{new Keyframe(0.2f, 0f),
                                                                    new Keyframe(0.4f, 1f),
                                                                    new Keyframe(0.7f, 0f)});
            mediumFoam = new AnimationCurve(new Keyframe[3]{new Keyframe(0.4f, 0f),
                                                                    new Keyframe(0.7f, 1f),
                                                                    new Keyframe(1f, 0f)});
            denseFoam = new AnimationCurve(new Keyframe[2]{new Keyframe(0.7f, 0f),
                                                                    new Keyframe(1f, 1f)});
        }
    }
}