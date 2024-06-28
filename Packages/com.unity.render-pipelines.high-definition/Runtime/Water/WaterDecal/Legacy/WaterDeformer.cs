using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Controls the type of the procedural water deformer.
    /// </summary>
    [Obsolete("WaterDeformer has been deprecated. Use WaterDecal instead.")]
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
    [ExecuteInEditMode]
    public partial class WaterDeformer : WaterDecal
    {
        #region General
        /// <summary>
        /// Specifies the type of the deformer. This parameter defines which parameters will be used to render it.
        /// </summary>
        [Obsolete("WaterDeformer has been deprecated. Use WaterDecal instead.")]
        public WaterDeformerType type = WaterDeformerType.Sphere;
        #endregion

        #region Box Deformer
        /// <summary>
        /// Specifies the range that is used to blend the box deformer.
        /// </summary>
        [Obsolete("WaterDeformer has been deprecated. Use WaterDecal instead.")]
        public Vector2 boxBlend;

        /// <summary>
        /// When enabled, the box deformer will have a cubic blend on the edges (instead of procedural).
        /// </summary>
        [Obsolete("WaterDeformer has been deprecated. Use WaterDecal instead.")]
        public bool cubicBlend = true;
        #endregion

        #region Shore Wave Deformer
        /// <summary>
        /// Specifies the wave length of the individual waves of the shore wave deformer.
        /// </summary>
        [Obsolete("WaterDeformer has been deprecated. Use WaterDecal instead.")]
        public float waveLength = 3.0f;

        /// <summary>
        /// Specifies the wave repetition of the waves. A higher value implies that additional waves will be skipped.
        /// </summary>
        [Obsolete("WaterDeformer has been deprecated. Use WaterDecal instead.")]
        public int waveRepetition = 10;

        /// <summary>
        /// Specifies the speed of the waves in kilometers per hour.
        /// </summary>
        [Obsolete("WaterDeformer has been deprecated. Use WaterDecal instead.")]
        public float waveSpeed = 15.0f;

        /// <summary>
        /// Specifies the offset in the waves' position.
        /// </summary>
        [Obsolete("WaterDeformer has been deprecated. Use WaterDecal instead.")]
        public float waveOffset = 0.0f;

        /// <summary>
        /// Specifies the blend size on the length of the deformer's region.
        /// </summary>
        [Obsolete("WaterDeformer has been deprecated. Use WaterDecal instead.")]
        public Vector2 waveBlend = new Vector2(0.3f, 0.6f);

        /// <summary>
        /// Specifies the range in which the waves break and generate surface foam.
        /// </summary>
        [Obsolete("WaterDeformer has been deprecated. Use WaterDecal instead.")]
        public Vector2 breakingRange = new Vector2(0.7f, 0.8f);

        /// <summary>
        /// Specifies the range in which the waves generate deep foam.
        /// </summary>
        [Obsolete("WaterDeformer has been deprecated. Use WaterDecal instead.")]
        public Vector2 deepFoamRange = new Vector2(0.5f, 0.8f);
        #endregion

        #region BowWave Deformer
        /// <summary>
        /// Specifies the elevation of outer part of the bow wave.
        /// </summary>
        [Obsolete("WaterDeformer has been deprecated. Use WaterDecal instead.")]
        public float bowWaveElevation = 1.0f;
        #endregion

        #region Texture Deformer
        /// <summary>
        /// Specifies the range of the texture deformer
        /// </summary>
        [Obsolete("WaterDeformer has been deprecated. Use WaterDecal instead.")]
        public Vector2 range = new Vector2(0.0f, 1.0f);

        /// <summary>
        /// Specifies the texture used for the deformer.
        /// </summary>
        [Obsolete("WaterDeformer has been deprecated. Use WaterDecal instead.")]
        public Texture texture = null;
        #endregion

        [SerializeField] int version = 0;

        private void Awake()
        {
#pragma warning disable 618 // Type or member is obsolete
            if (version != 0 || type == WaterDeformerType.Material)
                return;

        #if UNITY_EDITOR
            int typeInt = 0;
            resolution.Set(64, 64);
            updateMode = CustomRenderTextureUpdateMode.OnLoad;

            material = new Material(GraphicsSettings.GetRenderPipelineSettings<WaterSystemRuntimeResources>().waterDecalMigrationShader);
            material.name = gameObject.name;
            material.SetFloat("_AffectFoam", 0.0f);

            var size = regionSize * 0.5f * (Vector2)effectiveScale.xz;

            if (type == WaterDeformerType.Sphere)
            {
                typeInt = 0;
            }
            if (type == WaterDeformerType.Box)
            {
                typeInt = 1;
                material.SetVector("_Blend_Distance", new Vector4(boxBlend.x / size.x, boxBlend.y / size.y, 0, 0));
                material.SetFloat("_Cubic_Blend", cubicBlend ? 1.0f : 0.0f);
            }
            if (type == WaterDeformerType.BowWave)
            {
                typeInt = 2;
                material.SetFloat("_Elevation", bowWaveElevation / (amplitude != 0.0f ? amplitude : 1));
            }
            if (type == WaterDeformerType.ShoreWave)
            {
                typeInt = 3;
                resolution.Set(256, 256);
                material.SetFloat("_Wave_Length", waveLength / Mathf.Max(size.x, size.y));
                material.SetFloat("_Skipped_Waves", waveRepetition);
                material.SetFloat("_Wave_Speed", waveSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond / Mathf.Max(size.x, size.y));
                material.SetFloat("_Wave_Offset", waveOffset / Mathf.Max(size.x, size.y));
                material.SetVector("_Wave_Blend", waveBlend);
                material.SetVector("_Breaking_Range", breakingRange);
                material.SetVector("_Deep_Foam_Range", deepFoamRange);
                material.SetFloat("_AffectFoam", 1.0f);
                updateMode = CustomRenderTextureUpdateMode.Realtime;
            }
            if (type == WaterDeformerType.Texture)
            {
                typeInt = 4;
                material.SetTexture("_Deformation_Texture", texture);
                material.SetFloat("_Remap_Min", range.x);
                material.SetFloat("_Remap_Max", range.y);
                material.SetTexture("_Foam_Texture", Texture2D.blackTexture);

                if (texture is CustomRenderTexture crt)
                    updateMode = crt.updateMode;

                // Clear ref to separate asset
                texture = null;
            }

            material.SetFloat("_TYPE", typeInt);
            type = WaterDeformerType.Material;
            version = 1;

            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.MaterialEditor.ApplyMaterialPropertyDrawers(material);
            HDMaterial.ValidateMaterial(material);
        #else
            Debug.LogError($"Water Deformer '{gameObject.name}' was not migrated. It will not render correctly.");
        #endif
#pragma warning restore 618
        }
    }
}
