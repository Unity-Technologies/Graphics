using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public enum PhotometricType
    {
        A = 1,
        B = 2,
        C = 3,
    }

    public enum UnitType
    {
        Feet    = 1,
        Meter   = 2,
    }

    public class IesAsset : ScriptableObject
    {
        public string[]         keywords;
        public int              clampCount;
        public float            lumenPerLamp;
        public float            candelaMultiplier;
        public int              verticalAnglesCount;
        public int              horizontalAnglesCount;
        public PhotometricType  photometricType;
        public UnitType         unitType;
        public Vector3          luminousDimensions;

        public float            ballastFactor;
        public float            inputWatts;

        public List<float>      verticalAngles;
        public List<float>      horizontalAngles;
        public List<float>      intensityValues; // In candella
    }
}