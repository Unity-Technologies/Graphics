using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public struct FluidSimVolumeArtistParameters
    {
        public Texture3D initialStateTexture;

        [SerializeField]
        public Vector3 positiveFade;
        [SerializeField]
        public Vector3 negativeFade;

        [SerializeField]
        internal float m_EditorUniformFade;
        [SerializeField]
        internal Vector3 m_EditorPositiveFade;
        [SerializeField]
        internal Vector3 m_EditorNegativeFade;
        [SerializeField]
        internal bool m_EditorAdvancedFade;

        public Vector3 size;

        public float distanceFadeStart;
        public float distanceFadeEnd;

        public int textureIndex;

        public FluidSimVolumeEngineData ConvertToEngineData()
        {
            FluidSimVolumeEngineData data = new FluidSimVolumeEngineData();

            // todo : implement it!

            return data;
        }
    }

    [ExecuteAlways]
    [AddComponentMenu("Rendering/Fluid Simulation Volume", 1200)]
    public class FluidSimVolume : MonoBehaviour
    {
        public FluidSimVolumeArtistParameters parameters = new FluidSimVolumeArtistParameters();

        public Texture3D fluidSimTexture = null;

        private void Start()
        {
        }

        public FluidSimVolume()
        {
        }

        private void OnEnable()
        {
            FluidSimVolumeManager.manager.RegisterVolume(this);
        }

        private void OnDisable()
        {
            FluidSimVolumeManager.manager.DeRegisterVolume(this);
        }

        private void Update()
        {
            bool recreate =
                parameters.initialStateTexture != null && (
                fluidSimTexture == null ||
                parameters.initialStateTexture.width  != fluidSimTexture.width ||
                parameters.initialStateTexture.height != fluidSimTexture.height ||
                parameters.initialStateTexture.depth  != fluidSimTexture.depth);

            if (recreate)
            {
                if (fluidSimTexture != null)
                    DestroyImmediate(fluidSimTexture);

                fluidSimTexture = new Texture3D(
                    parameters.initialStateTexture.width,
                    parameters.initialStateTexture.height,
                    parameters.initialStateTexture.depth,
                    TextureFormat.RGBA32,
                    false);
            }
        }
    }
}
