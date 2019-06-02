using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public struct FluidSimVolumeArtistParameters
    {
        public Texture3D initialState;

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

        private Texture3D primary = null;
        private Texture3D secondary = null;

        private Texture3D inputPM;
        private Texture3D outputPM;

        private void Start()
        {
            primary   = new Texture3D(128, 128, 128, DefaultFormat.HDR, TextureCreationFlags.None);
            secondary = new Texture3D(128, 128, 128, DefaultFormat.HDR, TextureCreationFlags.None);

            inputPM  = primary;
            outputPM = secondary;
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
        }
    }
}
