using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.EditorGUI;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(WaterDeformer))]
    sealed partial class WaterDeformerEditor : Editor
    {
        static readonly Color k_HandleColor = new Color(0 / 255f, 0xE5 / 255f, 0xFF / 255f, 1f).gamma;

        // General parameters
        SerializedProperty m_Type;
        SerializedProperty m_Amplitude;
        SerializedProperty m_RegionSize;
        SerializedProperty m_ScaleMode;

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

        HierarchicalBox m_BoxHandle;

        void OnEnable()
        {
            var o = new PropertyFetcher<WaterDeformer>(serializedObject);

            // General parameters
            m_Type = o.Find(x => x.type);
            m_Amplitude = o.Find(x => x.amplitude);
            m_RegionSize = o.Find(x => x.regionSize);
            m_ScaleMode = o.Find(x => x.scaleMode);

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

            m_BoxHandle = new HierarchicalBox(k_HandleColor, new[] { k_HandleColor, k_HandleColor, k_HandleColor, k_HandleColor, k_HandleColor, k_HandleColor })
            {
                monoHandle = false,
                allowNegativeSize = true,
            };
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

        void ShoreWaveUI()
        {
            // Wave length
            EditorGUILayout.PropertyField(m_WaveLength, k_WaveLengthText);

            // Amplitude of the wave
            EditorGUILayout.PropertyField(m_Amplitude, k_AmplitudeText);

            // Wave repetition
            EditorGUILayout.PropertyField(m_WaveRepetition, k_WaveRepetionText);

            // Wave speed
            EditorGUILayout.PropertyField(m_WaveSpeed, k_WaveSpeedText);

            // Wave speed
            EditorGUILayout.PropertyField(m_WaveOffset, k_WaveOffsetText);

            // Blend region
            BeginChangeCheck();
            Vector2 blendSize = m_WaveBlend.vector2Value;
            EditorGUILayout.MinMaxSlider(k_WaveBlendSizeText, ref blendSize.x, ref blendSize.y, 0.0f, 1.0f);
            if (EndChangeCheck())
                m_WaveBlend.vector2Value = blendSize;

            // Wave peak location
            BeginChangeCheck();
            Vector2 breakingRange = m_BreakingRange.vector2Value;
            EditorGUILayout.MinMaxSlider(k_BreakingRangeText, ref breakingRange.x, ref breakingRange.y, 0.0f, 1.0f);
            if (EndChangeCheck())
                m_BreakingRange.vector2Value = breakingRange;

            // Deep foam range
            BeginChangeCheck();
            Vector2 deepFoamRange = m_DeepFoamRange.vector2Value;
            EditorGUILayout.MinMaxSlider(k_DeepFoamRangeText, ref deepFoamRange.x, ref deepFoamRange.y, 0.0f, 1.0f);
            if (EndChangeCheck())
                m_DeepFoamRange.vector2Value = deepFoamRange;

            // Surface foam dimmer
            EditorGUILayout.PropertyField(m_SurfaceFoamDimmer, k_SurfaceFoamDimmerText);

            // Deep foam dimmer
            EditorGUILayout.PropertyField(m_DeepFoamDimmer, k_DeepFoamDimmerText);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_ScaleMode);

            // Region Size
            EditorGUILayout.PropertyField(m_RegionSize, k_RegionSizeText);

            // Surface type
            EditorGUILayout.PropertyField(m_Type, k_TypeText);
            if (!m_Type.hasMultipleDifferentValues)
                SurfaceTypeGUI();

            // Apply the properties
            serializedObject.ApplyModifiedProperties();
        }

        public void SurfaceTypeGUI()
        {
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
                        BeginChangeCheck();
                        Vector2 range = m_Range.vector2Value;
                        EditorGUILayout.MinMaxSlider(k_RangeText, ref range.x, ref range.y, -1.0f, 1.0f);
                        if (EndChangeCheck())
                            m_Range.vector2Value = range;

                        EditorGUILayout.PropertyField(m_Texture, k_TextureText);
                    }
                    break;
                }
            }
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

        void OnSceneGUI()
        {
            WaterDeformer deformer = target as WaterDeformer;
            var tr = deformer.transform;
            var rotation = Quaternion.Euler(0, tr.eulerAngles.y, 0);
            var regionSize = new Vector3(deformer.regionSize.x, deformer.amplitude * 2, deformer.regionSize.y);

            using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one)))
            {
                Vector3 scale = deformer.scaleMode == DecalScaleMode.InheritFromHierarchy ? tr.lossyScale : Vector3.one;
                m_BoxHandle.center = Quaternion.Inverse(rotation) * tr.position;
                m_BoxHandle.size = Vector3.Scale(regionSize, scale);
                EditorGUI.BeginChangeCheck();
                m_BoxHandle.DrawHull(true);
                m_BoxHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(new UnityEngine.Object[] { tr, deformer }, "Update Deformer Region");
                    tr.position = rotation * m_BoxHandle.center;
                    deformer.regionSize = Vector2.Max(new Vector2(m_BoxHandle.size.x / scale.x, m_BoxHandle.size.z / scale.z), Vector2.one);
                    deformer.amplitude = m_BoxHandle.size.y * 0.5f;
                }
            }
        }
    }
}
