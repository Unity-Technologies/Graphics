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
        /// Defines the resolution of the internal deformation texture.
        /// </summary>
        public enum WaterDeformationResolution
        {
            /// <summary>
            /// The water deformation are rendered in a 256x256 texture.
            /// </summary>
            [InspectorName("Low 256")]
            Resolution256 = 256,
            /// <summary>
            /// The water deformation are rendered in a 512x512 texture.
            /// </summary>
            [InspectorName("Medium 512")]
            Resolution512 = 512,
            /// <summary>
            /// The water deformation are rendered in a 1024x1024 texture.
            /// </summary>
            [InspectorName("High 1024")]
            Resolution1024 = 1024,
            /// <summary>
            /// The water deformation are rendered in a 2048x2048 texture.
            /// </summary>
            [InspectorName("Very High 2048")]
            Resolution2048 = 2048,
        }

        /// <summary>
        /// Specifies the resolution of the deformation texture used to represent the deformation area.
        /// </summary>
        [Tooltip("Specifies the resolution of the deformation texture used to represent the deformation area.")]
        public WaterDeformationResolution deformationRes = WaterDeformationResolution.Resolution512;

        /// <summary>
        /// Specified the size of the deformation area in meters.
        /// </summary>
        [Tooltip("Specified the size of the deformation area in meters.")]
        public Vector2 deformationAreaSize = new Vector2(200.0f, 200.0f);

        /// <summary>
        /// Specified the offset of the deformation area in meters.
        /// </summary>
        [Tooltip("Specified the offset of the deformation area in meters.")]
        public Vector2 deformationAreaOffset = new Vector2(0.0f, 0.0f);
        #endregion

        // GPU deformation data
        internal RTHandle deformationBuffer = null;
        internal RTHandle deformationSGBuffer = null;

        // CPU Deformation data
        internal AsyncTextureSynchronizer<half> deformationBufferSychro = new AsyncTextureSynchronizer<half>(GraphicsFormat.R16_SFloat);

        void FillWaterDeformationData(ref WaterSimSearchData wsd)
        {
            // Water Mask
            if (deformation && deformationBufferSychro.TryGetBuffer(out var deformationBuffer) && deformationBuffer.Length > 0 && HDRenderPipeline.currentPipeline.NumActiveWaterDeformers() > 0)
            {
                wsd.activeDeformation = true;
                wsd.deformationBuffer = deformationBuffer;
                wsd.deformationResolution = deformationBufferSychro.CurrentResolution();
            }
            else
            {
                wsd.activeDeformation = false;
                wsd.deformationBuffer = HDRenderPipeline.currentPipeline.m_DefaultDeformationBuffer;
            }

            wsd.deformationRegionScale = float2(1.0f / deformationAreaSize.x, 1.0f / deformationAreaSize.y);
            wsd.deformationRegionOffset = float2(deformationAreaOffset.x, deformationAreaOffset.y);
        }

        internal void CheckDeformationResources()
        {
            if (deformation)
            {
                int resolution = (int)deformationRes;

                // If the deformation area is at the wrong resolution, release it and reallocate it.
                if (deformationBuffer != null && deformationBuffer.rt.width != resolution)
                    ReleaseDeformationResources();

                if (deformationBuffer == null)
                {
                    deformationBuffer = RTHandles.Alloc(resolution, resolution, 1, dimension: TextureDimension.Tex2D, colorFormat: GraphicsFormat.R16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Clamp);
                    deformationSGBuffer = RTHandles.Alloc(resolution, resolution, 1, dimension: TextureDimension.Tex2D, colorFormat: GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Clamp);
                }
            }
            else
            {
                if (deformationBuffer != null)
                    ReleaseDeformationResources();
            }
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
    }
}
