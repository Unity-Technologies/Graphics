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
        [Line(2, match: @"(^\[[\w+]+\].*)", maxLines: -1)]
        public List<string>     keywords;
        
        [Line(3, start: @"^", stop: @"^TILT=")]
        public List<string>     infos;

        [Line(4, @"^TILT=(\w+)")]
        public Tilt             tilt;
        [Line(5), SkipIfEqual("tilt", "None")]
        public int              lampToLuminaireGeometry;
        [Line(6), SkipIfEqual("tilt", "None")]
        public int              pairAnglesAndMultipyFactorCount;
        [Line(7), SkipIfEqual("tilt", "None")]
        public List<float>      angles;
        [Line(8), SkipIfEqual("tilt", "None")]
        public List<float>      multiplyingFactors;

        [Line(9)]
        public int              lampCount;
        [Line(9)]
        public float            lumenPerLamp;
        [Line(9)]
        public float            candelaMultiplier;
        [Line(9)]
        public int              verticalAnglesCount;
        [Line(9)]
        public int              horizontalAnglesCount;
        [Line(9)]
        public PhotometricType  photometricType;
        [Line(9)]
        public UnitType         unitType;
        [Line(9)]
        public float            luminousWidth;
        [Line(9)]
        public float            luminousLength;
        [Line(9)]
        public float            luminousheight;

        [Line(10)]
        public float            ballastFactor;
        [Line(10)]
        public float            ballastLampPhotometricFactor;
        [Line(10)]
        public float            inputWatts;

        [Line(15)]
        public List<float>      verticalAngles;
        [Line(16)]
        public List<float>      horizontalAngles;
        [Line(17, start: @".*", stop: @"^$")] // Keep looping until we encounter the end of the file
        public List<float>      intensityValues; // In candella
    }
}