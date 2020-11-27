using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedDensityVolume>;

    static partial class DensityVolumeUI
    {
        // also used for AdvancedModes
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
            CED.FoldoutGroup(Styles.k_VolumeHeader, Expandable.Volume, k_ExpandedState,
                Drawer_VolumeContent
                ),
            CED.FoldoutGroup(
                Styles.k_DensityMaskTextureHeader, Expandable.DensityMaskTexture, k_ExpandedState,
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

        static void Drawer_VolumeContent(SerializedDensityVolume serialized, Editor owner)
        {
            //keep previous data as value are stored in percent
            Vector3 previousSize = serialized.size.vector3Value;
            float previousUniformFade = serialized.editorUniformFade.floatValue;
            Vector3 previousPositiveFade = serialized.editorPositiveFade.vector3Value;
            Vector3 previousNegativeFade = serialized.editorNegativeFade.vector3Value;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serialized.size, Styles.s_Size);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 newSize = serialized.size.vector3Value;
                newSize.x = Mathf.Max(0f, newSize.x);
                newSize.y = Mathf.Max(0f, newSize.y);
                newSize.z = Mathf.Max(0f, newSize.z);
                serialized.size.vector3Value = newSize;

                //update advanced mode blend
                Vector3 newPositiveFade = new Vector3(
                    newSize.x < 0.00001 ? 0 : previousPositiveFade.x * previousSize.x / newSize.x,
                    newSize.y < 0.00001 ? 0 : previousPositiveFade.y * previousSize.y / newSize.y,
                    newSize.z < 0.00001 ? 0 : previousPositiveFade.z * previousSize.z / newSize.z
                );
                Vector3 newNegativeFade = new Vector3(
                    newSize.x < 0.00001 ? 0 : previousNegativeFade.x * previousSize.x / newSize.x,
                    newSize.y < 0.00001 ? 0 : previousNegativeFade.y * previousSize.y / newSize.y,
                    newSize.z < 0.00001 ? 0 : previousNegativeFade.z * previousSize.z / newSize.z
                );
                for (int axeIndex = 0; axeIndex < 3; ++axeIndex)
                {
                    if (newPositiveFade[axeIndex] + newNegativeFade[axeIndex] > 1)
                    {
                        float overValue = (newPositiveFade[axeIndex] + newNegativeFade[axeIndex] - 1f) * 0.5f;
                        newPositiveFade[axeIndex] -= overValue;
                        newNegativeFade[axeIndex] -= overValue;

                        if (newPositiveFade[axeIndex] < 0)
                        {
                            newNegativeFade[axeIndex] += newPositiveFade[axeIndex];
                            newPositiveFade[axeIndex] = 0f;
                        }
                        if (newNegativeFade[axeIndex] < 0)
                        {
                            newPositiveFade[axeIndex] += newNegativeFade[axeIndex];
                            newNegativeFade[axeIndex] = 0f;
                        }
                    }
                }
                serialized.editorPositiveFade.vector3Value = newPositiveFade;
                serialized.editorNegativeFade.vector3Value = newNegativeFade;

                //update normal mode blend
                float max = Mathf.Min(newSize.x, newSize.y, newSize.z) * 0.5f;
                serialized.editorUniformFade.floatValue = Mathf.Clamp(serialized.editorUniformFade.floatValue, 0f, max);
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serialized.editorAdvancedFade, Styles.s_ManipulatonTypeContent);

            Vector3 serializedSize = serialized.size.vector3Value;
            EditorGUI.BeginChangeCheck();
            if (serialized.editorAdvancedFade.hasMultipleDifferentValues)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.LabelField(Styles.s_BlendLabel, EditorGUIUtility.TrTextContent("Multiple values for Advanced mode"));
            }
            else if (serialized.editorAdvancedFade.boolValue)
            {
                CoreEditorUtils.DrawVector6(Styles.s_BlendLabel, serialized.editorPositiveFade, serialized.editorNegativeFade, Vector3.zero, serializedSize, InfluenceVolumeUI.k_HandlesColor, serialized.size, false);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serialized.editorUniformFade, Styles.s_BlendLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    float max = Mathf.Min(serializedSize.x, serializedSize.y, serializedSize.z) * 0.5f;
                    serialized.editorUniformFade.floatValue = Mathf.Clamp(serialized.editorUniformFade.floatValue, 0f, max);
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 posFade = new Vector3();
                posFade.x = Mathf.Clamp01(serialized.editorPositiveFade.vector3Value.x);
                posFade.y = Mathf.Clamp01(serialized.editorPositiveFade.vector3Value.y);
                posFade.z = Mathf.Clamp01(serialized.editorPositiveFade.vector3Value.z);

                Vector3 negFade = new Vector3();
                negFade.x = Mathf.Clamp01(serialized.editorNegativeFade.vector3Value.x);
                negFade.y = Mathf.Clamp01(serialized.editorNegativeFade.vector3Value.y);
                negFade.z = Mathf.Clamp01(serialized.editorNegativeFade.vector3Value.z);

                serialized.editorPositiveFade.vector3Value = posFade;
                serialized.editorNegativeFade.vector3Value = negFade;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serialized.invertFade, Styles.s_InvertFadeLabel);

            // Distance fade.
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serialized.distanceFadeStart, Styles.s_DistanceFadeStartLabel);
                EditorGUILayout.PropertyField(serialized.distanceFadeEnd, Styles.s_DistanceFadeEndLabel);

                if (EditorGUI.EndChangeCheck())
                {
                    float distanceFadeStart = Mathf.Max(0, serialized.distanceFadeStart.floatValue);
                    float distanceFadeEnd   = Mathf.Max(distanceFadeStart, serialized.distanceFadeEnd.floatValue);

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
