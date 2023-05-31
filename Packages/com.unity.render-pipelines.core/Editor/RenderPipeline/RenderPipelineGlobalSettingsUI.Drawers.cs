using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// UI for global settings
    /// </summary>
    public static partial class RenderPipelineGlobalSettingsUI
    {
        /// <summary>
        /// Draw Volume Profile property field with a foldout scope that ensures that the target cannot become null.
        /// </summary>
        /// <param name="volumeProfileSerializedProperty">Target serialized property</param>
        /// <param name="volumeProfileLabel">Label for the property field</param>
        /// <param name="getOrCreateVolumeProfile">Callback that creates and returns a valid Volume Profile</param>
        /// <param name="labelFoldoutExpanded">Reference parameter indicating whether the foldout is expanded</param>
        /// <returns>The volume profile</returns>
        public static VolumeProfile DrawVolumeProfileAssetField(
            SerializedProperty volumeProfileSerializedProperty,
            GUIContent volumeProfileLabel,
            Func<VolumeProfile> getOrCreateVolumeProfile,
            ref bool labelFoldoutExpanded)
        {
            VolumeProfile asset;
            using (new EditorGUILayout.HorizontalScope())
            {
                var oldAssetValue = volumeProfileSerializedProperty.objectReferenceValue;
                EditorGUILayout.PropertyField(volumeProfileSerializedProperty, volumeProfileLabel);
                asset = volumeProfileSerializedProperty.objectReferenceValue as VolumeProfile;
                if (asset == null)
                {
                    if (oldAssetValue != null)
                    {
                        Debug.Log("This Volume Profile Asset cannot be null. Rolling back to previous value.");
                        volumeProfileSerializedProperty.objectReferenceValue = oldAssetValue;
                        asset = oldAssetValue as VolumeProfile;
                    }
                    else
                    {
                        asset = getOrCreateVolumeProfile();
                    }
                }

                labelFoldoutExpanded = GUI.Toggle(
                    GUILayoutUtility.GetLastRect(), labelFoldoutExpanded, GUIContent.none, EditorStyles.foldout);
            }

            return asset;
        }
    }
}
