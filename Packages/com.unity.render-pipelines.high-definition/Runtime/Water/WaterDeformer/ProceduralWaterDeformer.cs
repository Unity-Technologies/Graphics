using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Controls the type of the procedural water deformer.
    /// </summary>
    [GenerateHLSL(PackingRules.Exact)]
    public enum ProceduralWaterDeformerType
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
        /// Sine Wave deformer.
        /// </summary>
        SineWave = 3
    }

    /// <summary>
    /// Water deformer component.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public partial class ProceduralWaterDeformer : MonoBehaviour
    {
        #region General
        /// <summary>
        /// Specifies the type of the deformer. This parameter defines which parameters will be used to render it.
        /// </summary>
        public ProceduralWaterDeformerType type = ProceduralWaterDeformerType.Sphere;

        /// <summary>
        /// Specifies the amplitude of the deformer. This parameter is used differently based on the deformer type.
        /// </summary>
        public float amplitude = 2.0f;

        /// <summary>
        /// Specifies the size of the deformer in meters.
        /// </summary>
        public Vector2 regionSize = new Vector2(20.0f, 20.0f);
        #endregion

        #region Box Deformer
        /// <summary>
        /// Specifies the range that is used to blend the box deformer.
        /// </summary>
        public Vector2 boxBlend;

        /// <summary>
        /// Specifies the type of blend that is used for the box deformers (Linear or cubic).
        /// </summary>
        public bool cubicBlend = true;
        #endregion

        #region SinWave Deformer
        /// <summary>
        /// Specifies the wave length of the individual waves of the sine wave deformer.
        /// </summary>
        public float waveLength = 3.0f;

        /// <summary>
        /// Specifies the wave reptition of the waves. A higher value implies that additional waves will be skipped.
        /// </summary>
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
        /// Specifies the location of the wave peak in the sine wave region.
        /// </summary>
        public float peakLocation = 0.7f;

        /// <summary>
        /// Specifies the blend size on the length of the deformer's region.
        /// </summary>
        public Vector2 waveBlend = new Vector2(0.3f, 0.6f);
        #endregion

        #region BowWave Deformer
        /// <summary>
        /// Specifies the elevation of outer part of the bow wave.
        /// </summary>
        public float bowWaveElevation = 1.0f;
        #endregion

        #region Instance Management
        // Management to avoid memory allocations at fetch time
        internal static HashSet<ProceduralWaterDeformer> instances = new HashSet<ProceduralWaterDeformer>();
        internal static ProceduralWaterDeformer[] instancesAsArray = null;
        internal static int instanceCount = 0;

        internal static void RegisterInstance(ProceduralWaterDeformer deformer)
        {
            instances.Add(deformer);
            instanceCount = instances.Count;
            if (instanceCount > 0)
            {
                instancesAsArray = new ProceduralWaterDeformer[instanceCount];
                instances.CopyTo(instancesAsArray);
            }
            else
            {
                instancesAsArray = null;
            }
        }

        internal static void UnregisterInstance(ProceduralWaterDeformer deformer)
        {
            instances.Remove(deformer);
            instanceCount = instances.Count;
            if (instanceCount > 0)
            {
                instancesAsArray = new ProceduralWaterDeformer[instanceCount];
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

        void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireMesh(Resources.GetBuiltinResource<Mesh>("Cube.fbx"), transform.position, Quaternion.Euler(0, transform.eulerAngles.y, 0), new Vector3(regionSize.x, amplitude * 2, regionSize.y));
        }
    }
}
