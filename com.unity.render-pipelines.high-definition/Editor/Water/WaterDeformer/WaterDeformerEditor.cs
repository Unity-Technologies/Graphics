using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.EditorGUI;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(WaterDeformer))]
    sealed partial class WaterDeformerEditor : Editor
    {
        // General parameters
        SerializedProperty m_Type;
        SerializedProperty m_Amplitude;
        SerializedProperty m_RegionSize;

        // Waves parameters
        SerializedProperty m_WaveLength;
        SerializedProperty m_WaveRepetition;
        SerializedProperty m_WaveSpeed;
        SerializedProperty m_WaveOffset;
        SerializedProperty m_WaveBlend;
        SerializedProperty m_BreakingRange;
        SerializedProperty m_DeepFoamRange;

        // Bow wave parameters
        SerializedProperty m_BowWaveElevation;

        // Box parameters
        SerializedProperty m_BoxBlend;
        SerializedProperty m_CubicBlend;

        // Texture params
        SerializedProperty m_Range;
        SerializedProperty m_Texture;

        // Foam
        SerializedProperty m_SurfaceFoamDimmer;
        SerializedProperty m_DeepFoamDimmer;

        void OnEnable()
        {
            var o = new PropertyFetcher<WaterDeformer>(serializedObject);

            // General parameters
            m_Type = o.Find(x => x.type);
            m_Amplitude = o.Find(x => x.amplitude);
            m_RegionSize = o.Find(x => x.regionSize);

            // Waves parameters
            m_WaveLength = o.Find(x => x.waveLength);
            m_WaveRepetition = o.Find(x => x.waveRepetition);
            m_WaveSpeed = o.Find(x => x.waveSpeed);
            m_WaveOffset = o.Find(x => x.waveOffset);
            m_WaveBlend = o.Find(x => x.waveBlend);
            m_BreakingRange = o.Find(x => x.breakingRange);
            m_DeepFoamRange = o.Find(x => x.deepFoamRange);

            // Bow wave parameters
            m_BowWaveElevation = o.Find(x => x.bowWaveElevation);

            // Box parameters
            m_BoxBlend = o.Find(x => x.boxBlend);
            m_CubicBlend = o.Find(x => x.cubicBlend);

            // Texture parameters
            m_Range = o.Find(x => x.range);
            m_Texture = o.Find(x => x.texture);

            // Foam
            m_SurfaceFoamDimmer = o.Find(x => x.surfaceFoamDimmer);
            m_DeepFoamDimmer = o.Find(x => x.deepFoamDimmer);
        }

        // General parameters
        static public readonly GUIContent k_AmplitudeText = EditorGUIUtility.TrTextContent("Amplitude", "Sets the vertical amplitude of the deformation.");
        static public readonly GUIContent k_TypeText = EditorGUIUtility.TrTextContent("Type", "Specifies the type of the deformer. Shore Wave will generate foam by default without any additional Foam Generator.");
        static public readonly GUIContent k_RegionSizeText = EditorGUIUtility.TrTextContent("Region Size", "Controls the region size of the deformer. Outside this region, there will be no deformation.");

        // Waves parameters
        static public readonly GUIContent k_WaveRegionSizeText = EditorGUIUtility.TrTextContent("Region Size", "Sets the region size of the deformer. Outside this region, there will be no deformation");
        static public readonly GUIContent k_WaveBlendSizeText = EditorGUIUtility.TrTextContent("Blend Range", "Specifies the range on the local Z axis where the shore waves have their maximal amplitude.");
        static public readonly GUIContent k_WaveLengthText = EditorGUIUtility.TrTextContent("Wavelength", "Sets the size in meters on the local X axis of a single shore wave.");
        static public readonly GUIContent k_WaveRepetionText = EditorGUIUtility.TrTextContent("Skipped Waves", "Sets the proportion of skipped shore waves.");
        static public readonly GUIContent k_WaveSpeedText = EditorGUIUtility.TrTextContent("Speed", "Sets the translation speed of the shore waves in kilometers per hour along the local X axis.");
        static public readonly GUIContent k_WaveOffsetText = EditorGUIUtility.TrTextContent("Offset", "Sets the local translation offset of the shore waves along the wave direction.");
        static public readonly GUIContent k_BreakingRangeText = EditorGUIUtility.TrTextContent("Breaking Range", "Controls the range on the X axis where the shore wave should break. The wave reaches its maximum amplitude at the start of the range, generates surface foam inside it and looses 70% of its amplitude at the end of the range.");
        static public readonly GUIContent k_DeepFoamRangeText = EditorGUIUtility.TrTextContent("Deep Foam Range", "Controls the range on the X axis where the shore wave generates deep foam.");

        // Bow wave
        static public readonly GUIContent k_BowWaveElevationText = EditorGUIUtility.TrTextContent("Bow Wave Elevation", "Controls the height, in meters, of the bow wave.");

        // Box
        static public readonly GUIContent k_BoxBlendDistanceText = EditorGUIUtility.TrTextContent("Box Blend Distance", "Controls the range in meters, where HDRP blends between the water neutral height and the deformer amplitude.");
        static public readonly GUIContent k_CubicBlendText = EditorGUIUtility.TrTextContent("Cubic Blend", "When enabled, the blend between the water surface neutral height and the deformer's amplitude is done using a cubic profile. When disabled, the blend is linear.");

        // Custom parameters
        static public readonly GUIContent k_RangeText = EditorGUIUtility.TrTextContent("Range Remap", "Specifies the range of the deformer in the [-1, 1] interval. The input texture values will be remapped from [0,1] to the specifed range. To avoid seams with a black texture, the range needs to be set to [0,1].");
        static public readonly GUIContent k_TextureText = EditorGUIUtility.TrTextContent("Texture", "Specifies the texture used for the deformer.");

        // Foam parameters
        static public readonly GUIContent k_DeepFoamDimmerText = EditorGUIUtility.TrTextContent("Deep Foam Dimmer", "Controls the dimmer for the deep foam generated by the deformer.");
        static public readonly GUIContent k_SurfaceFoamDimmerText = EditorGUIUtility.TrTextContent("Surface Foam Dimmer", "Controls the dimmer for the surface foam generated by the deformer.");

        void ValidateSize(SerializedProperty property, float minValue)
        {
            Vector2 vec = property.vector2Value;
            vec.x = Mathf.Max(vec.x, minValue);
            vec.y = Mathf.Max(vec.y, minValue);
            property.vector2Value = vec;
        }

        void ShoreWaveUI()
        {
            // Wave length
            EditorGUILayout.PropertyField(m_WaveLength, k_WaveLengthText);
            m_WaveLength.floatValue = Mathf.Max(m_WaveLength.floatValue, 1.0f);

            // Amplitude of the wave
            EditorGUILayout.PropertyField(m_Amplitude, k_AmplitudeText);

            // Wave repetition
            EditorGUILayout.PropertyField(m_WaveRepetition, k_WaveRepetionText);
            m_WaveRepetition.intValue = Math.Max(1, m_WaveRepetition.intValue);

            // Wave speed
            EditorGUILayout.PropertyField(m_WaveSpeed, k_WaveSpeedText);

            // Wave speed
            EditorGUILayout.PropertyField(m_WaveOffset, k_WaveOffsetText);

            // Blend region
            Vector2 blendSize = m_WaveBlend.vector2Value;
            EditorGUILayout.MinMaxSlider(k_WaveBlendSizeText, ref blendSize.x, ref blendSize.y, 0.0f, 1.0f);
            m_WaveBlend.vector2Value = blendSize;

            // Wave peak location
            Vector2 breakingRange = m_BreakingRange.vector2Value;
            EditorGUILayout.MinMaxSlider(k_BreakingRangeText, ref breakingRange.x, ref breakingRange.y, 0.0f, 1.0f);
            m_BreakingRange.vector2Value = breakingRange;

            // Deep foam range
            Vector2 deepFoamRange = m_DeepFoamRange.vector2Value;
            EditorGUILayout.MinMaxSlider(k_DeepFoamRangeText, ref deepFoamRange.x, ref deepFoamRange.y, 0.0f, 1.0f);
            m_DeepFoamRange.vector2Value = deepFoamRange;

            // Surface foam dimmer
            m_SurfaceFoamDimmer.floatValue = EditorGUILayout.Slider(k_SurfaceFoamDimmerText, m_SurfaceFoamDimmer.floatValue, 0.0f, 1.0f);

            // Deep foam dimmer
            m_DeepFoamDimmer.floatValue = EditorGUILayout.Slider(k_DeepFoamDimmerText, m_DeepFoamDimmer.floatValue, 0.0f, 1.0f);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            // Region Size
            EditorGUILayout.PropertyField(m_RegionSize, k_RegionSizeText);
            ValidateSize(m_RegionSize, 1.0f);

            // Surface type
            EditorGUILayout.PropertyField(m_Type, k_TypeText);

            WaterDeformerType type = (WaterDeformerType)m_Type.enumValueIndex;
            using (new IndentLevelScope())
            {
                switch (type)
                {
                    case WaterDeformerType.Sphere:
                    {
                        // Amplitude of the sphere
                        EditorGUILayout.PropertyField(m_Amplitude, k_AmplitudeText);
                    }
                    break;
                    case WaterDeformerType.Box:
                    {
                        // Amplitude of the box
                        EditorGUILayout.PropertyField(m_Amplitude, k_AmplitudeText);

                        // Blend region size
                        EditorGUILayout.PropertyField(m_BoxBlend, k_BoxBlendDistanceText);
                        ValidateSize(m_BoxBlend, 0.0f);

                        // Blend profile
                        EditorGUILayout.PropertyField(m_CubicBlend, k_CubicBlendText);
                    }
                    break;
                    case WaterDeformerType.BowWave:
                    {
                        // Amplitude of the wave
                        EditorGUILayout.PropertyField(m_Amplitude, k_AmplitudeText);

                        // Amplitude of the attack
                        EditorGUILayout.PropertyField(m_BowWaveElevation, k_BowWaveElevationText);
                    }
                    break;
                    case WaterDeformerType.ShoreWave:
                    {
                        ShoreWaveUI();
                    }
                    break;
                    case WaterDeformerType.Texture:
                    {
                        EditorGUILayout.PropertyField(m_Amplitude, k_AmplitudeText);

                        // Range
                        Vector2 range = m_Range.vector2Value;
                        EditorGUILayout.MinMaxSlider(k_RangeText, ref range.x, ref range.y, -1.0f, 1.0f);
                        m_Range.vector2Value = range;

                        EditorGUILayout.PropertyField(m_Texture, k_TextureText);
                    }
                    break;
                }
            }

            // Apply the properties
            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("CONTEXT/WaterDeformer/Reset", false, 0)]
        static void ResetWaterDeformer(MenuCommand menuCommand)
        {
            GameObject go = ((WaterDeformer)menuCommand.context).gameObject;
            Assert.IsNotNull(go);

            WaterDeformer deformer = go.GetComponent<WaterDeformer>();
            switch (deformer.type)
            {
                case WaterDeformerType.Sphere:
                    WaterDeformerPresets.ApplyWaterSphereDeformerPreset(deformer);
                    break;
                case WaterDeformerType.Box:
                    WaterDeformerPresets.ApplyWaterBoxDeformerPreset(deformer);
                    break;
                case WaterDeformerType.ShoreWave:
                    WaterDeformerPresets.ApplyWaterShoreWaveDeformerPreset(deformer);
                    break;
                case WaterDeformerType.BowWave:
                    WaterDeformerPresets.ApplyWaterBowWaveDeformerPreset(deformer);
                    break;
                case WaterDeformerType.Texture:
                    WaterDeformerPresets.ApplyWaterTextureDeformerPreset(deformer);
                    break;
                default:
                    break;
            }
        }

        // Anis 11/09/21: Currently, there is a bug that makes the icon disappear after the first selection
        // if we do not have this. Given that the geometry is procedural, we need this to be able to
        // select the water surfaces.
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(WaterDeformer waterSurface, GizmoType gizmoType)
        {
        }
    }
}
