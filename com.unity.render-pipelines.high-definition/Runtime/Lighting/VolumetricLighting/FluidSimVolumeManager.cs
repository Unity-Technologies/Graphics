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

        private const int threadTile = 4;
        private const int lessTile = threadTile - 1;

        private ComputeShader _fluidSimVolumeCS = null;
        private ComputeShader _texture3DAtlasCS = null;

        private List<FluidSimVolume> _volumes = null;

        public RTHandleSystem.RTHandle volumeAtlas = null;

        //TODO: hardcoded size....:-(
        // need to sync this with in VolumeVoxelization.compute and VolumetricShadowMap.compute
        public static int fluidSimVolumeTextureSize = 512;

        private FluidSimVolumeManager()
        {
            _volumes = new List<FluidSimVolume>();
            volumeAtlas = RTHandles.Alloc(
                fluidSimVolumeTextureSize,
                fluidSimVolumeTextureSize,
                fluidSimVolumeTextureSize,
                colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                filterMode: FilterMode.Bilinear,
                dimension: TextureDimension.Tex3D,
                enableRandomWrite: true,
                name: "FluidSimVolumeAtlas");
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

        public void SimulateVolume(CommandBuffer cmd)
        {
            foreach (var volume in _volumes)
            {
                if (volume.parameters.workflow != (int)FluidSimVolume.Workflow.VectorField)
                    continue;

                var initialSimTexture = volume.parameters.initialStateTexture;
                if (initialSimTexture == null)
                    continue;

                int fluidSimVolumeResX = initialSimTexture.width;
                int fluidSimVolumeResY = initialSimTexture.height;
                int fluidSimVolumeResZ = initialSimTexture.depth;

                int dispatchX = (fluidSimVolumeResX + lessTile) / threadTile;
                int dispatchY = (fluidSimVolumeResY + lessTile) / threadTile;
                int dispatchZ = (fluidSimVolumeResZ + lessTile) / threadTile;

                if (volume.needToInitialize)
                {
                    var simulationBuffer0 = volume.simulationBuffer0;
                    if (simulationBuffer0 == null)
                        continue;

                    var simulationBuffer1 = volume.simulationBuffer1;
                    if (simulationBuffer1 == null)
                        continue;

                    var kernel = _fluidSimVolumeCS.FindKernel("InitialState");
                    if (kernel == -1)
                        continue;

                    cmd.SetComputeTextureParam(_fluidSimVolumeCS, kernel, HDShaderIDs._InitialStateVolumeTexture, initialSimTexture);
                    cmd.SetComputeTextureParam(_fluidSimVolumeCS, kernel, HDShaderIDs._SimulationBuffer0, simulationBuffer0);
                    cmd.SetComputeTextureParam(_fluidSimVolumeCS, kernel, HDShaderIDs._SimulationBuffer1, simulationBuffer1);

                    cmd.DispatchCompute(_fluidSimVolumeCS, kernel, dispatchX, dispatchY, dispatchZ);
                }
                else
                {
                    var simulationBuffer0 = volume.simulationBuffer0;
                    if (simulationBuffer0 == null)
                        continue;

                    var simulationBuffer1 = volume.simulationBuffer1;
                    if (simulationBuffer1 == null)
                        continue;

                    var vectorField = volume.parameters.vectorField;
                    if (vectorField == null)
                        continue;

                    var vectorFieldNext = volume.parameters.vectorFieldNext;
                    if (vectorFieldNext == null)
                        continue;

                    var kernel = _fluidSimVolumeCS.FindKernel("Simulate");
                    if (kernel == -1)
                        continue;

                    //Debug.Log("FBlend: " + volume.frameBlend + "][ ComputeShader: " + vectorField.name + ",  " + vectorFieldLast.name);
                    //Debug.Log("ComputeShader: " + vectorField.name + ",  " + vectorFieldNext.name);

                    var fluidSimVolumeRes = new Vector3(fluidSimVolumeResX, fluidSimVolumeResY, fluidSimVolumeResZ);
                    var vectorFieldSpeed = volume.parameters.vectorFieldSpeed;
                
                    cmd.SetComputeVectorParam(_fluidSimVolumeCS, HDShaderIDs._FluidSimVolumeRes, fluidSimVolumeRes);
                    cmd.SetComputeTextureParam(_fluidSimVolumeCS, kernel, HDShaderIDs._VectorField0, vectorField);
                    cmd.SetComputeTextureParam(_fluidSimVolumeCS, kernel, HDShaderIDs._VectorField1, vectorFieldNext);
                    cmd.SetComputeFloatParam(_fluidSimVolumeCS, HDShaderIDs._VectorFieldSpeed, vectorFieldSpeed);
                    cmd.SetComputeFloatParam(_fluidSimVolumeCS, HDShaderIDs._FrameBlend, volume.frameBlend);

                    cmd.SetComputeTextureParam(_fluidSimVolumeCS, kernel, HDShaderIDs._SimulationBuffer0, simulationBuffer0);
                    cmd.SetComputeTextureParam(_fluidSimVolumeCS, kernel, HDShaderIDs._SimulationBuffer1, simulationBuffer1);

                    cmd.DispatchCompute(_fluidSimVolumeCS, kernel, dispatchX, dispatchY, dispatchZ);
                }
            }
        }
        public void ClearTexture3DAtlas(CommandBuffer cmd)
        {
            int kernel = _texture3DAtlasCS.FindKernel("ClearTexture");

            int dispatchX = (512 + lessTile) / threadTile;
            int dispatchY = (512 + lessTile) / threadTile;
            int dispatchZ = (512 + lessTile) / threadTile;

            cmd.SetComputeTextureParam(_texture3DAtlasCS, kernel, HDShaderIDs._OutputVolumeAtlas, volumeAtlas);

            cmd.DispatchCompute(_texture3DAtlasCS, kernel, dispatchX, dispatchY, dispatchZ);
        }
        public void CopyTextureToAtlas(CommandBuffer cmd)
        {
            int kernel0 = _texture3DAtlasCS.FindKernel("CopySimulation");
            int kernel1 = _texture3DAtlasCS.FindKernel("CopyAnimDensity");

            cmd.SetComputeTextureParam(_texture3DAtlasCS, kernel0, HDShaderIDs._OutputVolumeAtlas, volumeAtlas);
            cmd.SetComputeTextureParam(_texture3DAtlasCS, kernel1, HDShaderIDs._OutputVolumeAtlas, volumeAtlas);

            foreach (var volume in _volumes)
            {
                if (volume.parameters.workflow == (int)FluidSimVolume.Workflow.VectorField)
                {
                    //Debug.Log("On VectorField");
                    var initialSimTexture = volume.parameters.initialStateTexture;
                    if (initialSimTexture == null)
                        continue;

                    var simulationBuffer0 = volume.simulationBuffer0;
                    if (simulationBuffer0 == null)
                        continue;

                    var simulationBuffer1 = volume.simulationBuffer1;
                    if (simulationBuffer1 == null)
                        continue;

                    int fluidSimVolumeResX = initialSimTexture.width;
                    int fluidSimVolumeResY = initialSimTexture.height;
                    int fluidSimVolumeResZ = initialSimTexture.depth;

                    int dispatchX = (fluidSimVolumeResX + lessTile) / threadTile;
                    int dispatchY = (fluidSimVolumeResY + lessTile) / threadTile;
                    int dispatchZ = (fluidSimVolumeResZ + lessTile) / threadTile;

                    cmd.SetComputeTextureParam(_texture3DAtlasCS, kernel0, HDShaderIDs._SimulationBuffer0, simulationBuffer0);
                    cmd.SetComputeTextureParam(_texture3DAtlasCS, kernel0, HDShaderIDs._SimulationBuffer1, simulationBuffer1);

                    cmd.DispatchCompute(_texture3DAtlasCS, kernel0, dispatchX, dispatchY, dispatchZ);
                }
                else
                {
                    //Debug.Log("On AnimDensity");
                    var initialDensityTexture = volume.parameters.initialDensityTexture;
                    if (initialDensityTexture == null)
                        continue;

                    var densityTexture = volume.parameters.DensityTexture;
                    if (densityTexture == null)
                        continue;

                    var densityTextureNext = volume.parameters.DensityTextureNext;
                    if (densityTextureNext == null)
                        continue;

                    int fluidSimVolumeResX = initialDensityTexture.width;
                    int fluidSimVolumeResY = initialDensityTexture.height;
                    int fluidSimVolumeResZ = initialDensityTexture.depth;

                    int dispatchX = (fluidSimVolumeResX + lessTile) / threadTile;
                    int dispatchY = (fluidSimVolumeResY + lessTile) / threadTile;
                    int dispatchZ = (fluidSimVolumeResZ + lessTile) / threadTile;

                    cmd.SetComputeFloatParam(_texture3DAtlasCS, HDShaderIDs._FrameBlend, volume.frameBlend);
                    cmd.SetComputeTextureParam(_texture3DAtlasCS, kernel1, HDShaderIDs._AnimDensityTexture0, densityTexture);
                    cmd.SetComputeTextureParam(_texture3DAtlasCS, kernel1, HDShaderIDs._AnimDensityTexture1, densityTextureNext);

                    cmd.DispatchCompute(_texture3DAtlasCS, kernel1, dispatchX, dispatchY, dispatchZ);
                }
            }
        }

        public FluidSimVolume[] PrepareFluidSimVolumeData(CommandBuffer cmd, Camera currentCam, float time)
        {
            return _volumes.ToArray();
        }
    }
}
