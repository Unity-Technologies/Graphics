using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class WaterDecalSurfaceOptionsUIBlock : MaterialUIBlock
    {
        internal class Styles
        {
            public static GUIContent header { get; } = EditorGUIUtility.TrTextContent("Surface Options");

            public static GUIContent affectsDeformationText = new GUIContent("Affect Deformation", "When enabled, this decal can affect deformation.");
            public static GUIContent affectsFoamText = new GUIContent("Affect Foam", "When enabled, this decal can affect foam.");
            public static GUIContent affectsSimulationMaskText = new GUIContent("Affect Simulation Mask", "When enabled, this decal can affect simulation mask and simulation foam mask.");
            public static GUIContent affectsLargeCurrentText = new GUIContent("Affect Large Current", "When enabled, this decal can affect the swell current on oceans and the agitation current current on rivers.");
            public static GUIContent affectsRipplesCurrentText = new GUIContent("Affect Ripples Current", "When enabled, this decal can affect ripples current.");
            public static string warningSupportWaterHorizontalDeformation = "The current HDRP Asset only support vertical water deformation. If the shader graph uses Horizontal Deformation output, enable horizontal deformation in the current HDRP asset to allow for 3D deformation.";
        }

        MaterialProperty affectsDeformation;
        MaterialProperty affectsFoam;
        MaterialProperty affectsSimulationMask;
        MaterialProperty affectsLargeCurrent;
        MaterialProperty affectsRipplesCurrent;

        /// </summary>
        /// <summary>
        /// Constructs a DecalSurfaceOptionsUIBlock based on the parameters.
        /// <param name="expandableBit">Bit index used to store the foldout state.</param>
        public WaterDecalSurfaceOptionsUIBlock(ExpandableBit expandableBit)
            : base(expandableBit, Styles.header)
        {
        }

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            affectsDeformation = FindProperty(HDShaderIDs.kAffectsDeformation);
            affectsFoam = FindProperty(HDShaderIDs.kAffectsFoam);
            affectsSimulationMask = FindProperty(HDShaderIDs.kAffectsSimulationMask);
            affectsLargeCurrent = FindProperty(HDShaderIDs.kAffectsLargeCurrent);
            affectsRipplesCurrent = FindProperty(HDShaderIDs.kAffectsRipplesCurrent);
        }

        void AffectProperty(MaterialProperty property, GUIContent style, bool requireGlobalSetting = false)
        {
            if (property == null)
                return;

            materialEditor.ShaderProperty(property, style);

            if (requireGlobalSetting && property.floatValue > 0.0f && !GraphicsSettings.GetRenderPipelineSettings<WaterSystemGlobalSettings>().waterDecalMaskAndCurrent)
            {
                EditorGUILayout.Space();
                HDEditorUtils.GlobalSettingsHelpBox<WaterSystemGlobalSettings>("Water decals affecting mask and current is not enabled in the HDRP Global Settings.", MessageType.Error);
            }
        }

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        protected override void OnGUIOpen()
        {
            AffectProperty(affectsDeformation, Styles.affectsDeformationText);
            AffectProperty(affectsFoam, Styles.affectsFoamText);
            AffectProperty(affectsSimulationMask, Styles.affectsSimulationMaskText, true);
            AffectProperty(affectsLargeCurrent, Styles.affectsLargeCurrentText, true);
            AffectProperty(affectsRipplesCurrent, Styles.affectsRipplesCurrentText, true);

            // We display a warning in the inspector if affect deformation is checked and supportWaterHorizontalDeformation isn't in the current HDRP asset
            bool supportHorizontalDeformation = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportWaterHorizontalDeformation;
            if (affectsDeformation != null && affectsDeformation.floatValue > 0.0f && !supportHorizontalDeformation)
            {
                EditorGUILayout.Space();
                HDEditorUtils.QualitySettingsHelpBox(Styles.warningSupportWaterHorizontalDeformation, MessageType.Warning, HDRenderPipelineUI.ExpandableGroup.Rendering, "m_RenderPipelineSettings.supportWaterHorizontalDeformation");
            }
        }
    }
}
