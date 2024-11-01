using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class WaterSurface
    {
        #region Water Deformation
        /// <summary>
        /// Specifies if the water surfaces supports local deformations.
        /// </summary>
        public bool deformation = false;

        /// <summary>
        /// Specifies the resolution of the deformation texture used to represent the deformation area.
        /// </summary>
        [Tooltip("Specifies the resolution of the deformation texture used to represent the deformation area.")]
        public WaterDecalRegionResolution deformationRes = WaterDecalRegionResolution.Resolution512;
        #endregion

        /// <summary>
        /// Function that returns the deformation buffer for the water surface. If the feature is disabled or the resource is not available the function returns null.
        /// </summary>
        /// <seealso cref="WaterSurface.GetDecalRegion"/>
        /// <returns>A single channgel texture that holds the surface deformation of the water surface.</returns>
        public Texture GetDeformationBuffer()
        {
            return deformation ? deformationBuffer : null;
        }

        // GPU deformation data
        internal RTHandle deformationBuffer = null;
        internal RTHandle deformationSGBuffer = null;

        // CPU Deformation data
        internal AsyncTextureSynchronizer<half> deformationBufferSychro = new AsyncTextureSynchronizer<half>(GraphicsFormat.R16_SFloat);

        void FillWaterDeformationData(ref WaterSimSearchData wsd)
        {
            var system = HDRenderPipeline.currentPipeline.waterSystem;

            // Water Mask
            if (system.m_ActiveDeformation && deformation && deformationBufferSychro.TryGetBuffer(out var deformationBuffer) && deformationBuffer.Length > 0)
            {
                wsd.activeDeformation = true;
                wsd.deformationBuffer = deformationBuffer;
                wsd.deformationResolution = deformationBufferSychro.CurrentResolution();
            }
            else
            {
                wsd.activeDeformation = false;
                wsd.deformationBuffer = system.m_DefaultDeformationBuffer;
            }

            Matrix4x4 worldToWater = simulation.rendering.worldToWaterMatrix;
            wsd.waterForwardXZ = float2(worldToWater.m00, worldToWater.m02);
        }

        internal void CheckDeformationResources(bool horizontalDeformation = false)
        {
            if (deformation)
            {
                int resolution = (int)deformationRes;
                if (deformationBuffer != null && deformationBuffer.rt.width != resolution)
                    ReleaseDeformationResources();

                if (deformationBuffer == null)
                {
                    // If we support horizontal deformation we need more channels.
                    var formatDeformationBuffer = horizontalDeformation ? GraphicsFormat.R16G16B16A16_SFloat : GraphicsFormat.R16_SFloat;
                    var formatDeformationSGBuffer = horizontalDeformation ? GraphicsFormat.R16G16B16A16_SFloat : GraphicsFormat.R16G16_SFloat;

                    deformationBuffer = RTHandles.Alloc(resolution, resolution, 1, dimension: TextureDimension.Tex2D, colorFormat: formatDeformationBuffer, enableRandomWrite: true, wrapMode: TextureWrapMode.Clamp, name: "Water Deformation");
                    deformationSGBuffer = RTHandles.Alloc(resolution, resolution, 1, dimension: TextureDimension.Tex2D, colorFormat: formatDeformationSGBuffer, enableRandomWrite: true, wrapMode: TextureWrapMode.Clamp, name: "Water Deformation SG");
                }
            }
            else if (deformationBuffer != null)
                ReleaseDeformationResources();
        }

        internal void ReleaseDeformationResources()
        {
            RTHandles.Release(deformationBuffer);
            RTHandles.Release(deformationSGBuffer);
            deformationBuffer = null;
            deformationSGBuffer = null;

            // CPU Simulation data
            deformationBufferSychro.ReleaseATSResources();
        }

        internal Texture GetDeformationBuffer(WaterSystem system, bool frameSetting, Texture defaultValue = null)
        {
            return frameSetting && system.m_ActiveDeformation && deformation ? deformationBuffer : defaultValue;
        }

        internal Texture GetDeformationNormalBuffer(WaterSystem system, bool frameSetting, Texture defaultValue = null)
        {
            return frameSetting && system.m_ActiveDeformation && deformation ? deformationSGBuffer : defaultValue;
        }
    }
}
