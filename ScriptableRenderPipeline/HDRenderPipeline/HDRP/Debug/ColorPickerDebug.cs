using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public enum ColorPickerDebugMode
    {
        None,
        Byte,
        Byte4,
        Float,
        Float4,
    }

    [Serializable]
    public class ColorPickerDebugSettings
    {
        public ColorPickerDebugMode colorPickerMode = ColorPickerDebugMode.None;
        public Color fontColor = new Color(1.0f, 0.0f, 0.0f);

        public float colorThreshold0 = 0.0f;
        public float colorThreshold1 = 200.0f;
        public float colorThreshold2 = 9000.0f;
        public float colorThreshold3 = 10000.0f;

        public void OnValidate()
        {
        }
    }
}
