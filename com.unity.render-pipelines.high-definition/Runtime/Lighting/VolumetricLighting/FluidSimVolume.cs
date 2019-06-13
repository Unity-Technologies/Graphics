using System;
using UnityEngine.Rendering;

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

            if (initialStateTexture != null)
            {
                data.volumeRes = new Vector3(
                    initialStateTexture.width,
                    initialStateTexture.height,
                    initialStateTexture.depth);

                data.textureIndex = 0;
            }
            else
            {
                data.volumeRes = new Vector3(1.0f, 1.0f, 1.0f);
                data.textureIndex = -1;
            }

            return data;
        }
    }

    [ExecuteAlways]
    [AddComponentMenu("Rendering/Fluid Simulation Volume", 1200)]
    public class FluidSimVolume : MonoBehaviour
    {
        public FluidSimVolumeArtistParameters parameters = new FluidSimVolumeArtistParameters();

        public RTHandleSystem.RTHandle fSimTexture = null;
        public RTHandleSystem.RTHandle bSimTexture = null;

        public bool needToInitialize { get; private set; }

        private void Start()
        {
            needToInitialize = true;
        }

        public FluidSimVolume()
        {
        }

        public void SwapTexture()
        {
            var temp = fSimTexture;
            fSimTexture = bSimTexture;
            bSimTexture = temp;
        }

        private void OnEnable()
        {
            needToInitialize = true;
            FluidSimVolumeManager.manager.RegisterVolume(this);
        }

        private void OnDisable()
        {
            needToInitialize = false;
            FluidSimVolumeManager.manager.DeRegisterVolume(this);
        }

        private void Update()
        {
            bool recreate =
                parameters.initialStateTexture != null && (
                fSimTexture == null ||
                parameters.initialStateTexture.width  != fSimTexture.rt.width ||
                parameters.initialStateTexture.height != fSimTexture.rt.height ||
                parameters.initialStateTexture.depth  != fSimTexture.rt.volumeDepth);

            if (recreate)
            {
                needToInitialize = true;

                if (fSimTexture != null)
                    RTHandles.Release(fSimTexture);
                if (bSimTexture != null)
                    RTHandles.Release(bSimTexture);

                fSimTexture = RTHandles.Alloc(
                    parameters.initialStateTexture.width,
                    parameters.initialStateTexture.height,
                    parameters.initialStateTexture.depth,
                    colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                    filterMode: FilterMode.Bilinear,
                    dimension: TextureDimension.Tex3D,
                    enableRandomWrite: true,
                    name: "SimTexture0");

                bSimTexture = RTHandles.Alloc(
                    parameters.initialStateTexture.width,
                    parameters.initialStateTexture.height,
                    parameters.initialStateTexture.depth,
                    colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                    filterMode: FilterMode.Bilinear,
                    dimension: TextureDimension.Tex3D,
                    enableRandomWrite: true,
                    name: "SimTexture1");
            }
            else
            {
                needToInitialize = false;
            }
        }
    }
}
