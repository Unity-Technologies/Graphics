using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{

    public class IesAsset : ScriptableObject
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
    
        public enum Tilt
        {
            None,
            Include,
        }

        [Line(1, @"(^IESNA:.*)", required: false)]
        public string           fileVersion;
        [Line(2, @"(^\[[A-Z]+\].*)")]
        public List<string>     keywords;

        [Line(3, @"TILT=(\w+)")]
        public Tilt             tilt;
        [Line(4), SkipIfEqual("tilt", "None")]
        public float            lampToLuminaireGeometry;
        [Line(5), SkipIfEqual("tilt", "None")]
        public int              pairAnglesAndMultipyFactorCount;
        [Line(6), SkipIfEqual("tilt", "None")]
        public List<float>      angles;
        [Line(7), SkipIfEqual("tilt", "None")]
        public List<float>      multiplyingFactors;

        [Line(8)]
        public int              lampCount;
        [Line(8)]
        public float            lumenPerLamp;
        [Line(8)]
        public float            candelaMultiplier;
        [Line(8)]
        public int              verticalAnglesCount;
        [Line(8)]
        public int              horizontalAnglesCount;
        [Line(8)]
        public PhotometricType  photometricType;
        [Line(8)]
        public UnitType         unitType;
        [Line(8)]
        public float            luminousWidth;
        [Line(8)]
        public float            luminousLength;
        [Line(8)]
        public float            luminousheight;

        [Line(10)]
        public float            ballastFactor;
        [Line(10)]
        public float            ballastLampPhotometricFactor;
        [Line(10)]
        public float            inputWatts;

        [Line(15, start: @"^(0|0.[0+])", stop: @"(180|180.[0+])")]
        public List<float>      verticalAngles;
        [Line(16, start: @"^(0|0.[0+])", stop: @"(180|180.[0+])")]
        public List<float>      horizontalAngles;
        [Line(17, start: @".*", stop: @"^$")] // Keep looping until we encounter the end of the file
        public List<float>      intensityValues; // In candella
    }
}