using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Reflection;
using System.Linq.Expressions;
using System.Linq;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// The UI block that displays emission properties for materials.
    /// </summary>
    public class EmissionUIBlock : MaterialUIBlock
    {
        // Max EV Value. Equals to LightUtils.ConvertLuminanceToEv(float.MaxValue)
        // Literal value to avoid precision issue with max float and to be independent of ColorUtils.s_LightMeterCalibrationConstant.
        static float s_MaxEvValue = 130.0f;

        /// <summary>Options for emission block features. Use this to control which fields are visible.</summary>
        [Flags]
        public enum Features
        {
            /// <summary>Shows the minimal emission fields.</summary>
            None = 0,
            /// <summary>Shows the enable emission for GI field.</summary>
            EnableEmissionForGI = 1 << 0,
            /// <summary>Shows the multiply with base field.</summary>
            MultiplyWithBase = 1 << 1,
            /// <summary>Shows all the fields.</summary>
            All = ~0
        }

        static Func<LightingSettings> GetLightingSettingsOrDefaultsFallback;

        static EmissionUIBlock()
        {
            Type lightMappingType = typeof(Lightmapping);
            var getLightingSettingsOrDefaultsFallbackInfo = lightMappingType.GetMethod("GetLightingSettingsOrDefaultsFallback", BindingFlags.Static | BindingFlags.NonPublic);
            var getLightingSettingsOrDefaultsFallbackLambda = Expression.Lambda<Func<LightingSettings>>(Expression.Call(null, getLightingSettingsOrDefaultsFallbackInfo));
            GetLightingSettingsOrDefaultsFallback = getLightingSettingsOrDefaultsFallbackLambda.Compile();
        }

        internal class Styles
        {
            public static readonly GUIContent header = EditorGUIUtility.TrTextContent("Emission Inputs");

            public static GUIContent emissiveMap = new GUIContent("Emissive Map", "Specifies the emissive color (RGB) of the Material.");

            public static GUIContent albedoAffectEmissiveText = new GUIContent("Emission multiply with Base", "Specifies whether or not the emission color is multiplied by the albedo.");
            public static GUIContent useEmissiveIntensityText = new GUIContent("Use Emission Intensity", "Specifies whether to use to a HDR color or a LDR color with a separate multiplier.");
            public static GUIContent emissiveIntensityText = new GUIContent("Emission Intensity", "Emission intensity in provided Unit");
            public static GUIContent emissiveIntensityFromHDRColorText = new GUIContent("The emission intensity is from the HDR color picker in luminance", "");
            public static GUIContent emissiveExposureWeightText = new GUIContent("Exposure weight", "Controls how the camera exposure influences the perceived intensity of the emissivity. A weight of 0 means that the emissive intensity is calculated ignoring the exposure; increasing this weight progressively increases the influence of exposure on the final emissive value.");

            public static GUIContent UVEmissiveMappingText = new GUIContent("Emission UV mapping", "");
            public static GUIContent texWorldScaleText = new GUIContent("World Scale", "Sets the tiling factor HDRP applies to Planar/Trilinear mapping.");
        }

        MaterialProperty emissiveColorLDR = null;
        const string kEmissiveColorLDR = "_EmissiveColorLDR";
        MaterialProperty emissiveExposureWeight = null;
        const string kEmissiveExposureWeight = "_EmissiveExposureWeight";
        MaterialProperty useEmissiveIntensity = null;
        const string kUseEmissiveIntensity = "_UseEmissiveIntensity";
        MaterialProperty emissiveIntensityUnit = null;
        const string kEmissiveIntensityUnit = "_EmissiveIntensityUnit";
        MaterialProperty emissiveIntensity = null;
        const string kEmissiveIntensity = "_EmissiveIntensity";
        MaterialProperty emissiveColor = null;
        const string kEmissiveColor = "_EmissiveColor";
        MaterialProperty emissiveColorMap = null;
        const string kEmissiveColorMap = "_EmissiveColorMap";
        MaterialProperty UVEmissive = null;
        const string kUVEmissive = "_UVEmissive";
        MaterialProperty TexWorldScaleEmissive = null;
        const string kTexWorldScaleEmissive = "_TexWorldScaleEmissive";
        MaterialProperty UVMappingMaskEmissive = null;
        const string kUVMappingMaskEmissive = "_UVMappingMaskEmissive";
        MaterialProperty albedoAffectEmissive = null;
        const string kAlbedoAffectEmissive = "_AlbedoAffectEmissive";

        Features m_Features;

        /// <summary>
        /// Constructs an EmissionUIBlock based on the parameters.
        /// </summary>
        /// <param name="expandableBit">Bit index used to store the foldout state.</param>
        /// <param name="features">Features of the block.</param>
        public EmissionUIBlock(ExpandableBit expandableBit, Features features = Features.All)
            : base(expandableBit, Styles.header)
        {
            m_Features = features;
        }

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            emissiveColor = FindProperty(kEmissiveColor);
            emissiveColorMap = FindProperty(kEmissiveColorMap);
            emissiveIntensityUnit = FindProperty(kEmissiveIntensityUnit);
            emissiveIntensity = FindProperty(kEmissiveIntensity);
            emissiveExposureWeight = FindProperty(kEmissiveExposureWeight);
            emissiveColorLDR = FindProperty(kEmissiveColorLDR);
            useEmissiveIntensity = FindProperty(kUseEmissiveIntensity);
            albedoAffectEmissive = FindProperty(kAlbedoAffectEmissive);
            UVEmissive = FindProperty(kUVEmissive);
            TexWorldScaleEmissive = FindProperty(kTexWorldScaleEmissive);
            UVMappingMaskEmissive = FindProperty(kUVMappingMaskEmissive);
        }

        internal static void UpdateEmissiveColorFromIntensityAndEmissiveColorLDR(MaterialProperty emissiveColorLDR, MaterialProperty emissiveIntensity, MaterialProperty emissiveColor)
            => emissiveColor.colorValue = emissiveColorLDR.colorValue.linear * emissiveIntensity.floatValue;

        internal static void UpdateEmissiveColorLDRAndIntensityFromEmissiveColor(MaterialProperty emissiveColorLDR, MaterialProperty emissiveIntensity, MaterialProperty emissiveColor)
        {
            // specifies the max byte value to use when decomposing a float color into bytes with exposure
            // this is the value used by Photoshop
            const byte k_MaxByteForOverexposedColor = 191;

            float intensity = 1f;
            Color colorHDR = emissiveColor.colorValue;
            Color colorLDR = colorHDR;

            var maxColorComponent = emissiveColor.colorValue.maxColorComponent;
            if (maxColorComponent != 0f)
            {
                int maxColorComponentIndex = 0;
                if (colorLDR.r > colorLDR.g) maxColorComponentIndex = colorLDR.r > colorLDR.b ? 0 : 2;
                if (colorLDR.g > colorLDR.r) maxColorComponentIndex = colorLDR.g > colorLDR.b ? 1 : 2;

                // calibrate exposure to the max float color component
                var scaleFactor = k_MaxByteForOverexposedColor / maxColorComponent;

                // maintain maximal integrity of byte values to prevent off-by-one errors when scaling up a color one component at a time
                colorLDR.r = Math.Min(k_MaxByteForOverexposedColor, (byte)Mathf.CeilToInt(scaleFactor * colorHDR.r)) / 255f;
                colorLDR.g = Math.Min(k_MaxByteForOverexposedColor, (byte)Mathf.CeilToInt(scaleFactor * colorHDR.g)) / 255f;
                colorLDR.b = Math.Min(k_MaxByteForOverexposedColor, (byte)Mathf.CeilToInt(scaleFactor * colorHDR.b)) / 255f;

                intensity = colorHDR[maxColorComponentIndex] / colorLDR[maxColorComponentIndex];
            }

            colorLDR.a = 1.0f;
            emissiveIntensity.floatValue = intensity;
            emissiveColorLDR.colorValue = colorLDR.gamma;
        }

        internal static void DoEmissiveIntensityGUI(MaterialEditor materialEditor, MaterialProperty emissiveIntensity, MaterialProperty emissiveIntensityUnit)
        {
            bool unitIsMixed = emissiveIntensityUnit.hasMixedValue;
            bool intensityIsMixed = unitIsMixed || emissiveIntensity.hasMixedValue;

            float indent = 15 * EditorGUI.indentLevel;
            const int k_ValueUnitSeparator = 2;
            const int k_UnitWidth = 100;
            Rect valueRect = EditorGUILayout.GetControlRect();
            valueRect.width += indent - k_ValueUnitSeparator - k_UnitWidth;
            Rect unitRect = valueRect;
            unitRect.x += valueRect.width - indent + k_ValueUnitSeparator;
            unitRect.width = k_UnitWidth + .5f;

            {
                EditorGUI.showMixedValue = intensityIsMixed;
                EmissiveIntensityUnit unit = (EmissiveIntensityUnit)emissiveIntensityUnit.floatValue;

                if (unitIsMixed)
                {
                    using (new EditorGUI.DisabledScope(true))
                        materialEditor.ShaderProperty(valueRect, emissiveIntensity, Styles.emissiveIntensityText);
                }
                else
                {
                    if (!intensityIsMixed && unit == EmissiveIntensityUnit.EV100)
                    {
                        float evValue = LightUtils.ConvertLuminanceToEv(emissiveIntensity.floatValue);
                        evValue = EditorGUI.FloatField(valueRect, Styles.emissiveIntensityText, evValue);
                        evValue = Mathf.Clamp(evValue, 0, s_MaxEvValue);
                        emissiveIntensity.floatValue = LightUtils.ConvertEvToLuminance(evValue);
                    }
                    else
                    {
                        EditorGUI.BeginChangeCheck();
                        materialEditor.ShaderProperty(valueRect, emissiveIntensity, Styles.emissiveIntensityText);
                        if (EditorGUI.EndChangeCheck())
                            emissiveIntensity.floatValue = Mathf.Clamp(emissiveIntensity.floatValue, 0, float.MaxValue);
                    }
                }

                EditorGUI.showMixedValue = emissiveIntensityUnit.hasMixedValue;
                EditorGUI.BeginChangeCheck();
                var newUnit = (EmissiveIntensityUnit)EditorGUI.EnumPopup(unitRect, unit);
                if (EditorGUI.EndChangeCheck())
                    emissiveIntensityUnit.floatValue = (float)newUnit;
            }
            EditorGUI.showMixedValue = false;
        }

        /// <summary>
        /// GUI callback when the header is open
        /// </summary>
        protected override void OnGUIOpen()
        {
            EditorGUI.BeginChangeCheck();
            materialEditor.ShaderProperty(useEmissiveIntensity, Styles.useEmissiveIntensityText);
            bool updateEmissiveColor = EditorGUI.EndChangeCheck();

            // This flag allows us to track is a material has a non-null emission color. That would require us to enable the target pass
            if (useEmissiveIntensity.floatValue == 0)
            {
                DoEmissiveTextureProperty(emissiveColor);
                EditorGUILayout.HelpBox(Styles.emissiveIntensityFromHDRColorText.text, MessageType.Info, true);
            }
            else
            {
                if (updateEmissiveColor)
                    UpdateEmissiveColorLDRAndIntensityFromEmissiveColor(emissiveColorLDR, emissiveIntensity, emissiveColor);

                EditorGUI.BeginChangeCheck();
                DoEmissiveTextureProperty(emissiveColorLDR);
                DoEmissiveIntensityGUI(materialEditor, emissiveIntensity, emissiveIntensityUnit);
                if (EditorGUI.EndChangeCheck())
                    UpdateEmissiveColorFromIntensityAndEmissiveColorLDR(emissiveColorLDR, emissiveIntensity, emissiveColor);
            }

            materialEditor.ShaderProperty(emissiveExposureWeight, Styles.emissiveExposureWeightText);

            if ((m_Features & Features.MultiplyWithBase) != 0)
                materialEditor.ShaderProperty(albedoAffectEmissive, Styles.albedoAffectEmissiveText);

            // Emission for GI?
            if ((m_Features & Features.EnableEmissionForGI) != 0)
            {
                // Change the GI emission flag and fix it up with emissive as black if necessary.
                materialEditor.LightmapEmissionFlagsProperty(0, true);
            }
        }

        internal static void DoEmissiveTextureProperty(MaterialEditor materialEditor, MaterialProperty texture, MaterialProperty color)
        {
            materialEditor.TexturePropertySingleLine(Styles.emissiveMap, texture, color);
        }

        void DoEmissiveTextureProperty(MaterialProperty color)
        {
            DoEmissiveTextureProperty(materialEditor, emissiveColorMap, color);

            if (materials.All(m => m.GetTexture(kEmissiveColorMap)))
            {
                EditorGUI.indentLevel++;
                if (UVEmissive != null) // Unlit does not have UVEmissive
                {
                    materialEditor.ShaderProperty(UVEmissive, Styles.UVEmissiveMappingText);
                    UVEmissiveMapping uvEmissiveMapping = (UVEmissiveMapping)UVEmissive.floatValue;

                    float X, Y, Z, W;
                    X = (uvEmissiveMapping == UVEmissiveMapping.UV0) ? 1.0f : 0.0f;
                    Y = (uvEmissiveMapping == UVEmissiveMapping.UV1) ? 1.0f : 0.0f;
                    Z = (uvEmissiveMapping == UVEmissiveMapping.UV2) ? 1.0f : 0.0f;
                    W = (uvEmissiveMapping == UVEmissiveMapping.UV3) ? 1.0f : 0.0f;

                    UVMappingMaskEmissive.colorValue = new Color(X, Y, Z, W);

                    if ((uvEmissiveMapping == UVEmissiveMapping.Planar) || (uvEmissiveMapping == UVEmissiveMapping.Triplanar))
                    {
                        materialEditor.ShaderProperty(TexWorldScaleEmissive, Styles.texWorldScaleText);
                    }
                }

                if (UVEmissive == null || (UVEmissiveMapping)UVEmissive.floatValue != UVEmissiveMapping.SameAsBase)
                    materialEditor.TextureScaleOffsetProperty(emissiveColorMap);
                EditorGUI.indentLevel--;
            }
        }
    }
}
