using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditorInternal;
using UnityEditor.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DensityVolume))]
    class DensityVolumeEditor : Editor
    {
        internal const EditMode.SceneViewEditMode k_EditShape = EditMode.SceneViewEditMode.ReflectionProbeBox;
        internal const EditMode.SceneViewEditMode k_EditBlend = EditMode.SceneViewEditMode.GridBox;

        static HierarchicalBox s_ShapeBox;
        internal static HierarchicalBox s_BlendBox;

        SerializedDensityVolume m_SerializedDensityVolume;

        void OnEnable()
        {
            m_SerializedDensityVolume = new SerializedDensityVolume(serializedObject);

            if (s_ShapeBox == null || s_ShapeBox.Equals(null))
            {
                s_ShapeBox = new HierarchicalBox(DensityVolumeUI.Styles.k_GizmoColorBase, DensityVolumeUI.Styles.k_BaseHandlesColor);
                s_ShapeBox.monoHandle = false;
            }
            if (s_BlendBox == null || s_BlendBox.Equals(null))
            {
                s_BlendBox = new HierarchicalBox(DensityVolumeUI.Styles.k_GizmoColorBase, InfluenceVolumeUI.k_HandlesColor, parent: s_ShapeBox);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DensityVolumeUI.Inspector.Draw(m_SerializedDensityVolume, this);

            m_SerializedDensityVolume.Apply();
        }

        static Vector3 CenterBlendLocalPosition(DensityVolume densityVolume)
        {
            if (densityVolume.parameters.m_EditorAdvancedFade)
            {
                Vector3 size = densityVolume.parameters.size;
                Vector3 posBlend = densityVolume.parameters.m_EditorPositiveFade;
                posBlend.x *= size.x;
                posBlend.y *= size.y;
                posBlend.z *= size.z;
                Vector3 negBlend = densityVolume.parameters.m_EditorNegativeFade;
                negBlend.x *= size.x;
                negBlend.y *= size.y;
                negBlend.z *= size.z;
                Vector3 localPosition = (negBlend - posBlend) * 0.5f;
                return localPosition;
            }
            else
                return Vector3.zero;
        }

        static Vector3 BlendSize(DensityVolume densityVolume)
        {
            Vector3 size = densityVolume.parameters.size;
            if (densityVolume.parameters.m_EditorAdvancedFade)
            {
                Vector3 blendSize = (Vector3.one - densityVolume.parameters.m_EditorPositiveFade - densityVolume.parameters.m_EditorNegativeFade);
                blendSize.x *= size.x;
                blendSize.y *= size.y;
                blendSize.z *= size.z;
                return blendSize;
            }
            else
                return size - densityVolume.parameters.m_EditorUniformFade * 2f * Vector3.one;
        }

        [DrawGizmo(GizmoType.Selected|GizmoType.Active)]
        static void DrawGizmosSelected(DensityVolume densityVolume, GizmoType gizmoType)
        {
            using (new Handles.DrawingScope(Matrix4x4.TRS(densityVolume.transform.position, densityVolume.transform.rotation, Vector3.one)))
            {
                // Blend box
                s_BlendBox.center = CenterBlendLocalPosition(densityVolume);
                s_BlendBox.size = BlendSize(densityVolume);
                Color baseColor = densityVolume.parameters.albedo;
                baseColor.a = 8/255f;
                s_BlendBox.baseColor = baseColor;
                s_BlendBox.DrawHull(EditMode.editMode == k_EditBlend);

                // Bounding box.
                s_ShapeBox.center = Vector3.zero;
                s_ShapeBox.size = densityVolume.parameters.size;
                s_ShapeBox.DrawHull(EditMode.editMode == k_EditShape);
            }
        }

        void OnSceneGUI()
        {
            //Note: for each handle to be independent when multi-selecting DensityVolume,
            //We cannot rely  hereon SerializedDensityVolume which is the collection of
            //selected DensityVolume. Thus code is almost the same of the UI.

            DensityVolume densityVolume = target as DensityVolume;

            switch (EditMode.editMode)
            {
                case k_EditBlend:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(densityVolume.transform.position, densityVolume.transform.rotation, Vector3.one)))
                    {
                        //contained must be initialized in all case
                        s_ShapeBox.center = Vector3.zero;
                        s_ShapeBox.size = densityVolume.parameters.size;

                        Color baseColor = densityVolume.parameters.albedo;
                        baseColor.a = 8 / 255f;
                        s_BlendBox.baseColor = baseColor;
                        s_BlendBox.monoHandle = !densityVolume.parameters.m_EditorAdvancedFade;
                        s_BlendBox.center = CenterBlendLocalPosition(densityVolume);
                        s_BlendBox.size = BlendSize(densityVolume);
                        EditorGUI.BeginChangeCheck();
                        s_BlendBox.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(densityVolume, "Change Density Volume Blend");

                            if (densityVolume.parameters.m_EditorAdvancedFade)
                            {
                                //work in local space to compute the change on positiveFade and negativeFade
                                Vector3 newCenterBlendLocalPosition = s_BlendBox.center;
                                Vector3 halfSize = s_BlendBox.size * 0.5f;
                                Vector3 size = densityVolume.parameters.size;
                                Vector3 posFade = newCenterBlendLocalPosition + halfSize;
                                posFade.x = 0.5f - posFade.x / size.x;
                                posFade.y = 0.5f - posFade.y / size.y;
                                posFade.z = 0.5f - posFade.z / size.z;
                                Vector3 negFade = newCenterBlendLocalPosition - halfSize;
                                negFade.x = 0.5f + negFade.x / size.x;
                                negFade.y = 0.5f + negFade.y / size.y;
                                negFade.z = 0.5f + negFade.z / size.z;
                                densityVolume.parameters.m_EditorPositiveFade = posFade;
                                densityVolume.parameters.m_EditorNegativeFade = negFade;
                            }
                            else
                            {
                                float uniformDistance = (s_ShapeBox.size.x - s_BlendBox.size.x) * 0.5f;
                                float max = Mathf.Min(s_ShapeBox.size.x, s_ShapeBox.size.y, s_ShapeBox.size.z) * 0.5f;
                                densityVolume.parameters.m_EditorUniformFade = Mathf.Clamp(uniformDistance, 0f, max);
                            }
                        }
                    }
                    break;
                case k_EditShape:
                    //important: if the origin of the handle's space move along the handle,
                    //handles displacement will appears as moving two time faster.
                    using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, densityVolume.transform.rotation, Vector3.one)))
                    {
                        //contained must be initialized in all case
                        s_ShapeBox.center = Quaternion.Inverse(densityVolume.transform.rotation) * densityVolume.transform.position;
                        s_ShapeBox.size = densityVolume.parameters.size;

                        Vector3 previousSize = densityVolume.parameters.size;
                        Vector3 previousPositiveFade = densityVolume.parameters.m_EditorPositiveFade;
                        Vector3 previousNegativeFade = densityVolume.parameters.m_EditorNegativeFade;

                        EditorGUI.BeginChangeCheck();
                        s_ShapeBox.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(new Object[] { densityVolume, densityVolume.transform }, "ChangeDensity Volume Bounding Box");

                            Vector3 newSize = s_ShapeBox.size;
                            densityVolume.parameters.size = newSize;

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
                            densityVolume.parameters.m_EditorPositiveFade = newPositiveFade;
                            densityVolume.parameters.m_EditorNegativeFade = newNegativeFade;

                            //update normal mode blend
                            float max = Mathf.Min(newSize.x, newSize.y, newSize.z) * 0.5f;
                            float newUniformFade = Mathf.Clamp(densityVolume.parameters.m_EditorUniformFade, 0f, max);
                            densityVolume.parameters.m_EditorUniformFade = newUniformFade;

                            //update engine used percents
                            if (densityVolume.parameters.m_EditorAdvancedFade)
                            {
                                densityVolume.parameters.positiveFade = newPositiveFade;
                                densityVolume.parameters.negativeFade = newNegativeFade;
                            }
                            else
                            {
                                densityVolume.parameters.positiveFade =
                                    densityVolume.parameters.negativeFade =
                                    new Vector3(
                                        newSize.x > 0.00001 ? (newSize.x - newUniformFade) / newSize.x : 0f,
                                        newSize.y > 0.00001 ? (newSize.y - newUniformFade) / newSize.y : 0f,
                                        newSize.z > 0.00001 ? (newSize.z - newUniformFade) / newSize.z : 0f
                                    );
                            }

                            Vector3 delta = densityVolume.transform.rotation * s_ShapeBox.center - densityVolume.transform.position;
                            densityVolume.transform.position += delta;
                        }
                    }
                    break;
            }
        }
    }
}
