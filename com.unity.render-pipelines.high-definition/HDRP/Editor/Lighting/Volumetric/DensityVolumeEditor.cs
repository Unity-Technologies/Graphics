using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DensityVolume))]
    class DensityVolumeEditor : Editor
    {
        static GUIContent s_AlbedoLabel          = new GUIContent("Single Scattering Albedo", "Hue and saturation control the color of the fog (the wavelength of in-scattered light). Value controls scattering (0 = max absorption & no scattering, 1 = no absorption & max scattering).");
        static GUIContent s_MeanFreePathLabel    = new GUIContent("Mean Free Path", "Controls the density, which determines how far you can seen through the fog. It's the distance in meters at which 50% of background light is lost in the fog (due to absorption and out-scattering).");
        static GUIContent s_VolumeTextureLabel   = new GUIContent("Density Mask Texture");
        static GUIContent s_TextureScrollLabel   = new GUIContent("Texture Scroll Speed");
        static GUIContent s_TextureTileLabel     = new GUIContent("Texture Tiling Amount");
        static GUIContent s_TextureSettingsTitle = new GUIContent("Volume Texture Settings");

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
            albedo.colorValue = EditorGUILayout.ColorField(s_AlbedoLabel, albedo.colorValue, true, false, false);
            EditorGUILayout.PropertyField(meanFreePath, s_MeanFreePathLabel);
            EditorGUILayout.Space();

            showTextureParams = EditorGUILayout.Foldout(showTextureParams, s_TextureSettingsTitle, true);
            if (showTextureParams)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(volumeTexture, s_VolumeTextureLabel);
                EditorGUILayout.PropertyField(textureScroll, s_TextureScrollLabel);
                EditorGUILayout.PropertyField(textureTile, s_TextureTileLabel);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
