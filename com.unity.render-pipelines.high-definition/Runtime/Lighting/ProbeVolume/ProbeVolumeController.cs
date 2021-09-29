using UnityEngine.Rendering;
using UnityEngine.Serialization;
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Lighting/Experimental/Probe Volume")]
    internal class ProbeVolumeController : VolumeComponent
    {
        [Serializable]
        internal sealed class LeakMitigationModeParameter : VolumeParameter<LeakMitigationMode>
        {
            public LeakMitigationModeParameter(LeakMitigationMode value, bool overrideState = false)
                : base(value, overrideState) {}
        }

        [SerializeField, Tooltip("The global distance fade start, applied on top of per Probe Volume distance fade start.")]
        internal FloatParameter distanceFadeStart = new FloatParameter(200.0f);

        [SerializeField, Tooltip("The global distance fade end, applied on top of per Probe Volume distance fade end.")]
        internal FloatParameter distanceFadeEnd = new FloatParameter(300.0f);

        [SerializeField, Tooltip("Selects the heuristic used for mitigating light leaking and self-shadowing artifacts when sampling from the probe volumes.")]
        internal LeakMitigationModeParameter leakMitigationMode = new LeakMitigationModeParameter(LeakMitigationMode.NormalBias);

        [SerializeField, Tooltip("Controls the strength of our bilateral filter. 0.0 falls back to trilinear filtering. 1.0 is maximum cross term (geometric or validity).")]
        internal ClampedFloatParameter bilateralFilterWeight = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [SerializeField, Tooltip("Controls the minimum weight of our octahedral depth filter term. 0.0 means a probe can be fully rejected based on octahedral depth. 1.0 means a probe will never be rejected based on octahedral depth. Low, but non-zero values are reccomended.")]
        internal ClampedFloatParameter octahedralDepthWeightMin = new ClampedFloatParameter(0.05f, 0.0f, 1.0f);

        [SerializeField, Tooltip("Controls the sharpness of our octahedral depth filter. 0.0 is the least sharp transition. Can result in light leaks. 1.0 is the sharpest transition. Can result in sharp edge artifacts. High, but less than 1 values are reccomended.")]
        internal ClampedFloatParameter octahedralDepthLightBleedReductionThreshold = new ClampedFloatParameter(0.99f, 0.0f, 0.9999f);

        [SerializeField, Tooltip("Controls how much reflection probe reflections are scaled to approximately match probe volume brightness.\nEnabling can help mitigate specular glowing in dark corners.\nA value of 1.0 means reflections in fully black corners can become fully black (zero).\nA value of 0.0 means reflections will never be darkened or lightened.")]
        internal ClampedFloatParameter reflectionProbeNormalizationWeight = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [SerializeField, Tooltip("Controls how much of the spherical harmonic directionality is used in normalization. If set to 0.0, reflection probe normalization will use the average incoming light from all directions for rescaling.\nIf set to 1.0, it will use all spherical harmonic data to sample light along the view direction.\n1.0 is theoretically most accurate, but can produce poor results (explosions) if SH terms contain strong ringing, which is fairly common.\nStart at 1.0 and dial down until you are happy with the results.")]
        internal ClampedFloatParameter reflectionProbeNormalizationDirectionality = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [SerializeField, Tooltip("If enabled, reflection probe reflections will never be brightened by bright probe volume samples. They will only be darkened by dark probe volume samples.\nThis can be useful if probe volume data contains strong hotspots that blow out reflections.")]
        internal BoolParameter reflectionProbeNormalizationDarkenOnly = new BoolParameter(false);

        ProbeVolumeController()
        {
            displayName = "Probe Volume Controller (Experimental)";
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
