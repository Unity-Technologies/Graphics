using UnityEngine.Rendering;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class VolumeParameters
    {
        public Bounds  bounds;       // Position and dimensions in meters
        public Vector3 albedo;       // Single scattering albedo [0, 1]
        public Vector3 meanFreePath; // In meters [0.01, inf]
        public float   anisotropy;   // [-1, 1]

        public VolumeParameters()
        {
            bounds       = new Bounds(Vector3.zero, Vector3.positiveInfinity);
            albedo       = new Vector3(0.5f, 0.5f, 0.5f);
            meanFreePath = new Vector3(100.0f, 100.0f, 100.0f);
            anisotropy   = 0.0f;
        }

        public bool IsVolumeUnbounded()
        {
            return bounds.size == Vector3.positiveInfinity;
        }

        public Vector3 AbsorptionCoefficient()
        {
            return Vector3.Max(ExtinctionCoefficient() - ScatteringCoefficient(), Vector3.zero);
        }

        public Vector3 ScatteringCoefficient()
        {
            return new Vector3(albedo.x / meanFreePath.x, albedo.y / meanFreePath.y, albedo.z / meanFreePath.z);
        }

        public Vector3 ExtinctionCoefficient()
        {
            return new Vector3(1.0f / meanFreePath.x, 1.0f / meanFreePath.y, 1.0f / meanFreePath.z);
        }

        public void SetAbsorptionAndScatteringCoefficients(Vector3 absorption, Vector3 scattering)
        {
            Debug.Assert(Mathf.Min(absorption.x, absorption.y, absorption.z) >= 0, "The absorption coefficient must be non-negative.");
            Debug.Assert(Mathf.Min(scattering.x, scattering.y, scattering.z) >= 0, "The scattering coefficient must be non-negative.");

            Vector3 extinction = absorption + scattering;

            meanFreePath = new Vector3(1.0f / extinction.x, 1.0f / extinction.y, 1.0f / extinction.z);
            albedo       = new Vector3(scattering.x * meanFreePath.x, scattering.y * meanFreePath.y, scattering.z * meanFreePath.z);

            ConstrainParameters();
        }

        public void ConstrainParameters()
        {
            bounds.size = Vector3.Max(bounds.size, Vector3.zero);

            albedo.x = Mathf.Clamp01(albedo.x);
            albedo.y = Mathf.Clamp01(albedo.y);
            albedo.z = Mathf.Clamp01(albedo.z);

            meanFreePath.x = Mathf.Max(meanFreePath.x, 0.01f);
            meanFreePath.y = Mathf.Max(meanFreePath.y, 0.01f);
            meanFreePath.z = Mathf.Max(meanFreePath.z, 0.01f);

            anisotropy = Mathf.Clamp(anisotropy, -1.0f, 1.0f);
        }
    }

    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/Homogeneous Fog", -1)]
    public class HomogeneousFog : MonoBehaviour
    {
        public VolumeParameters volumeParameters;

        void Awake()
        {
            if (volumeParameters == null)
            {
                volumeParameters = new VolumeParameters();
            }
        }

        private void OnEnable()
        {
        }

        void OnDisable()
        {
        }

        void Update()
        {
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
