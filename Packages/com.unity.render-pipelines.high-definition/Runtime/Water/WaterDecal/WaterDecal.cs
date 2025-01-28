using System.Collections.Generic;
using Unity.Mathematics;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Water decal component.
    /// </summary>
    [HDRPHelpURL("water-decals-and-masking-in-the-water-system")]
    [ExecuteInEditMode]
    public partial class WaterDecal : MonoBehaviour
    {
        internal enum PassType
        {
            Deformation = 0,
            Foam = 1,
            SimulationMask = 2,
            LargeCurrent = 3,
            RipplesCurrent = 4,
        }

        #region General
        /// <summary>
        /// The scaling mode to apply to this Foam Generator.
        /// </summary>
        [Tooltip("Specify the scaling mode")]
        public DecalScaleMode scaleMode = DecalScaleMode.InheritFromHierarchy;

        /// <summary>
        /// Specifies the size of the deformer in meters.
        /// </summary>
        public Vector2 regionSize = new Vector2(20.0f, 20.0f);

        /// <summary>
        /// Specifies the amplitude of the deformation.
        /// </summary>
        public float amplitude = 2.0f;

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

        /// <summary>Scale that should be used for rendering and handles.</summary>
        internal float3 effectiveScale => scaleMode == DecalScaleMode.InheritFromHierarchy ? transform.lossyScale : Vector3.one;
        #endregion

        #region Material Management
        /// <summary>
        /// Specifies the resolution when written inside the atlas.
        /// </summary>
        public Vector2Int resolution = new Vector2Int(128, 128);

        /// <summary>
        /// Frequency of update of the Material in the atlas.
        /// </summary>
        [Tooltip("Frequency of update of the Material in the atlas.")]
        public CustomRenderTextureUpdateMode updateMode = CustomRenderTextureUpdateMode.OnLoad;

        /// <summary>
        /// Specifies the material used for the water decal.
        /// </summary>
        [Tooltip("Specifies the material used for the water decal.")]
        public Material material = null;

        internal int updateCount = 0;

        /// <summary>
        /// Triggers a render of the material in the deformer atlas.
        /// </summary>
        public void RequestUpdate()
        {
            updateCount++;
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
            return material != null;
            #endif
        }

        internal int GetMaterialAtlasingId()
        {
            // If material has a property block, we can't reuse the atlas slot
            return HasPropertyBlock() ? GetInstanceID(): material.GetInstanceID();
        }
        #endregion

        #region Instance Management
        internal static List<WaterDecal> instances = new List<WaterDecal>();

        internal static void RegisterInstance(WaterDecal decal)
        {
            instances.Add(decal);
        }

        internal static void UnregisterInstance(WaterDecal decal)
        {
            if (instances.Contains(decal))
                instances.Remove(decal);
        }

        internal void Reset()
        {
            regionSize = new Vector2(10f, 10.0f);
            resolution = new Vector2Int(256, 256);
            scaleMode = DecalScaleMode.InheritFromHierarchy;
            updateMode = CustomRenderTextureUpdateMode.OnLoad;
            updateCount++;
            material = GraphicsSettings.GetRenderPipelineSettings<WaterSystemRuntimeResources>().waterDecalMaterial;

            amplitude = 2.0f;
            surfaceFoamDimmer = 1.0f;
            deepFoamDimmer = 1.0f;
        }
        #endregion

        #region MonoBehavior Methods
        private void OnEnable()
        {
            RegisterInstance(this);

            if (updateMode == CustomRenderTextureUpdateMode.OnLoad)
                updateCount++;
        }

        private void OnDisable()
        {
            UnregisterInstance(this);
        }
        #endregion
    }
}
