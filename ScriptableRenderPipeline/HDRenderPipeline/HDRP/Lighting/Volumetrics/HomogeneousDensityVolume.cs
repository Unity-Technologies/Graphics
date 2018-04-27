using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public struct DensityVolumeParameters
    {
        public Color albedo;       // Single scattering albedo [0, 1]. Alpha is ignored
        public float meanFreePath; // In meters [1, inf]. Should be chromatic - this is an optimization!
        public float asymmetry;    // Only used if (isLocal == false)

        public Texture3D volumeMask;
        public int textureIndex;

        public void Constrain()
        {
            albedo.r = Mathf.Clamp01(albedo.r);
            albedo.g = Mathf.Clamp01(albedo.g);
            albedo.b = Mathf.Clamp01(albedo.b);
            albedo.a = 1.0f;

            meanFreePath = Mathf.Clamp(meanFreePath, 1.0f, float.MaxValue);

            asymmetry = Mathf.Clamp(asymmetry, -1.0f, 1.0f);
        }

        public DensityVolumeData GetData()
        {
            DensityVolumeData data = new DensityVolumeData();

            data.extinction = VolumeRenderingUtils.ExtinctionFromMeanFreePath(meanFreePath);
            data.scattering = VolumeRenderingUtils.ScatteringFromExtinctionAndAlbedo(data.extinction, (Vector3)(Vector4)albedo);
            data.textureIndex = textureIndex;

            return data;
        }
    } // class DensityVolumeParameters

    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/Homogeneous Density Volume", 1100)]
    public class HomogeneousDensityVolume : MonoBehaviour
    {
        public DensityVolumeParameters parameters;

        private Texture3D previousVolumeMask = null;

        public Action OnTextureUpdated;

        public HomogeneousDensityVolume()
        {
            parameters.albedo       = new Color(0.5f, 0.5f, 0.5f);
            parameters.meanFreePath = 10.0f;
            parameters.asymmetry    = 0.0f;

            parameters.volumeMask = null;
            parameters.textureIndex = -1;
        }

        //Gather and Update any parameters that may have changed
        public void PrepareParameters()
        {
            //Texture has been updated notify the manager
            if (previousVolumeMask != parameters.volumeMask) 
            {
                NotifyUpdatedTexure();
                previousVolumeMask = parameters.volumeMask;
            }
        }

        private void NotifyUpdatedTexure()
        {
            if (OnTextureUpdated != null)
            {
                OnTextureUpdated();
            }
        }

        private void Awake()
        {
        }

        private void OnEnable()
        {
            DensityVolumeManager.manager.RegisterVolume(this);
        }

        private void OnDisable()
        {
            DensityVolumeManager.manager.DeRegisterVolume(this);
        }

        private void Update()
        {
        }

        private void OnValidate()
        {
            parameters.Constrain();
        }

        void OnDrawGizmos()
        {
            Gizmos.color  = parameters.albedo;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
