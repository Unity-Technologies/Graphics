using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.EditorGUI;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TextureWaterDeformer))]
    sealed partial class TextureWaterDeformerEditor : Editor
    {
        SerializedProperty m_RegionSize;
        SerializedProperty m_Amplitude;
        SerializedProperty m_Range;
        SerializedProperty m_Texture;

        void OnEnable()
        {
            var o = new PropertyFetcher<TextureWaterDeformer>(serializedObject);

            m_RegionSize = o.Find(x => x.regionSize);
            m_Amplitude = o.Find(x => x.amplitude);
            m_Range = o.Find(x => x.range);
            m_Texture = o.Find(x => x.texture);
        }

        static public readonly GUIContent k_RegionSizeText = EditorGUIUtility.TrTextContent("Region Size", "Specified the region size of the deformer.");
        static public readonly GUIContent k_AmplitudeText = EditorGUIUtility.TrTextContent("Amplitude", "Specified the amplitude of the deformer.");
        static public readonly GUIContent k_RangeText = EditorGUIUtility.TrTextContent("Range", "Specified the range of the deformer.");
        static public readonly GUIContent k_TextureText = EditorGUIUtility.TrTextContent("Texture", "Specified the texture used for the deformer.");

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

            EditorGUILayout.PropertyField(m_Amplitude, k_AmplitudeText);

            // Range
            Vector2 range = m_Range.vector2Value;
            EditorGUILayout.MinMaxSlider(k_RangeText, ref range.x, ref range.y, -1.0f, 1.0f);
            m_Range.vector2Value = range;

            EditorGUILayout.PropertyField(m_Texture, k_TextureText);

            // Apply the properties
            serializedObject.ApplyModifiedProperties();
        }

        // Anis 11/09/21: Currently, there is a bug that makes the icon disappear after the first selection
        // if we do not have this. Given that the geometry is procedural, we need this to be able to
        // select the water surfaces.
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(TextureWaterDeformer waterSurface, GizmoType gizmoType)
        {
        }
    }
}
