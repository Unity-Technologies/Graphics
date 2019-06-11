using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class FluidSimVolumeManager
    {
        static private FluidSimVolumeManager _instance = null;

        public static FluidSimVolumeManager manager
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FluidSimVolumeManager();
                }
                return _instance;
            }
        }

        private ComputeShader _fluidSimVolumeCS = null;
        private ComputeShader _texture3DAtlasCS = null;

        private List<FluidSimVolume> _volumes = null;

        private RTHandleSystem.RTHandle _volumeAtlas = null;
        private bool atlasNeedsRefresh = false;

        //TODO: hardcoded size....:-(
        public static int fluidSimVolumeTextureSize = 256;

        private FluidSimVolumeManager()
        {
            int res = 512;

            _volumes = new List<FluidSimVolume>();
            _volumeAtlas = RTHandles.Alloc(
                res,
                res,
                res,
                colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                filterMode: FilterMode.Bilinear,
                dimension: TextureDimension.Tex3D,
                enableRandomWrite: true,
                name: "VolumeAtlas");
        }

        public void Build(HDRenderPipelineAsset asset)
        {
            _fluidSimVolumeCS = asset.renderPipelineResources.shaders.fluidSimVolumeCS;
            _texture3DAtlasCS = asset.renderPipelineResources.shaders.texture3DAtlasCS;
        }

        public void RegisterVolume(FluidSimVolume volume)
        {
            _volumes.Add(volume);
        }

        public void DeRegisterVolume(FluidSimVolume volume)
        {
            if (_volumes.Contains(volume))
            {
                _volumes.Remove(volume);
            }
        }

        public FluidSimVolume[] PrepareFluidSimVolumeData(CommandBuffer cmd, Camera currentCam, float time)
        {
            int kernel = _texture3DAtlasCS.FindKernel("");

            foreach (var volume in _volumes)
            {
                cmd.SetComputeTextureParam(_texture3DAtlasCS, kernel, "", _volumeAtlas);
            }
            cmd.SetComputeTextureParam(_texture3DAtlasCS, kernel, "", _volumeAtlas);

            return _volumes.ToArray();
        }

        public void TriggerVolumeAtlasRefresh()
        {
            atlasNeedsRefresh = true;
        }
    }
}
