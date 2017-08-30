using UnityEngine.Rendering;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public struct VolumeProperties
    {
        public Vector3 scattering; // [0, 1], prefer sRGB
        public float   extinction; // [0, 1], prefer sRGB
        public float   asymmetry;  // Global (scene) property
        public float   align16_0;
        public float   align16_1;
        public float   align16_2;

        public static VolumeProperties GetNeutralVolumeProperties()
        {
            VolumeProperties properties = new VolumeProperties();

            properties.scattering = Vector3.zero;
            properties.extinction = 0;
            properties.asymmetry  = 0;

            return properties;
        }
    }

    [Serializable]
    public class VolumeParameters
    {
        public Bounds bounds;       // Position and dimensions in meters
        public Color  albedo;       // Single scattering albedo [0, 1]
        public float  meanFreePath; // In meters [1, inf]. Should be chromatic - this is an optimization!
        public float  anisotropy;   // [-1, 1]; 0 = isotropic

        public VolumeParameters()
        {
            bounds       = new Bounds(Vector3.zero, Vector3.positiveInfinity);
            albedo       = new Color(0.5f, 0.5f, 0.5f);
            meanFreePath = 10.0f;
            anisotropy   = 0.0f;
        }

        public bool IsVolumeUnbounded()
        {
            return bounds.size.x == float.PositiveInfinity &&
                   bounds.size.y == float.PositiveInfinity &&
                   bounds.size.z == float.PositiveInfinity;
        }

        public Vector3 GetAbsorptionCoefficient()
        {
            float   extinction = GetExtinctionCoefficient();
            Vector3 scattering = GetScatteringCoefficient();

            return Vector3.Max(new Vector3(extinction, extinction, extinction) - scattering, Vector3.zero);
        }

        public Vector3 GetScatteringCoefficient()
        {
            float extinction = GetExtinctionCoefficient();

            return new Vector3(albedo.r * extinction, albedo.g * extinction, albedo.b * extinction);
        }

        public float GetExtinctionCoefficient()
        {
            return 1.0f / meanFreePath;
        }

        public void Constrain()
        {
            bounds.size = Vector3.Max(bounds.size, Vector3.zero);

            albedo.r = Mathf.Clamp01(albedo.r);
            albedo.g = Mathf.Clamp01(albedo.g);
            albedo.b = Mathf.Clamp01(albedo.b);

            meanFreePath = Mathf.Max(meanFreePath, 1.0f);

            anisotropy = Mathf.Clamp(anisotropy, -1.0f, 1.0f);
        }

        public VolumeProperties GetProperties()
        {
            VolumeProperties properties = new VolumeProperties();

            properties.scattering = GetScatteringCoefficient();
            properties.extinction = GetExtinctionCoefficient();
            properties.asymmetry  = anisotropy;

            return properties;
        }
    }

    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/Homogeneous Fog", -1)]
    public class HomogeneousFog : MonoBehaviour
    {
        public VolumeParameters volumeParameters;

        private void Awake()
        {
            if (volumeParameters == null)
            {
                volumeParameters = new VolumeParameters();
            }
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        private void Update()
        {
        }

        private void OnValidate()
        {
            volumeParameters.Constrain();
        }

        void OnDrawGizmos()
        {
            if (volumeParameters != null && !volumeParameters.IsVolumeUnbounded())
            {
                Gizmos.DrawWireCube(volumeParameters.bounds.center, volumeParameters.bounds.size);
            }
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
