namespace UnityEditor.Rendering
{
    /// <summary> Camera UI Shared Properties among SRP</summary>
    public static partial class CameraUI
    {
        public static partial class Rendering
        {
            /// <summary>Draws Stop NaNs related fields on the inspector</summary>
            /// <param name="p"><see cref="ISerializedCamera"/> The serialized camera</param>
            /// <param name="owner"><see cref="Editor"/> The editor owner calling this drawer</param>
            public static void Drawer_Rendering_StopNaNs(ISerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.stopNaNs, Styles.stopNaNs);
            }

            /// <summary>Draws Dithering related fields on the inspector</summary>
            /// <param name="p"><see cref="ISerializedCamera"/> The serialized camera</param>
            /// <param name="owner"><see cref="Editor"/> The editor owner calling this drawer</param>
            public static void Drawer_Rendering_Dithering(ISerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.dithering, Styles.dithering);

                if (PlayerSettings.useHDRDisplay && p.dithering.boolValue)
                    EditorGUILayout.HelpBox(Styles.unsupportedDitheringWithHDROutputWarning, MessageType.Warning);
            }

            /// <summary>Draws Culling mask related fields on the inspector</summary>
            /// <param name="p"><see cref="ISerializedCamera"/> The serialized camera</param>
            /// <param name="owner"><see cref="Editor"/> The editor owner calling this drawer</param>
            public static void Drawer_Rendering_CullingMask(ISerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.baseCameraSettings.cullingMask, Styles.cullingMask);
            }

            /// <summary>Draws occlusion Culling related fields on the inspector</summary>
            /// <param name="p"><see cref="ISerializedCamera"/> The serialized camera</param>
            /// <param name="owner"><see cref="Editor"/> The editor owner calling this drawer</param>
            public static void Drawer_Rendering_OcclusionCulling(ISerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.baseCameraSettings.occlusionCulling, Styles.occlusionCulling);
            }
        }
    }
}
