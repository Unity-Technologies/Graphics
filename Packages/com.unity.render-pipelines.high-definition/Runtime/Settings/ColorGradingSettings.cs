using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.HighDefinition
{
    enum ColorGradingSpace
    {
        AcesCg = 0,
        [InspectorName("sRGB")]
        sRGB        // Legacy.
    }

    [Serializable]
    [Category("Miscellaneous")]
    [HideInInspector] // TODO Remove when UI has fully being migrated
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    class ColorGradingSettings : IRenderPipelineGraphicsSettings
    {
        #region Version
        internal enum Version : int
        {
            Initial = 0,
        }

        [SerializeField][HideInInspector]
        private Version m_Version;

        /// <summary>Current version.</summary>
        public int version => (int)m_Version;
        #endregion

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        #region SerializeFields

        [SerializeField]
        [InspectorName("Color Grading Space")]
        [Tooltip("Set the color space in which color grading is performed. If ACES is used as tonemapper, the grading always happens in ACEScg. sRGB will lead to rendering in a non-wide color gamut, while ACEScg is a wider color gamut that will allow to exploit the wide color gamut on UHD TV when outputting in HDR.")]
        private ColorGradingSpace m_ColorGradingSpace;
        #endregion

        #region Data Accessors

        /// <summary>
        /// Set the color space in which color grading is performed. If ACES is used as tonemapper, the grading always happens in ACEScg. sRGB will lead to rendering in a non-wide color gamut, while ACEScg is a wider color gamut that will allow to exploit the wide color gamut on UHD TV when outputting in HDR.
        /// </summary>
        public ColorGradingSpace space
        {
            get => m_ColorGradingSpace;
            set => this.SetValueAndNotify(ref m_ColorGradingSpace, value);
        }

        public string GetColorGradingSpaceKeyword()
        {
            return space switch
            {
                ColorGradingSpace.sRGB => "GRADE_IN_SRGB",
                ColorGradingSpace.AcesCg => "GRADE_IN_ACESCG",
                _ => throw new NotImplementedException($"Missing case entry for {space}")
            };
        }

        #endregion
    }
}
