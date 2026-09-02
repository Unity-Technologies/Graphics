using UnityEngine;
using UnityEditor.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ScreenSpaceLensFlare))]
    class ScreenSpaceLensFlareEditor : VolumeComponentEditor
    {
        protected SerializedDataParameter m_Intensity;
        protected SerializedDataParameter m_TintColor;
        protected SerializedDataParameter m_BloomMip;
        protected SerializedDataParameter m_FirstFlareIntensity;
        protected SerializedDataParameter m_SecondaryFlareIntensity;
        protected SerializedDataParameter m_WarpedFlareIntensity;
        protected SerializedDataParameter m_WarpedFlareScale;
        protected SerializedDataParameter m_Samples;
        protected SerializedDataParameter m_SampleDimmer;
        protected SerializedDataParameter m_VignetteEffect;
        protected SerializedDataParameter m_StartingPosition;
        protected SerializedDataParameter m_Scale;
        protected SerializedDataParameter m_ChromaticAberrationIntensity;
        protected SerializedDataParameter m_StreaksIntensity;
        protected SerializedDataParameter m_StreaksLength;
        protected SerializedDataParameter m_StreaksOrientation;
        protected SerializedDataParameter m_StreaksThreshold;
        protected SerializedDataParameter m_Resolution;

        static GUIContent s_Intensity = L10n.TextContent("Intensity", "Sets the global intensity of the Screen Space Lens Flare effect. When set to 0, the pass is skipped. ", null, null);
        static GUIContent s_TintColor = L10n.TextContent("Tint Color", "Sets the color used to tint all the flares. ", null, null);
        static GUIContent s_BloomMip = L10n.TextContent("Bloom Mip Bias", "Controls the Bloom mip used as a source for the Lens Flare effect. A high value will result in a blurrier result for all the flares.", null, null);
        static GUIContent s_FirstFlareIntensity = L10n.TextContent("Regular Multiplier", "Controls the intensity of the Regular Flare sample. Those flares are sampled using scaled screen coordinates.", null, null);
        static GUIContent s_SecondaryFlareIntensity = L10n.TextContent("Reversed Multiplier", "Controls the intensity of the Reversed Flare sample. Those flares are sampled using scaled and flipped screen coordinates.", null, null);
        static GUIContent s_WarpedFlareIntensity = L10n.TextContent("Warped Multiplier", "Controls the intensity of the Warped Flare sample. Those flares are sampled using polar screen coordinates.", null, null);
        static GUIContent s_WarpedFlareScale = L10n.TextContent("Scale", "Sets the scale of the Warped Flare sample. A value of 1,1 will keep this flare circular. Set values below 1 can lead to stretching artifacts on the side of the screen.", null, null);
        static GUIContent s_Samples = L10n.TextContent("Samples", "Controls the number of times the flare effect is repeated for each flare type (regular, reversed, warped). This parameter has a strong impact on performance.", null, null);
        static GUIContent s_SampleDimmer = L10n.TextContent("Sample Dimmer", "Controls the value by which each additionnal sample is multiplied. This parameter has an effect only after the first sample.", null, null);
        static GUIContent s_VignetteEffect = L10n.TextContent("Vignette Effect", "Controls the intensity of the vignette effect to occlude the Lens Flare effect at the center of the screen.", null, null);
        static GUIContent s_StartingPosition = L10n.TextContent("Starting Position", "Controls the starting position of the flares in screen space relative to their source.", null, null);
        static GUIContent s_Scale = L10n.TextContent("Scale", "Controls the scale at which the flares are sampled. This parameter only impacts regular, reversed and warped flares.", null, null);
        static GUIContent s_ChromaticAberrationIntensity = L10n.TextContent("Intensity", "Controls the strength of the Chromatic Aberration effect. The higher the value, the more light is dispersed on the sides of the screen.", null, null);
        static GUIContent s_StreaksIntensity = L10n.TextContent("Multiplier", "Controls the intensity of streaks effect. This effect has an impact on performance when above zero. When this intensity is zero, this effect is not evaluated to save costs.", null, null);
        static GUIContent s_StreaksOrientation = L10n.TextContent("Orientation", "Controls the orientation streaks effect in degrees. A value of 0 creates horizontal streaks.", null, null);
        static GUIContent s_StreaksLength = L10n.TextContent("Length", "Controls the length of streaks effect. A value of one creates streaks about the width of the screen.", null, null);
        static GUIContent s_StreaksThreshold = L10n.TextContent("Threshold", "Controls the threshold of streak effect. A high value makes the effect more localised on the high intensity areas of the screen. When using a non HDR Camera, a high threshold can remove streaks entirely.", null, null);
        static GUIContent s_Resolution = L10n.TextContent("Resolution", "Specifies the resolution ratio at which the streaks effect is computed.", null, null);

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceLensFlare>(serializedObject);

            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_TintColor = Unpack(o.Find(x => x.tintColor));
            m_BloomMip = Unpack(o.Find(x => x.bloomMip));
            m_FirstFlareIntensity = Unpack(o.Find(x => x.firstFlareIntensity));
            m_SecondaryFlareIntensity = Unpack(o.Find(x => x.secondaryFlareIntensity));
            m_WarpedFlareIntensity = Unpack(o.Find(x => x.warpedFlareIntensity));
            m_WarpedFlareScale = Unpack(o.Find(x => x.warpedFlareScale));
            m_Samples = Unpack(o.Find(x => x.samples));
            m_SampleDimmer = Unpack(o.Find(x => x.sampleDimmer));
            m_VignetteEffect = Unpack(o.Find(x => x.vignetteEffect));
            m_StartingPosition = Unpack(o.Find(x => x.startingPosition));
            m_Scale = Unpack(o.Find(x => x.scale));
            m_ChromaticAberrationIntensity = Unpack(o.Find(x => x.chromaticAberrationIntensity));
            m_StreaksIntensity = Unpack(o.Find(x => x.streaksIntensity));
            m_StreaksLength = Unpack(o.Find(x => x.streaksLength));
            m_StreaksOrientation = Unpack(o.Find(x => x.streaksOrientation));
            m_StreaksThreshold = Unpack(o.Find(x => x.streaksThreshold));
            m_Resolution = Unpack(o.Find(x => x.resolution));

        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Intensity, s_Intensity);
            PropertyField(m_TintColor, s_TintColor);
            PropertyField(m_BloomMip, s_BloomMip);

            //Regular Flares
            PropertyField(m_FirstFlareIntensity, s_FirstFlareIntensity);
            PropertyField(m_SecondaryFlareIntensity, s_SecondaryFlareIntensity);
            PropertyField(m_WarpedFlareIntensity, s_WarpedFlareIntensity);

            if(showAdditionalProperties)
            {
                using (new IndentLevelScope())
                {
                    PropertyField(m_WarpedFlareScale, s_WarpedFlareScale);
                }
            }

            //Parameters for Regular Flares
            PropertyField(m_Samples, s_Samples);
            if(showAdditionalProperties)
            {
                using (new IndentLevelScope())
                {
                    PropertyField(m_SampleDimmer, s_SampleDimmer);
                }
            }
            PropertyField(m_VignetteEffect, s_VignetteEffect);
            PropertyField(m_StartingPosition, s_StartingPosition);
            PropertyField(m_Scale, s_Scale);

            //Streaks
            PropertyField(m_StreaksIntensity, s_StreaksIntensity);
            using (new IndentLevelScope())
            {
                PropertyField(m_StreaksLength, s_StreaksLength);
                PropertyField(m_StreaksOrientation, s_StreaksOrientation);
                PropertyField(m_StreaksThreshold, s_StreaksThreshold);
                PropertyField(m_Resolution, s_Resolution);
            }

            //Chromatic Aberration
            PropertyField(m_ChromaticAberrationIntensity, s_ChromaticAberrationIntensity);
        }

    }
}
