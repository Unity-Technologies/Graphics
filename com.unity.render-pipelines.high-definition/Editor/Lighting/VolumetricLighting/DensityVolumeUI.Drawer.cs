using UnityEditor.Rendering;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<SerializedDensityVolume>;

    static partial class DensityVolumeUI
    {
        [System.Flags]
        enum Expandable
        {
            Volume = 1 << 0,
            DensityMaskTexture = 1 << 1
        }

        readonly static ExpandedState<Expandable, DensityVolume> k_ExpandedState = new ExpandedState<Expandable, DensityVolume>(Expandable.Volume | Expandable.DensityMaskTexture, "HDRP");
        
        public static readonly CED.IDrawer Inspector = CED.Group(
            CED.Group(
                Drawer_ToolBar,
                Drawer_PrimarySettings
                ),
            CED.space,
            CED.FoldoutGroup(
                Styles.k_VolumeHeader,
                Expandable.Volume,
                k_ExpandedState,
                Drawer_AdvancedSwitch,
                Drawer_VolumeContent
                ),
            CED.FoldoutGroup(
                Styles.k_DensityMaskTextureHeader,
                Expandable.DensityMaskTexture,
                k_ExpandedState,
                Drawer_DensityMaskTextureContent
                )
            );
        
        static void Drawer_ToolBar(SerializedDensityVolume serialized, Editor owner)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditMode.DoInspectorToolbar(new[] { DensityVolumeEditor.k_EditShape, DensityVolumeEditor.k_EditBlend }, Styles.s_Toolbar_Contents, () =>
                {
                    var bounds = new Bounds();
                    foreach (Component targetObject in owner.targets)
                    {
                        bounds.Encapsulate(targetObject.transform.position);
                    }
                    return bounds;
                },
                owner);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        static void Drawer_PrimarySettings(SerializedDensityVolume serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.albedo, Styles.s_AlbedoLabel);
            EditorGUILayout.PropertyField(serialized.meanFreePath, Styles.s_MeanFreePathLabel);
        }

        static void Drawer_AdvancedSwitch(SerializedDensityVolume serialized, Editor owner)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                bool advanced = serialized.advancedFade.boolValue;
                advanced = GUILayout.Toggle(advanced, Styles.s_AdvancedModeContent, EditorStyles.miniButton, GUILayout.Width(60f), GUILayout.ExpandWidth(false));
                foreach (var containedBox in DensityVolumeEditor.blendBoxes.Values)
                {
                    containedBox.monoHandle = !advanced;
                }
                if (serialized.advancedFade.boolValue ^ advanced)
                {
                    serialized.advancedFade.boolValue = advanced;
                }
            }
        }

        static void Drawer_VolumeContent(SerializedDensityVolume serialized, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serialized.size, Styles.s_Size);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 tmpClamp = serialized.size.vector3Value;
                tmpClamp.x = Mathf.Max(0f, tmpClamp.x);
                tmpClamp.y = Mathf.Max(0f, tmpClamp.y);
                tmpClamp.z = Mathf.Max(0f, tmpClamp.z);
                serialized.size.vector3Value = tmpClamp;
            }

            Vector3 s = serialized.size.vector3Value;
            EditorGUI.BeginChangeCheck();
            if (serialized.advancedFade.boolValue)
            {
                Vector3 positive = serialized.positiveFade.vector3Value;
                positive.x *= s.x;
                positive.y *= s.y;
                positive.z *= s.z;
                Vector3 negative = serialized.negativeFade.vector3Value;
                negative.x *= s.x;
                negative.y *= s.y;
                negative.z *= s.z;
                EditorGUI.BeginChangeCheck();
                CoreEditorUtils.DrawVector6(Styles.s_BlendLabel, ref positive, ref negative, Vector3.zero, s, InfluenceVolumeUI.k_HandlesColor);
                if (EditorGUI.EndChangeCheck())
                {
                    positive.x /= s.x;
                    positive.y /= s.y;
                    positive.z /= s.z;
                    negative.x /= s.x;
                    negative.y /= s.y;
                    negative.z /= s.z;

                    //forbid positive/negative box that doesn't intersect in inspector too
                    for (int axis = 0; axis < 3; ++axis)
                    {
                        if (positive[axis] > 1f - negative[axis])
                        {
                            if (positive == serialized.positiveFade.vector3Value)
                            {
                                negative[axis] = 1f - positive[axis];
                            }
                            else
                            {
                                positive[axis] = 1f - negative[axis];
                            }
                        }
                    }

                    serialized.positiveFade.vector3Value = positive;
                    serialized.negativeFade.vector3Value = negative;
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                float distanceMax = Mathf.Min(s.x, s.y, s.z);
                float uniformFadeDistance = serialized.uniformFade.floatValue * distanceMax;
                uniformFadeDistance = EditorGUILayout.FloatField(Styles.s_BlendLabel, uniformFadeDistance);
                if (EditorGUI.EndChangeCheck())
                {
                    serialized.uniformFade.floatValue = Mathf.Clamp(uniformFadeDistance / distanceMax, 0f, 0.5f);
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 posFade = new Vector3();
                posFade.x = Mathf.Clamp01(serialized.positiveFade.vector3Value.x);
                posFade.y = Mathf.Clamp01(serialized.positiveFade.vector3Value.y);
                posFade.z = Mathf.Clamp01(serialized.positiveFade.vector3Value.z);

                Vector3 negFade = new Vector3();
                negFade.x = Mathf.Clamp01(serialized.negativeFade.vector3Value.x);
                negFade.y = Mathf.Clamp01(serialized.negativeFade.vector3Value.y);
                negFade.z = Mathf.Clamp01(serialized.negativeFade.vector3Value.z);

                serialized.positiveFade.vector3Value = posFade;
                serialized.negativeFade.vector3Value = negFade;
            }

            EditorGUILayout.PropertyField(serialized.invertFade, Styles.s_InvertFadeLabel);

            // Distance fade.
            {
                EditorGUI.BeginChangeCheck();

                float distanceFadeStart = EditorGUILayout.FloatField(Styles.s_DistanceFadeStartLabel, serialized.distanceFadeStart.floatValue);
                float distanceFadeEnd   = EditorGUILayout.FloatField(Styles.s_DistanceFadeEndLabel,   serialized.distanceFadeEnd.floatValue);

                if (EditorGUI.EndChangeCheck())
                {
                    distanceFadeStart = Mathf.Max(0, distanceFadeStart);
                    distanceFadeEnd   = Mathf.Max(distanceFadeStart, distanceFadeEnd);

                    serialized.distanceFadeStart.floatValue = distanceFadeStart;
                    serialized.distanceFadeEnd.floatValue   = distanceFadeEnd;
                }
            }
        }

        static void Drawer_DensityMaskTextureContent(SerializedDensityVolume serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.volumeTexture, Styles.s_VolumeTextureLabel);
            EditorGUILayout.PropertyField(serialized.textureScroll, Styles.s_TextureScrollLabel);
            EditorGUILayout.PropertyField(serialized.textureTile, Styles.s_TextureTileLabel);
        }
    }
}
