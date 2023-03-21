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
        Texture = 2
    }

    /// <summary>
    /// Procedural water foam generator component.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
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
        /// Specifies the texture used for the deformer.
        /// </summary>
        public Texture texture = null;

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
            // Add this water surface to the internal surface management
            RegisterInstance(this);
        }

        private void OnEnable()
        {
            // Add this water surface to the internal surface management
            RegisterInstance(this);
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
