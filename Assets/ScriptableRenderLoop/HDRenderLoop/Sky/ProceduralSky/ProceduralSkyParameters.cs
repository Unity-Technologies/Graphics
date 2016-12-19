using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class ProceduralSkyParameters
        : SkyParameters
    {
        public enum OcclusionDownscale { x1 = 1, x2 = 2, x4 = 4 }
        public enum OcclusionSamples { x64 = 0, x164 = 1, x244 = 2 }
        public enum DepthTexture { Enable, Disable/*, Ignore*/ } // 'Ignore' appears to be currently unused.
        public enum ScatterDebugMode { None, Scattering, Occlusion, OccludedScattering, Rayleigh, Mie, Height }

        [Header("Global Settings")]
        public Gradient worldRayleighColorRamp = null;
        public float worldRayleighColorIntensity = 1f;
        public float worldRayleighDensity = 10f;
        public float worldRayleighExtinctionFactor = 1.1f;
        public float worldRayleighIndirectScatter = 0.33f;
        public Gradient worldMieColorRamp = null;
        public float worldMieColorIntensity = 1f;
        public float worldMieDensity = 15f;
        public float worldMieExtinctionFactor = 0f;
        public float worldMiePhaseAnisotropy = 0.9f;
        public float worldNearScatterPush = 0f;
        public float worldNormalDistance = 1000f;

        [Header("Height Settings")]
        public Color heightRayleighColor = Color.white;
        public float heightRayleighIntensity = 1f;
        public float heightRayleighDensity = 10f;
        public float heightMieDensity = 0f;
        public float heightExtinctionFactor = 1.1f;
        public float heightSeaLevel = 0f;
        public float heightDistance = 50f;
        public Vector3 heightPlaneShift = Vector3.zero;
        public float heightNearScatterPush = 0f;
        public float heightNormalDistance = 1000f;

        [Header("Sky Dome")]
        public Vector3 skyDomeRotation = Vector3.zero;
        public bool skyDomeVerticalFlip = false;
        public Cubemap skyDomeCubemap = null;
        public float skyDomeExposure = 1f;
        public Color skyDomeTint = Color.white;
        public Transform skyDomeTrackedYawRotation = null;

        /*
        [Header("Scatter Occlusion")]
        public bool               useOcclusion             = false;
        public float              occlusionBias            = 0f;
        public float              occlusionBiasIndirect    = 0.6f;
        public float              occlusionBiasClouds      = 0.3f;
        public OcclusionDownscale occlusionDownscale       = OcclusionDownscale.x2;
        public OcclusionSamples   occlusionSamples         = OcclusionSamples.x64;
        public bool               occlusionDepthFixup      = true;
        public float              occlusionDepthThreshold  = 25f;
        public bool               occlusionFullSky         = false;
        public float              occlusionBiasSkyRayleigh = 0.2f;
        public float              occlusionBiasSkyMie      = 0.4f;
        */

        [Header("Other")]
        public Shader atmosphericShader = null;
        // public Shader             occlusionShader       = null;
        public float worldScaleExponent = 1.0f;
        // public bool            forcePerPixel            = true;
        // public bool            forcePostEffect          = true;
        // [Tooltip("Soft clouds need depth values. Ignore means externally controlled.")]
        public DepthTexture depthTexture = DepthTexture.Enable;
        public ScatterDebugMode debugMode = ScatterDebugMode.None;

        // Camera   m_currentCamera;

        // UnityEngine.Rendering.CommandBuffer m_occlusionCmdAfterShadows, m_occlusionCmdBeforeScreen;

        public void OnValidate()
        {
            worldScaleExponent = Mathf.Clamp(worldScaleExponent, 1f, 2f);
            worldNormalDistance = Mathf.Clamp(worldNormalDistance, 1f, 10000f);
            worldNearScatterPush = Mathf.Clamp(worldNearScatterPush, -200f, 300f);
            worldRayleighDensity = Mathf.Clamp(worldRayleighDensity, 0, 1000f);
            worldMieDensity = Mathf.Clamp(worldMieDensity, 0f, 1000f);
            worldRayleighIndirectScatter = Mathf.Clamp(worldRayleighIndirectScatter, 0f, 1f);
            worldMiePhaseAnisotropy = Mathf.Clamp01(worldMiePhaseAnisotropy);

            heightNormalDistance = Mathf.Clamp(heightNormalDistance, 1f, 10000f);
            heightNearScatterPush = Mathf.Clamp(heightNearScatterPush, -200f, 300f);
            heightRayleighDensity = Mathf.Clamp(heightRayleighDensity, 0, 1000f);
            heightMieDensity = Mathf.Clamp(heightMieDensity, 0, 1000f);

            /*
            occlusionBias            = Mathf.Clamp01(occlusionBias);
            occlusionBiasIndirect    = Mathf.Clamp01(occlusionBiasIndirect);
            occlusionBiasClouds      = Mathf.Clamp01(occlusionBiasClouds);
            occlusionBiasSkyRayleigh = Mathf.Clamp01(occlusionBiasSkyRayleigh);
            occlusionBiasSkyMie      = Mathf.Clamp01(occlusionBiasSkyMie);
            */

            skyDomeExposure = Mathf.Clamp(skyDomeExposure, 0f, 8f);
        }
    }
}
