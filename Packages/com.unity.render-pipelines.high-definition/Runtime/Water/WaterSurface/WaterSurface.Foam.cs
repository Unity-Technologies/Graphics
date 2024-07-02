using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class WaterSurface : MonoBehaviour
    {
        #region Water Foam
        /// <summary>
        /// Specifies if the water surfaces support foam rendering.
        /// </summary>
        public bool foam = false;

        /// <summary>
        /// Specifies the resolution of the foam texture.
        /// </summary>
        [Tooltip("Specifies the resolution of the foam texture.")]
        public WaterDecalRegionResolution foamResolution = WaterDecalRegionResolution.Resolution512;

        /// <summary>
        /// Specifies the foam persistence multiplier. A higher value will lead to the foam remaining visible longer.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float foamPersistenceMultiplier = 0.5f;

        /// <summary>
        /// Specifies the influence of current on foam.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float foamCurrentInfluence = 0.6f;

        /// <summary>
        /// Set the foam color.
        /// </summary>
        public Color foamColor = Color.white;

        /// <summary>
        /// Set the per meter tiling for the foam texture.
        /// </summary>
        [Min(0.0f)]
        public float foamTextureTiling = 0.2f;

        /// <summary>
        /// Controls the surface foam smoothness.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float foamSmoothness = 0.3f;

        /// <summary>
        /// Controls the simulation foam amount. Higher values generate larger foam patches. Foam presence is highly dependent on the wind speed and choppiness values.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float simulationFoamAmount = 0.3f;

        /// <summary>
        /// Set the texture used to attenuate or suppress the simulation foam.
        /// </summary>
        public bool supportSimulationFoamMask = false;

        /// <summary>
        /// Set the texture used to attenuate or suppress the simulation foam.
        /// </summary>
        public Texture2D simulationFoamMask = null;

        /// <summary>
        /// Sets the extent of the foam mask in meters.
        /// </summary>
        public Vector2 simulationFoamMaskExtent = new Vector2(100.0f, 100.0f);

        /// <summary>
        /// Sets the offset of the foam mask in meters.
        /// </summary>
        public Vector2 simulationFoamMaskOffset = new Vector2(0.0f, 0.0f);

        /// <summary>
        /// Controls the simulation foam amount depending on the wind speed.
        /// </summary>
        public AnimationCurve simulationFoamWindCurve = new AnimationCurve(new Keyframe(0f, 0.0f), new Keyframe(0.2f, 0.0f), new Keyframe(0.3f, 1.0f), new Keyframe(1.0f, 1.0f));
        #endregion

        /// <summary>
        /// Function that returns the foam buffer for the water surface. If the feature is disabled or the resource is not available the function returns null.
        /// </summary>
        /// <param name="foamArea">Output parameter that returns the size of the foam region.</param>
        /// <seealso cref="WaterSurface.GetDecalRegion"/>
        /// <returns>An RG texture that holds the surface foam (red channel) and deep foam (green channel) of the water surface.</returns>
        public Texture GetFoamBuffer(out Vector2 foamArea)
        {
            foamArea = decalRegionSize;
            return GetFoamBuffer(HDRenderPipeline.currentPipeline?.waterSystem, true);
        }

        // GPU foam data
        internal RTHandle[] foamBuffers = new RTHandle[2];
        internal float4 previousFoamRegionScaleOffset;

        internal void CheckFoamResources(CommandBuffer cmd)
        {
            if (foam)
            {
                int resolution = (int)foamResolution;

                // If the foam area is at the wrong resolution, release it and reallocate it.
                if (foamBuffers[0] != null && foamBuffers[0].rt.width != resolution)
                    ReleaseFoamResources();

                if (foamBuffers[0] == null)
                {
                    foamBuffers[0] = RTHandles.Alloc(resolution, resolution, 1, dimension: TextureDimension.Tex2D, colorFormat: GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Clamp, name: "Water Foam");
                    foamBuffers[1] = RTHandles.Alloc(resolution, resolution, 1, dimension: TextureDimension.Tex2D, colorFormat: GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Clamp, name: "Water Foam Tmp");

                    // Clear buffer 0 only
                    CoreUtils.SetRenderTarget(cmd, foamBuffers[0], ClearFlag.Color, Color.black);
                }
            }
            else
            {
                if (foamBuffers[0] != null)
                {
                    ReleaseFoamResources();
                }
            }
        }

        internal void ReleaseFoamResources()
        {
            RTHandles.Release(foamBuffers[0]);
            foamBuffers[0] = null;
            RTHandles.Release(foamBuffers[1]);
            foamBuffers[1] = null;
        }

        internal bool HasSimulationFoam()
        {
            return foam && simulationFoamAmount > 0.0f && surfaceType != WaterSurfaceType.Pool;
        }

        internal RTHandle FoamBuffer()
        {
            return foamBuffers[0];
        }

        internal RTHandle TmpFoamBuffer()
        {
            return foamBuffers[1];
        }

        internal Texture GetFoamBuffer(WaterSystem system, bool frameSetting, Texture defaultValue = null)
        {
            return frameSetting && system != null && system.HasActiveFoam() && foam ? FoamBuffer() : defaultValue;
        }
    }
}
