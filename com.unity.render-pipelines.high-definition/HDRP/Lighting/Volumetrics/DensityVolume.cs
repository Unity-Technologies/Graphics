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
        public Vector3 textureScrollingSpeed;
        public Vector3 textureTiling;

        private Vector3 volumeScrollingAmount;

        public DensityVolumeParameters(Color color, float _meanFreePath, float _asymmetry)
        {
            albedo = color;
            meanFreePath = _meanFreePath;
            asymmetry = _asymmetry;

            volumeMask = null;
            textureIndex = -1;
            textureScrollingSpeed = Vector3.zero;
            textureTiling = Vector3.one;
            volumeScrollingAmount = textureScrollingSpeed;
        }

        public void Update(bool animate, float time)
        {
            //Update scrolling based on deltaTime
            if (volumeMask != null)
            {
                float animationTime = animate ? time : 0.0f;
                volumeScrollingAmount = (textureScrollingSpeed * animationTime);
            }
        }

        public void Constrain()
        {
            albedo.r = Mathf.Clamp01(albedo.r);
            albedo.g = Mathf.Clamp01(albedo.g);
            albedo.b = Mathf.Clamp01(albedo.b);
            albedo.a = 1.0f;

            meanFreePath = Mathf.Clamp(meanFreePath, 1.0f, float.MaxValue);

            asymmetry = Mathf.Clamp(asymmetry, -1.0f, 1.0f);

            volumeScrollingAmount = Vector3.zero;
        }

        public DensityVolumeData GetData()
        {
            DensityVolumeData data = new DensityVolumeData();

            data.extinction = VolumeRenderingUtils.ExtinctionFromMeanFreePath(meanFreePath);
            data.scattering = VolumeRenderingUtils.ScatteringFromExtinctionAndAlbedo(data.extinction, (Vector3)(Vector4)albedo);

            data.textureIndex = textureIndex;
            data.textureScroll = volumeScrollingAmount;
            data.textureTiling = textureTiling;

            return data;
        }
    } // class DensityVolumeParameters

    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/Density Volume", 1100)]
    public class DensityVolume : MonoBehaviour
    {
        public DensityVolumeParameters parameters = new DensityVolumeParameters(Color.white, 10.0f, 0.0f);

        private Texture3D previousVolumeMask = null;

        public Action OnTextureUpdated;

        //Gather and Update any parameters that may have changed
        public void PrepareParameters(bool animate, float time)
        {
            //Texture has been updated notify the manager
            if (previousVolumeMask != parameters.volumeMask)
            {
                NotifyUpdatedTexure();
                previousVolumeMask = parameters.volumeMask;
            }

            parameters.Update(animate, time);
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
