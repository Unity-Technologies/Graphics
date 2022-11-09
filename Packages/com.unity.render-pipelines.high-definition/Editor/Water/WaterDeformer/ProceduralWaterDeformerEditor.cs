using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.EditorGUI;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(ProceduralWaterDeformer))]
    sealed partial class ProceduralWaterDeformerEditor : Editor
    {
        // General parameters
        SerializedProperty m_Type;
        SerializedProperty m_Amplitude;
        SerializedProperty m_RegionSize;
        SerializedProperty m_CubicBlend;

        // Waves parameters
        SerializedProperty m_WaveLength;
        SerializedProperty m_WaveRepetition;
        SerializedProperty m_WaveSpeed;
        SerializedProperty m_WaveOffset;
        SerializedProperty m_WavePeakLocation;
        SerializedProperty m_WaveBlend;

        // Bow wave parameters
        SerializedProperty m_BowWaveElevation;

        // Box parameters
        SerializedProperty m_BoxBlend;

        void OnEnable()
        {
            var o = new PropertyFetcher<ProceduralWaterDeformer>(serializedObject);

            // General parameters
            m_Type = o.Find(x => x.type);
            m_Amplitude = o.Find(x => x.amplitude);

            // Waves parameters
            m_WaveLength = o.Find(x => x.waveLength);
            m_WaveRepetition = o.Find(x => x.waveRepetition);
            m_WaveSpeed = o.Find(x => x.waveSpeed);
            m_WaveOffset = o.Find(x => x.waveOffset);
            m_WavePeakLocation = o.Find(x => x.peakLocation);
            m_WaveBlend = o.Find(x => x.waveBlend);

            // Bow wave parameters
            m_BowWaveElevation = o.Find(x => x.bowWaveElevation);

            // Box parameters
            m_BoxBlend = o.Find(x => x.boxBlend);

            // Shared
            m_RegionSize = o.Find(x => x.regionSize);
            m_CubicBlend = o.Find(x => x.cubicBlend);
        }

        // General parameters
        static public readonly GUIContent k_AmplitudeText = EditorGUIUtility.TrTextContent("Amplitude", "Specified the amplitude of the deformer.");
        static public readonly GUIContent k_TypeText = EditorGUIUtility.TrTextContent("Type", "Specified the type of the deformer.");
        static public readonly GUIContent k_RegionSizeText = EditorGUIUtility.TrTextContent("Region Size", "Specified the region size of the deformer.");

        // Waves parameters
        static public readonly GUIContent k_WaveRegionSizeText = EditorGUIUtility.TrTextContent("Region Size", "Specifies the region size of the sine waves deformer.");
        static public readonly GUIContent k_WaveBlendSizeText = EditorGUIUtility.TrTextContent("Blend Size", "Specifies the region size of the sine waves deformer.");
        static public readonly GUIContent k_WaveLengthText = EditorGUIUtility.TrTextContent("Wavelength", "Specifies the wavelength of the individual sine waves in meters.");
        static public readonly GUIContent k_WaveRepetionText = EditorGUIUtility.TrTextContent("Repetition", "Specifies the repetition frequency of the individual sine waves.");
        static public readonly GUIContent k_WaveSpeedText = EditorGUIUtility.TrTextContent("Speed", "Specifies the translation speed of the sine waves in kilometers per hour.");
        static public readonly GUIContent k_WaveOffsetText = EditorGUIUtility.TrTextContent("Offset", "Specifies the offset speed of the sine waves.");
        static public readonly GUIContent k_WavePeakLocationText = EditorGUIUtility.TrTextContent("Peak Location", "Specifies where the peak is located on the X axis of the deformer region.");

        // Custom parameters
        static public readonly GUIContent k_CustomTextureText = EditorGUIUtility.TrTextContent("Texture", "Specifies the custom texture used for the deformer.");

        void ValidateSize(SerializedProperty property, float minValue)
        {
            Vector2 vec = property.vector2Value;
            vec.x = Mathf.Max(vec.x, minValue);
            vec.y = Mathf.Max(vec.y, minValue);
            property.vector2Value = vec;
        }

        public override void OnInspectorGUI()
        {
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportWaterDeformation ?? false)
            {
                EditorGUILayout.Space();
                HDEditorUtils.QualitySettingsHelpBox("The current HDRP Asset does not support deformation for Water Surfaces.", MessageType.Error,
                    HDRenderPipelineUI.Expandable.Rendering, "m_RenderPipelineSettings.supportWaterDeformation");
                return;
            }

            serializedObject.Update();
            // Region Size
            EditorGUILayout.PropertyField(m_RegionSize, k_RegionSizeText);
            ValidateSize(m_RegionSize, 1.0f);

            // Surface type
            EditorGUILayout.PropertyField(m_Type, k_TypeText);

            ProceduralWaterDeformerType type = (ProceduralWaterDeformerType)m_Type.enumValueIndex;
            using (new IndentLevelScope())
            {
                switch (type)
                {
                    case ProceduralWaterDeformerType.Sphere:
                    {
                        // Amplitude of the sphere
                        EditorGUILayout.PropertyField(m_Amplitude, k_AmplitudeText);
                    }
                    break;
                    case ProceduralWaterDeformerType.Box:
                    {
                        // Amplitude of the box
                        EditorGUILayout.PropertyField(m_Amplitude, k_AmplitudeText);

                        // Blend region size
                        EditorGUILayout.PropertyField(m_BoxBlend);
                        ValidateSize(m_BoxBlend, 0.0f);

                        // Blend profile
                        EditorGUILayout.PropertyField(m_CubicBlend);
                    }
                    break;
                    case ProceduralWaterDeformerType.BowWave:
                    {
                        // Amplitude of the wave
                        EditorGUILayout.PropertyField(m_Amplitude, k_AmplitudeText);

                        // Amplitude of the attack
                        EditorGUILayout.PropertyField(m_BowWaveElevation);
                    }
                    break;
                    case ProceduralWaterDeformerType.SineWave:
                    {
                        // Blend region
                        Vector2 blendSize = m_WaveBlend.vector2Value;
                        EditorGUILayout.MinMaxSlider(k_WaveBlendSizeText, ref blendSize.x, ref blendSize.y, 0.0f, 1.0f);
                        m_WaveBlend.vector2Value = blendSize;

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

                        // Wave peak location
                        m_WavePeakLocation.floatValue = EditorGUILayout.Slider(k_WavePeakLocationText, m_WavePeakLocation.floatValue, 0.0f, 1.0f);
                    }
                    break;
                }
            }

            // Apply the properties
            serializedObject.ApplyModifiedProperties();
        }

        // Anis 11/09/21: Currently, there is a bug that makes the icon disappear after the first selection
        // if we do not have this. Given that the geometry is procedural, we need this to be able to
        // select the water surfaces.
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(ProceduralWaterDeformer waterSurface, GizmoType gizmoType)
        {
        }
    }
}
