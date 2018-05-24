using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DensityVolume))]
    class DensityVolumeEditor : Editor
    {
        private static GUIContent albedoLabel = new GUIContent("Scattering Color");
        private static GUIContent meanFreePathLabel = new GUIContent("Mean Free Path");
        private static GUIContent volumeTextureLabel = new GUIContent("Volume Texture Mask");
        private static GUIContent textureScrollLabel = new GUIContent("Texture Scroll Speed");
        private static GUIContent textureTileLabel = new GUIContent("Texture Tiling Amount");
        private static GUIContent textureSettingsTitle = new GUIContent("Volume Texture Settings");

        private bool showTextureParams = false;

        SerializedProperty densityParams;
        SerializedProperty albedo;
        SerializedProperty meanFreePath;

        SerializedProperty volumeTexture;
        SerializedProperty textureScroll;
        SerializedProperty textureTile;

        void OnEnable()
        {
            densityParams = serializedObject.FindProperty("parameters");
            albedo = densityParams.FindPropertyRelative("albedo");
            meanFreePath = densityParams.FindPropertyRelative("meanFreePath");

            volumeTexture = densityParams.FindPropertyRelative("volumeMask");
            textureScroll = densityParams.FindPropertyRelative("textureScrollingSpeed");
            textureTile = densityParams.FindPropertyRelative("textureTiling");

            if (volumeTexture != null && volumeTexture.objectReferenceValue != null)
            {
                showTextureParams = true;
            }
        }

        public override void OnInspectorGUI()
        {
            albedo.colorValue = EditorGUILayout.ColorField(albedoLabel, albedo.colorValue, true, false, false);
            EditorGUILayout.PropertyField(meanFreePath, meanFreePathLabel);
            EditorGUILayout.Space();

            showTextureParams = EditorGUILayout.Foldout(showTextureParams, textureSettingsTitle, true);
            if (showTextureParams)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(volumeTexture, volumeTextureLabel);
                EditorGUILayout.PropertyField(textureScroll, textureScrollLabel);
                EditorGUILayout.PropertyField(textureTile, textureTileLabel);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
