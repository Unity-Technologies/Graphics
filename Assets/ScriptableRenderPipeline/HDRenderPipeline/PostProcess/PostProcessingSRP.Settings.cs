using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    partial class PostProcessing
    {
        [Serializable]
        public sealed class ColorGradingSettings
        {
            public enum GradingType
            {
                None,
                Neutral,
                Custom
            }

            public GradingType type = GradingType.None;
            public float exposure = 0f;

            public Texture logLut = null;

            [Range(-0.10f,  0.1f)] public float neutralBlackIn    = 0.02f;
            [Range( 1.00f, 20.0f)] public float neutralWhiteIn    = 10f;
            [Range(-0.09f,  0.1f)] public float neutralBlackOut   = 0f;
            [Range( 1.00f, 19.0f)] public float neutralWhiteOut   = 10f;
            [Range( 0.10f, 20.0f)] public float neutralWhiteLevel = 5.3f;
            [Range( 1.00f, 10.0f)] public float neutralWhiteClip  = 10f;
        }

        [Serializable]
        public sealed class EyeAdaptationSettings
        {
            public enum EyeAdaptationType
            {
                Progressive,
                Fixed
            }

            public bool enabled = false;
            public bool showDebugHistogramInGameView = true;

            [Range(1f, 99f)] public float lowPercent = 65f;
            [Range(1f, 99f)] public float highPercent = 95f;

            public float minLuminance = 0.03f;
            public float maxLuminance = 2f;
            public float exposureCompensation = 0.5f;

            public EyeAdaptationType adaptationType = EyeAdaptationType.Progressive;

            public float speedUp = 2f;
            public float speedDown = 1f;

            [Range(-16, -1)] public int logMin = -8;
            [Range(  1, 16)] public int logMax = 4;
        }

        [Serializable]
        public sealed class ChromaticAberrationSettings
        {
            public bool enabled = false;
            public Texture spectralTexture;
            [Range(0f, 1f)] public float intensity = 0.1f;
        }

        [Serializable]
        public sealed class VignetteSettings
        {
            public bool enabled = false;

            [ColorUsage(false, true, 0f, 10f, 0f, 10f)]
            public Color color = new Color(0f, 0f, 0f, 1f);

            public Vector2 center = new Vector2(0.5f, 0.5f);

            [Range(0f, 1f)] public float intensity = 0.3f;
            [Range(0f, 1f)] public float smoothness = 0.3f;
        }
    }
}
