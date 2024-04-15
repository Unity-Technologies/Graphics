using UnityEngine;

namespace UnityEditor.Rendering
{
    public static partial class CameraUI
    {
        /// <summary>
        /// Output Section
        /// </summary>
        public static partial class Output
        {
            /// <summary>Draws Allow Dynamic Resolution related fields on the inspector</summary>
            /// <param name="p"><see cref="ISerializedCamera"/> The serialized camera</param>
            /// <param name="owner"><see cref="Editor"/> The editor owner calling this drawer</param>
            /// <param name="customLabel">Override property name</param>
            public static void Drawer_Output_AllowDynamicResolution(ISerializedCamera p, Editor owner, GUIContent customLabel = null)
            {
                EditorGUILayout.PropertyField(p.allowDynamicResolution, customLabel ?? Styles.allowDynamicResolution);
                p.baseCameraSettings.allowDynamicResolution.boolValue = p.allowDynamicResolution.boolValue;
            }

            /// <summary>Draws Normalized ViewPort related fields on the inspector</summary>
            /// <param name="p"><see cref="ISerializedCamera"/> The serialized camera</param>
            /// <param name="owner"><see cref="Editor"/> The editor owner calling this drawer</param>
            public static void Drawer_Output_NormalizedViewPort(ISerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.baseCameraSettings.normalizedViewPortRect, Styles.viewport);
            }

            /// <summary>Draws Depth related fields on the inspector</summary>
            /// <param name="p"><see cref="ISerializedCamera"/> The serialized camera</param>
            /// <param name="owner"><see cref="Editor"/> The editor owner calling this drawer</param>
            public static void Drawer_Output_Depth(ISerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.baseCameraSettings.depth, Styles.depth);
            }

            /// <summary>Draws Render Target related fields on the inspector</summary>
            /// <param name="p"><see cref="ISerializedCamera"/> The serialized camera</param>
            /// <param name="owner"><see cref="Editor"/> The editor owner calling this drawer</param>
            public static void Drawer_Output_RenderTarget(ISerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.baseCameraSettings.targetTexture);
            }
        }
    }
}
