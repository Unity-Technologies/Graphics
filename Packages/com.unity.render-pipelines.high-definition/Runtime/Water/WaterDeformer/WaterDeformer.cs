using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Controls the type of the procedural water deformer.
    /// </summary>
    [GenerateHLSL(PackingRules.Exact)]
    public enum WaterDeformerType
    {
        /// <summary>
        /// Sphere deformer.
        /// </summary>
        Sphere = 0,
        /// <summary>
        /// Box deformer.
        /// </summary>
        Box = 1,
        /// <summary>
        /// Bow Wave deformer.
        /// </summary>
        BowWave = 2,
        /// <summary>
        /// Shore Wave deformer.
        /// </summary>
        ShoreWave = 3,
        /// <summary>
        /// Texture deformer.
        /// </summary>
        Texture = 4,
        /// <summary>
        /// Material deformer.
        /// </summary>
        Material = 5,
    }

    /// <summary>
    /// Water deformer component.
    /// </summary>
    [DisallowMultipleComponent]
    [HDRPHelpURL("WaterSystem-waterdeformer")]
    [ExecuteInEditMode]
    public partial class WaterDeformer : MonoBehaviour
    {
        internal enum PassType
        {
            Deformer = 0,
            FoamGenerator = 1,
        }

        #region General
        /// <summary>
        /// Specifies the type of the deformer. This parameter defines which parameters will be used to render it.
        /// </summary>
        public WaterDeformerType type = WaterDeformerType.Sphere;

        /// <summary>
        /// Specifies the amplitude of the deformer. This parameter is used differently based on the deformer type.
        /// </summary>
        public float amplitude = 2.0f;

        /// <summary>
        /// Specifies the size of the deformer in meters.
        /// </summary>
        public Vector2 regionSize = new Vector2(20.0f, 20.0f);

        /// <summary>
        /// The scaling mode to apply to this Foam Generator.
        /// </summary>
        [Tooltip("Specify the scaling mode")]
        public DecalScaleMode scaleMode = DecalScaleMode.ScaleInvariant;
        #endregion

        #region Box Deformer
        /// <summary>
        /// Specifies the range that is used to blend the box deformer.
        /// </summary>
        [Min(0.0f)]
        public Vector2 boxBlend;

        /// <summary>
        /// When enabled, the box deformer will have a cubic blend on the edges (instead of procedural).
        /// </summary>
        public bool cubicBlend = true;
        #endregion

        #region Shore Wave Deformer
        /// <summary>
        /// Specifies the wave length of the individual waves of the shore wave deformer.
        /// </summary>
        [Min(1.0f)]
        public float waveLength = 3.0f;

        /// <summary>
        /// Specifies the wave repetition of the waves. A higher value implies that additional waves will be skipped.
        /// </summary>
        [Min(1)]
        public int waveRepetition = 10;

        /// <summary>
        /// Specifies the speed of the waves in kilometers per hour.
        /// </summary>
        public float waveSpeed = 15.0f;

        /// <summary>
        /// Specifies the offset in the waves' position.
        /// </summary>
        public float waveOffset = 0.0f;

        /// <summary>
        /// Specifies the blend size on the length of the deformer's region.
        /// </summary>
        public Vector2 waveBlend = new Vector2(0.3f, 0.6f);

        /// <summary>
        /// Specifies the range in which the waves break and generate surface foam.
        /// </summary>
        public Vector2 breakingRange = new Vector2(0.7f, 0.8f);

        /// <summary>
        /// Specifies the range in which the waves generate deep foam.
        /// </summary>
        public Vector2 deepFoamRange = new Vector2(0.5f, 0.8f);
        #endregion

        #region BowWave Deformer
        /// <summary>
        /// Specifies the elevation of outer part of the bow wave.
        /// </summary>
        public float bowWaveElevation = 1.0f;
        #endregion

        #region Texture Deformer
        /// <summary>
        /// Specifies the range of the texture deformer
        /// </summary>
        public Vector2 range = new Vector2(0.0f, 1.0f);

        /// <summary>
        /// Specifies the texture used for the deformer.
        /// </summary>
        [Tooltip("Specifies the texture used for the deformer.")]
        public Texture texture = null;
        #endregion

        #region Material Deformer
        /// <summary>
        /// Specifies the resolution when written inside the atlas.
        /// </summary>
        public Vector2Int resolution = new Vector2Int(256, 256);

        /// <summary>
        /// Frequency of update of the Material in the atlas.
        /// </summary>
        [Tooltip("Frequency of update of the Material in the atlas.")]
        public CustomRenderTextureUpdateMode updateMode = CustomRenderTextureUpdateMode.OnLoad;

        /// <summary>
        /// Specifies the material used for the deformer.
        /// </summary>
        [Tooltip("Specifies the material used for the deformer.")]
        public Material material = null;

        internal bool shouldUpdate = false;

        /// <summary>
        /// Triggers a render of the material in the deformer atlas.
        /// </summary>
        public void RequestUpdate()
        {
            shouldUpdate = true;
        }

        internal MaterialPropertyBlock mpb;

        /// <summary>
        /// Override per-deformer material parameters. This is more memory efficient than having one complete distinct Material per deformer but is recommended when only a few properties of a Material overriden.
        /// </summary>
        /// <param name="properties">Property block with values you want to override.</param>
        public void SetPropertyBlock(MaterialPropertyBlock properties)
        {
            mpb = properties;
        }

        /// <summary>
        /// Returns true if the Deformer has a material property block attached via SetPropertyBlock.
        /// </summary>
        /// <returns>Returns true if the Deformer has a material property block attached via SetPropertyBlock.</returns>
        public bool HasPropertyBlock()
        {
            return mpb != null;
        }

        internal bool IsValidMaterial()
        {
            #if UNITY_EDITOR
            return material != null && material.GetTag("ShaderGraphTargetId", false, null) == "WaterDecalSubTarget";
            #else
            return true;
            #endif
        }

        internal int GetMaterialAtlasingId()
        {
            // If material has a property block, we can't reuse the atlas slot
            if (HasPropertyBlock())
                return GetInstanceID();
            else
                return material.GetInstanceID();
        }
        #endregion

        #region Foam
        /// <summary>
        /// Specifies the dimmer for the deep foam generated by the deformer.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float deepFoamDimmer = 1.0f;

        /// <summary>
        /// Specifies the dimmer for the surface foam generated by the deformer.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float surfaceFoamDimmer = 1.0f;
        #endregion

        #region Instance Management
        // Management to avoid memory allocations at fetch time
        internal static HashSet<WaterDeformer> instances = new HashSet<WaterDeformer>();
        internal static WaterDeformer[] instancesAsArray = null;
        internal static int instanceCount = 0;

        internal static void RegisterInstance(WaterDeformer deformer)
        {
            instances.Add(deformer);
            instanceCount = instances.Count;
            if (instanceCount > 0)
            {
                instancesAsArray = new WaterDeformer[instanceCount];
                instances.CopyTo(instancesAsArray);
            }
            else
            {
                instancesAsArray = null;
            }
        }

        internal static void UnregisterInstance(WaterDeformer deformer)
        {
            instances.Remove(deformer);
            instanceCount = instances.Count;
            if (instanceCount > 0)
            {
                instancesAsArray = new WaterDeformer[instanceCount];
                instances.CopyTo(instancesAsArray);
            }
            else
            {
                instancesAsArray = null;
            }
        }
        #endregion

        #region MonoBehavior Methods
        private void Start()
        {
            // Add this water surface to the internal surface management
            RegisterInstance(this);
        }

        private void Awake()
        {
            // Add this water surface to the internal surface management
            RegisterInstance(this);
        }

        private void OnEnable()
        {
            // Add this water surface to the internal surface management
            RegisterInstance(this);

            if (updateMode == CustomRenderTextureUpdateMode.OnLoad)
                shouldUpdate = true;
        }

        private void OnDisable()
        {
            // Remove this water surface from the internal surface management
            UnregisterInstance(this);
        }

        void OnDestroy()
        {
            // Remove this water surface from the internal surface management
            UnregisterInstance(this);
        }
        #endregion
    }
}
