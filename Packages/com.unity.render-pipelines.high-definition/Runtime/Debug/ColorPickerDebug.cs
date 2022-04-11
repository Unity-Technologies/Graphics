using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Color Picker Debug Mode.
    /// </summary>
    [GenerateHLSL]
    public enum ColorPickerDebugMode
    {
        /// <summary>No color picking debug.</summary>
        None,
        /// <summary>One byte display (0-255).</summary>
        Byte,
        /// <summary>Four bytes display (0-255).</summary>
        Byte4,
        /// <summary>One float display.</summary>
        Float,
        /// <summary>Four floats display.</summary>
        Float4,
    }

    /// <summary>
    /// Color Pcker debug settings.
    /// </summary>
    [Serializable]
    public class ColorPickerDebugSettings
    {
        /// <summary>
        /// Color picker mode.
        /// </summary>
        public ColorPickerDebugMode colorPickerMode = ColorPickerDebugMode.None;
        /// <summary>
        /// Color of the font used for color picker display.
        /// </summary>
        public Color fontColor = new Color(1.0f, 0.0f, 0.0f);
    }
}
