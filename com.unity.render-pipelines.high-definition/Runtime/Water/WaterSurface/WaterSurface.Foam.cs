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
        /// Defines the resolution of the internal foam texture.
        /// </summary>
        public enum WaterFoamResolution
        {
            /// <summary>
            /// The water foam is rendered in a 256x256 texture.
            /// </summary>
            Resolution256 = 256,
            /// <summary>
            /// The water foam is rendered in a 512x512 texture.
            /// </summary>
            Resolution512 = 512,
            /// <summary>
            /// The water foam is rendered in a 1024x1024 texture.
            /// </summary>
            Resolution1024 = 1024,
            /// <summary>
            /// The water foam is rendered in a 2048x2048 texture.
            /// </summary>
            Resolution2048 = 2048,
        }

        /// <summary>
        /// Specifies the resolution of the foam texture.
        /// </summary>
        [Tooltip("Specifies the resolution of the foam texture.")]
        public WaterFoamResolution foamResolution = WaterFoamResolution.Resolution512;

        /// <summary>
        /// Specifies the size of the foam area in meters.
        /// </summary>
        [Tooltip("Specifies the size of the foam area in meters.")]
        public Vector2 foamAreaSize = new Vector2(200.0f, 200.0f);

        /// <summary>
        /// Specifies the offset of the foam area in meters.
        /// </summary>
        [Tooltip("Specifies the offset of the foam area in meters.")]
        public Vector2 foamAreaOffset = new Vector2(0.0f, 0.0f);

        /// <summary>
        /// Specifies the foam persistence multiplier. A higher value will lead to the foam remaining visible longer.
        /// </summary>
        [Tooltip("Specifies the foam persistence multiplier. A higher value will lead to the foam remaining visible longer.")]
        public float foamPersistenceMultiplier = 0.5f;

        /// <summary>
        /// Set the per meter tiling for the foam texture.
        /// </summary>
        [Tooltip("Set the per meter tiling for the foam texture.")]
        public float foamTextureTiling = 0.2f;

        /// <summary>
        /// Controls the surface foam smoothness.
        /// </summary>
        [Tooltip("Controls the surface foam smoothness.")]
        public float foamSmoothness = 0.3f;

        /// <summary>
        /// When enabled, the water surface will receive foam from the simulation on the crest of the waves.
        /// </summary>
        public bool simulationFoam = true;

        /// <summary>
        /// Controls the simulation foam amount. Higher values generate larger foam patches. Foam presence is highly dependent on the wind speed and choppiness values.
        /// </summary>
        [Tooltip("Controls the simulation foam amount. Higher values generate larger foam patches. Foam presence is highly dependent on the wind speed and choppiness values.")]
        public float simulationFoamAmount = 0.3f;

        /// <summary>
        /// Set the texture used to attenuate or suppress the simulation foam.
        /// </summary>
        public Texture2D simulationFoamMask = null;

        /// <summary>
        /// Sets the extent of the foam mask in meters.
        /// </summary>
        [Tooltip("Sets the extent of the foam mask in meters.")]
        public Vector2 simulationFoamMaskExtent = new Vector2(100.0f, 100.0f);

        /// <summary>
        /// Sets the offset of the foam mask in meters.
        /// </summary>
        [Tooltip("Sets the offset of the foam mask in meters.")]
        public Vector2 simulationFoamMaskOffset = new Vector2(0.0f, 0.0f);

        /// <summary>
        /// Controls the simulation foam amount depending on the wind speed.
        /// </summary>
        public AnimationCurve simulationFoamWindCurve = new AnimationCurve(new Keyframe(0f, 0.0f), new Keyframe(0.2f, 0.0f), new Keyframe(0.3f, 1.0f), new Keyframe(1.0f, 1.0f));
        #endregion

        // GPU foam data
        internal RTHandle[] foamBuffers = new RTHandle[2];
        internal float4 previousFoamData;

        internal void CheckFoamResources()
        {
            if (foam)
            {
                int resolution = (int)foamResolution;

                // If the foam area is at the wrong resolution, release it and reallocate it.
                if (foamBuffers[0] != null && foamBuffers[0].rt.width != resolution)
                    ReleaseFoamResources();

                if (foamBuffers[0] == null)
                {
                    foamBuffers[0] = RTHandles.Alloc(resolution, resolution, 1, dimension: TextureDimension.Tex2D, colorFormat: GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Clamp);
                    foamBuffers[1] = RTHandles.Alloc(resolution, resolution, 1, dimension: TextureDimension.Tex2D, colorFormat: GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Clamp);
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
            return foam && simulationFoam && surfaceType != WaterSurfaceType.Pool;
        }

        internal RTHandle FoamBuffer()
        {
            return foamBuffers[0];
        }

        internal RTHandle TmpFoamBuffer()
        {
            return foamBuffers[1];
        }
    }
}
