using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Controls the type of the procedural foam generator.
    /// </summary>
    [GenerateHLSL(PackingRules.Exact)]
    public enum WaterFoamGeneratorType
    {
        /// <summary>
        /// Disk foam generator.
        /// </summary>
        Disk = 0,
        /// <summary>
        /// Square foam generator.
        /// </summary>
        Rectangle = 1,
        /// <summary>
        /// Texture foam generator.
        /// </summary>
        Texture = 2,
        /// <summary>
        /// Material foam generator.
        /// </summary>
        Material = 3,
    }

    /// <summary>
    /// Procedural water foam generator component.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    [HDRPHelpURL("WaterSystem-foam")]
    public partial class WaterFoamGenerator : MonoBehaviour
    {
        /// <summary>
        /// Specifies the type of the generator. This parameter defines which parameters will be used to render it.
        /// </summary>
        public WaterFoamGeneratorType type = WaterFoamGeneratorType.Disk;

        /// <summary>
        /// Specifies the size of the generator in meters.
        /// </summary>
        public Vector2 regionSize = new Vector2(20.0f, 20.0f);

        /// <summary>
        /// Specifies the texture used for the foam.
        /// </summary>
        public Texture texture = null;

        #region Material Generator
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
        /// Specifies the material used for the foam.
        /// </summary>
        [Tooltip("Specifies the material used for the generator.")]
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
        /// Override per-generator material parameters. This is more memory efficient than having one complete distinct Material per generator but is recommended when only a few properties of a Material overriden.
        /// </summary>
        /// <param name="properties">Property block with values you want to override.</param>
        public void SetPropertyBlock(MaterialPropertyBlock properties)
        {
            mpb = properties;
        }

        /// <summary>
        /// Returns true if the Foam Generator has a material property block attached via SetPropertyBlock.
        /// </summary>
        /// <returns>Returns true if the Foam Generator has a material property block attached via SetPropertyBlock.</returns>
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

        /// <summary>
        /// Specifies the dimmer for the surface foam.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float surfaceFoamDimmer = 1.0f;

        /// <summary>
        /// Specifies a dimmer for the deep foam.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float deepFoamDimmer = 1.0f;

        /// <summary>
        /// The scaling mode to apply to this Foam Generator.
        /// </summary>
        [Tooltip("Specify the scaling mode")]
        public DecalScaleMode scaleMode = DecalScaleMode.ScaleInvariant;

        internal Vector2 scale
        {
            get
            {
                Vector3 scale = scaleMode == DecalScaleMode.InheritFromHierarchy ? transform.lossyScale : Vector3.one;
                return new Vector2(scale.x, scale.z);
            }
        }

        #region Instance Management
        // Management to avoid memory allocations at fetch time
        internal static HashSet<WaterFoamGenerator> instances = new HashSet<WaterFoamGenerator>();
        internal static WaterFoamGenerator[] instancesAsArray = null;
        internal static int instanceCount = 0;

        internal static void RegisterInstance(WaterFoamGenerator foamGenerator)
        {
            instances.Add(foamGenerator);
            instanceCount = instances.Count;
            if (instanceCount > 0)
            {
                instancesAsArray = new WaterFoamGenerator[instanceCount];
                instances.CopyTo(instancesAsArray);
            }
            else
            {
                instancesAsArray = null;
            }
        }

        internal static void UnregisterInstance(WaterFoamGenerator foamGenerator)
        {
            instances.Remove(foamGenerator);
            instanceCount = instances.Count;
            if (instanceCount > 0)
            {
                instancesAsArray = new WaterFoamGenerator[instanceCount];
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
            k_Migration.Migrate(this);

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
