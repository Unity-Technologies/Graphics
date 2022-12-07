using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditorInternal;
using UnityEditor.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LocalVolumetricFog))]
    class LocalVolumetricFogEditor : Editor
    {
        internal const EditMode.SceneViewEditMode k_EditShape = EditMode.SceneViewEditMode.ReflectionProbeBox;
        internal const EditMode.SceneViewEditMode k_EditBlend = EditMode.SceneViewEditMode.GridBox;
        Editor m_MaterialEditor;

        static HierarchicalBox s_ShapeBox;
        internal static HierarchicalBox s_BlendBox;

        SerializedLocalVolumetricFog m_SerializedLocalVolumetricFog;

        void OnEnable()
        {
            m_SerializedLocalVolumetricFog = new SerializedLocalVolumetricFog(serializedObject);

            if (s_ShapeBox == null || s_ShapeBox.Equals(null))
            {
                s_ShapeBox = new HierarchicalBox(LocalVolumetricFogUI.Styles.k_GizmoColorBase, LocalVolumetricFogUI.Styles.k_BaseHandlesColor);
                s_ShapeBox.monoHandle = false;
            }
            if (s_BlendBox == null || s_BlendBox.Equals(null))
            {
                s_BlendBox = new HierarchicalBox(LocalVolumetricFogUI.Styles.k_GizmoColorBase, InfluenceVolumeUI.k_HandlesColor, parent: s_ShapeBox);
            }
        }

        void OnDisable()
        {
            CoreUtils.Destroy(m_MaterialEditor);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (!HDRenderPipeline.currentAsset?.currentPlatformRenderPipelineSettings.supportVolumetrics ?? false)
            {
                HDEditorUtils.QualitySettingsHelpBox("The current HDRP Asset does not support volumetric fog.", MessageType.Error,
                    HDRenderPipelineUI.Expandable.Lighting, "m_RenderPipelineSettings.supportVolumetrics");
            }

            LocalVolumetricFogUI.Inspector.Draw(m_SerializedLocalVolumetricFog, this);

            m_SerializedLocalVolumetricFog.Apply();

            if ((LocalVolumetricFogMaskMode)m_SerializedLocalVolumetricFog.maskMode.intValue == LocalVolumetricFogMaskMode.Material
                && m_SerializedLocalVolumetricFog.materialMask.objectReferenceValue is Material mat)
            {
                // Update material target
                if (m_MaterialEditor == null || m_MaterialEditor.target != mat)
                    Editor.CreateCachedEditor(mat, typeof(MaterialEditor), ref m_MaterialEditor);

                // Draw material UI
                using (new EditorGUI.DisabledScope((mat.hideFlags & HideFlags.NotEditable) != 0))
                {
                    m_MaterialEditor.DrawHeader();
                    m_MaterialEditor.OnInspectorGUI();
                }
            }
        }

        static Vector3 CenterBlendLocalPosition(LocalVolumetricFog localVolumetricFog)
        {
            if (localVolumetricFog.parameters.m_EditorAdvancedFade)
            {
                Vector3 size = localVolumetricFog.parameters.size;
                Vector3 posBlend = localVolumetricFog.parameters.m_EditorPositiveFade;
                posBlend.x *= size.x;
                posBlend.y *= size.y;
                posBlend.z *= size.z;
                Vector3 negBlend = localVolumetricFog.parameters.m_EditorNegativeFade;
                negBlend.x *= size.x;
                negBlend.y *= size.y;
                negBlend.z *= size.z;
                Vector3 localPosition = (negBlend - posBlend) * 0.5f;
                return localPosition;
            }
            else
                return Vector3.zero;
        }

        static Vector3 BlendSize(LocalVolumetricFog localVolumetricFog)
        {
            Vector3 size = localVolumetricFog.parameters.size;
            if (localVolumetricFog.parameters.m_EditorAdvancedFade)
            {
                Vector3 blendSize = (Vector3.one - localVolumetricFog.parameters.m_EditorPositiveFade - localVolumetricFog.parameters.m_EditorNegativeFade);
                blendSize.x *= size.x;
                blendSize.y *= size.y;
                blendSize.z *= size.z;
                return blendSize;
            }
            else
                return size - localVolumetricFog.parameters.m_EditorUniformFade * 2f * Vector3.one;
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(LocalVolumetricFog localVolumetricFog, GizmoType gizmoType)
        {
            if (s_BlendBox == null || s_BlendBox.Equals(null)
                || s_ShapeBox == null || s_ShapeBox.Equals(null))
                return;

            using (new Handles.DrawingScope(Matrix4x4.TRS(localVolumetricFog.transform.position, localVolumetricFog.transform.rotation, Vector3.one)))
            {
                // Blend box
                s_BlendBox.center = CenterBlendLocalPosition(localVolumetricFog);
                s_BlendBox.size = BlendSize(localVolumetricFog);
                Color baseColor = localVolumetricFog.parameters.albedo;
                baseColor.a = 8 / 255f;
                s_BlendBox.baseColor = baseColor;
                s_BlendBox.DrawHull(EditMode.editMode == k_EditBlend);

                // Bounding box.
                s_ShapeBox.center = Vector3.zero;
                s_ShapeBox.size = localVolumetricFog.parameters.size;
                s_ShapeBox.DrawHull(EditMode.editMode == k_EditShape);
            }
        }

        void OnSceneGUI()
        {
            //Note: for each handle to be independent when multi-selecting LocalVolumetricFog,
            //We cannot rely  hereon SerializedLocalVolumetricFog which is the collection of
            //selected LocalVolumetricFog. Thus code is almost the same of the UI.

            LocalVolumetricFog localVolumetricFog = target as LocalVolumetricFog;

            switch (EditMode.editMode)
            {
                case k_EditBlend:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(localVolumetricFog.transform.position, localVolumetricFog.transform.rotation, Vector3.one)))
                    {
                        //contained must be initialized in all case
                        s_ShapeBox.center = Vector3.zero;
                        s_ShapeBox.size = localVolumetricFog.parameters.size;

                        Color baseColor = localVolumetricFog.parameters.albedo;
                        baseColor.a = 8 / 255f;
                        s_BlendBox.baseColor = baseColor;
                        s_BlendBox.monoHandle = !localVolumetricFog.parameters.m_EditorAdvancedFade;
                        s_BlendBox.center = CenterBlendLocalPosition(localVolumetricFog);
                        s_BlendBox.size = BlendSize(localVolumetricFog);
                        EditorGUI.BeginChangeCheck();
                        s_BlendBox.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(localVolumetricFog, L10n.Tr("Change Local Volumetric Fog Blend"));

                            if (localVolumetricFog.parameters.m_EditorAdvancedFade)
                            {
                                //work in local space to compute the change on positiveFade and negativeFade
                                Vector3 newCenterBlendLocalPosition = s_BlendBox.center;
                                Vector3 halfSize = s_BlendBox.size * 0.5f;
                                Vector3 size = localVolumetricFog.parameters.size;
                                Vector3 posFade = newCenterBlendLocalPosition + halfSize;
                                posFade.x = 0.5f - posFade.x / size.x;
                                posFade.y = 0.5f - posFade.y / size.y;
                                posFade.z = 0.5f - posFade.z / size.z;
                                Vector3 negFade = newCenterBlendLocalPosition - halfSize;
                                negFade.x = 0.5f + negFade.x / size.x;
                                negFade.y = 0.5f + negFade.y / size.y;
                                negFade.z = 0.5f + negFade.z / size.z;
                                localVolumetricFog.parameters.m_EditorPositiveFade = posFade;
                                localVolumetricFog.parameters.m_EditorNegativeFade = negFade;
                            }
                            else
                            {
                                float uniformDistance = (s_ShapeBox.size.x - s_BlendBox.size.x) * 0.5f;
                                float max = Mathf.Min(s_ShapeBox.size.x, s_ShapeBox.size.y, s_ShapeBox.size.z) * 0.5f;
                                localVolumetricFog.parameters.m_EditorUniformFade = Mathf.Clamp(uniformDistance, 0f, max);
                            }
                        }
                    }
                    break;
                case k_EditShape:
                    //important: if the origin of the handle's space move along the handle,
                    //handles displacement will appears as moving two time faster.
                    using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, localVolumetricFog.transform.rotation, Vector3.one)))
                    {
                        //contained must be initialized in all case
                        s_ShapeBox.center = Quaternion.Inverse(localVolumetricFog.transform.rotation) * localVolumetricFog.transform.position;
                        s_ShapeBox.size = localVolumetricFog.parameters.size;

                        Vector3 previousSize = localVolumetricFog.parameters.size;
                        Vector3 previousPositiveFade = localVolumetricFog.parameters.m_EditorPositiveFade;
                        Vector3 previousNegativeFade = localVolumetricFog.parameters.m_EditorNegativeFade;

                        EditorGUI.BeginChangeCheck();
                        s_ShapeBox.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(new Object[] { localVolumetricFog, localVolumetricFog.transform }, L10n.Tr("Change Local Volumetric Fog Bounding Box"));

                            Vector3 newSize = s_ShapeBox.size;
                            localVolumetricFog.parameters.size = newSize;

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
                            localVolumetricFog.parameters.m_EditorPositiveFade = newPositiveFade;
                            localVolumetricFog.parameters.m_EditorNegativeFade = newNegativeFade;

                            //update normal mode blend
                            float max = Mathf.Min(newSize.x, newSize.y, newSize.z) * 0.5f;
                            float newUniformFade = Mathf.Clamp(localVolumetricFog.parameters.m_EditorUniformFade, 0f, max);
                            localVolumetricFog.parameters.m_EditorUniformFade = newUniformFade;

                            //update engine used percents
                            if (localVolumetricFog.parameters.m_EditorAdvancedFade)
                            {
                                localVolumetricFog.parameters.positiveFade = newPositiveFade;
                                localVolumetricFog.parameters.negativeFade = newNegativeFade;
                            }
                            else
                            {
                                localVolumetricFog.parameters.positiveFade =
                                    localVolumetricFog.parameters.negativeFade = new Vector3(
                                        1.0f - (newSize.x > 0.00000001 ? (newSize.x - newUniformFade) / newSize.x : 0f),
                                        1.0f - (newSize.y > 0.00000001 ? (newSize.y - newUniformFade) / newSize.y : 0f),
                                        1.0f - (newSize.z > 0.00000001 ? (newSize.z - newUniformFade) / newSize.z : 0f));
                            }

                            Vector3 delta = localVolumetricFog.transform.rotation * s_ShapeBox.center - localVolumetricFog.transform.position;
                            localVolumetricFog.transform.position += delta;
                        }
                    }
                    break;
            }
        }
    }
}
