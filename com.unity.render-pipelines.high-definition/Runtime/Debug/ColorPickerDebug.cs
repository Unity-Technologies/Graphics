using System;

namespace UnityEngine.Rendering.HighDefinition
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

        public void OnValidate()
        {
        }
    }
}
