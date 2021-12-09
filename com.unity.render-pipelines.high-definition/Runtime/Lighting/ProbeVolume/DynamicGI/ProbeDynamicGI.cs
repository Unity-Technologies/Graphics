using System;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEditor.Experimental;
using Unity.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

[assembly: InternalsVisibleTo("Unity.Entities.Hybrid.HybridComponents")]
[assembly: InternalsVisibleTo("Unity.Rendering.Hybrid")]

namespace UnityEngine.Rendering.HighDefinition
{
    public class ProbeDynamicGI : VolumeComponent
    {
        [Tooltip("Indirect multiplier for all lights affect Dynamic GI")]
        public ClampedFloatParameter indirectMultiplier = new ClampedFloatParameter(1f, 0.0f, 2f);
        [Tooltip("Multiplier for material emissive colors that affect Dynamic GI")]
        public ClampedFloatParameter bakedEmissionMultiplier = new ClampedFloatParameter(0f, 0f, 2f);
        [Tooltip("A control for blending in more influence of infinite bounce light near surfaces")]
        public ClampedFloatParameter infiniteBounce = new ClampedFloatParameter(1f, 0.0f, 2f);

        [Tooltip("Max range to perform dynamic GI operation on an individual probe")]
        public ClampedFloatParameter rangeInFrontOfCamera = new ClampedFloatParameter(50.0f, 0.0f, 100.0f);
        [Tooltip("Max range to perform dynamic GI operation on an individual probe")]
        public ClampedFloatParameter rangeBehindCamera = new ClampedFloatParameter(25.0f, 0.0f, 100.0f);

        [Tooltip("Advanced control for the contribution amount of direct light")]
        public ClampedFloatParameter directContribution = new ClampedFloatParameter(1f, 0.5f, 2);
        [Tooltip("Advanced control for the contribution amount of secondary propagation indirect light")]
        public ClampedFloatParameter propagationContribution = new ClampedFloatParameter(1f, 0.5f, 2);
        [Tooltip("Advanced control for the SG sharpness used when propagating light")]
        public ClampedFloatParameter propagationSharpness = new ClampedFloatParameter(2.0f, 0.0f, 16.0f);
        [Tooltip("Advanced control for the SG sharpness used when evaluating the influence of infinite bounce light near surfaces")]
        public ClampedFloatParameter infiniteBounceSharpness = new ClampedFloatParameter(2.0f, 0.0f, 16.0f);
        [Tooltip("Advanced control for probe propagation combine pass.\nSamplePeakAndProject: Spherical gaussians will simply be evaluated at their peak and projected to convert to spherical harmonics.\nSHFromSGFit: A spherical gaussian to spherical harmonic function fit is used, which is physically plausible.\nSHFromSGFitWithCosineWindow: A spherical gaussian with an additional cosine window to spherical harmonic function fit is used, which is physically plausible. Less directional blur than SHFromSGFit.")]
        public SHFromSGModeParameter shFromSGMode = new SHFromSGModeParameter(SHFromSGMode.SamplePeakAndProject);
        [Tooltip("Advanced control for darkening down the indirect light on invalid probes")]
        public ClampedFloatParameter leakMultiplier = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
        [Tooltip("Advanced control to bias the distance from the normal of the hit surface to perform direct lighting evaluation on")]
        public ClampedFloatParameter bias = new ClampedFloatParameter(0.05f, 0.0f, 0.33f);
        [Tooltip("Advanced control for how probes at volume boundaries propagate light from neighboring Probe Volumes.\n\nDisabled: No light is propagated across neighbors.\n\nSample Neighbors Direction Only: Samples all probe volumes once per probe and evaluates light from the resulting probe data for each axis.\n\nSample Neighbors Position and Direction: Samples all probe volumes for all propagation axes at each axes neighboring probe position.\nPotentially more accurate than Sample Neighbors Direction Only, but significantly more expensive.")]
        public DynamicGINeighboringVolumePropagationModeParameter neighborVolumePropagationMode = new DynamicGINeighboringVolumePropagationModeParameter(DynamicGINeighboringVolumePropagationMode.SampleNeighborsDirectionOnly);

        [Tooltip("Debug Contribution control for mixing in baked indirect lighting")]
        public ClampedFloatParameter bakeAmount = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        [Tooltip("Debug Contribution control for mixing in dynamic indirect lighting")]
        public ClampedFloatParameter dynamicAmount = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [Tooltip("Advanced control to clear all dynamic GI buffers in the event lighting blows up when tuning")]
        public BoolParameter clear = new BoolParameter(false);

        [Serializable]
        public enum SHFromSGMode
        {
            SamplePeakAndProject = 0,
            SHFromSGFit,
            SHFromSGFitWithCosineWindow
        };

        [Serializable]
        public sealed class SHFromSGModeParameter : VolumeParameter<SHFromSGMode>
        {
            public SHFromSGModeParameter(SHFromSGMode value, bool overrideState = false)
                : base(value, overrideState) {}
        }

        [Serializable]
        public enum DynamicGINeighboringVolumePropagationMode
        {
            Disabled = 0,
            SampleNeighborsDirectionOnly,
            SampleNeighborsPositionAndDirection
        };

        [Serializable]
        public sealed class DynamicGINeighboringVolumePropagationModeParameter : VolumeParameter<DynamicGINeighboringVolumePropagationMode>
        {
            public DynamicGINeighboringVolumePropagationModeParameter(DynamicGINeighboringVolumePropagationMode value, bool overrideState = false)
                : base(value, overrideState) { }
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
