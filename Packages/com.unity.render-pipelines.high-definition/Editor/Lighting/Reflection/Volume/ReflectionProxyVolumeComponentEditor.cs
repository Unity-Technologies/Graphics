using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(ReflectionProxyVolumeComponent))]
    [CanEditMultipleObjects]
    class ReflectionProxyVolumeComponentEditor : Editor
    {
        static readonly Color k_HandleColor = new Color(0 / 255f, 0xE5 / 255f, 0xFF / 255f, 1f).gamma;

        SerializedReflectionProxyVolumeComponent m_SerializedData;
        static List<ReflectionProxyVolumeComponent> s_TypedTargets = new List<ReflectionProxyVolumeComponent>();

        private static Lazy<HierarchicalSphere> s_SphereHandle = new Lazy<HierarchicalSphere>(() =>
        {
            var value = new HierarchicalSphere(k_HandleColor);
            return value;
        });

        private static Lazy<HierarchicalBox> s_BoxHandle = new Lazy<HierarchicalBox>(() =>
        {
            var value = new HierarchicalBox(k_HandleColor, new[] { k_HandleColor, k_HandleColor, k_HandleColor, k_HandleColor, k_HandleColor, k_HandleColor })
            {
                monoHandle = false
            };
            return value;
        });

        private void OnEnable()
        {
            m_SerializedData = new SerializedReflectionProxyVolumeComponent(serializedObject);
            s_TypedTargets.Capacity = serializedObject.targetObjects.Length;
            foreach (var targetObject in serializedObject.targetObjects)
                s_TypedTargets.Add((ReflectionProxyVolumeComponent)targetObject);
        }

        private void OnDisable()
        {
            s_TypedTargets.Clear();
        }

        public override void OnInspectorGUI()
        {
            m_SerializedData.Update();

            ProxyVolumeUI.SectionShape.Draw(m_SerializedData.proxyVolume, this);

            m_SerializedData.Apply();
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(ReflectionProxyVolumeComponent proxyVolumeComponent, GizmoType gizmoType)
        {
            foreach (var comp in s_TypedTargets)
            {
                var tr = comp.transform;
                var prox = comp.proxyVolume;

                var matrix = Matrix4x4.TRS(Vector3.zero, tr.rotation, Vector3.one);
                if (matrix.ValidTRS())
                {
                    using (new Handles.DrawingScope(matrix))
                    {
                        switch (prox.shape)
                        {
                            case ProxyShape.Box:
                                s_BoxHandle.Value.center = Quaternion.Inverse(tr.rotation) * tr.position;
                                s_BoxHandle.Value.size = prox.boxSize;
                                EditorGUI.BeginChangeCheck();
                                s_BoxHandle.Value.DrawHull(true);
                                s_BoxHandle.Value.DrawHandle();
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObjects(new Object[] {tr, comp}, "Update Proxy Volume Size");
                                    tr.position = tr.rotation * s_BoxHandle.Value.center;
                                    prox.boxSize = s_BoxHandle.Value.size;
                                }

                                break;
                            case ProxyShape.Sphere:
                                s_SphereHandle.Value.center = Quaternion.Inverse(tr.rotation) * tr.position;
                                s_SphereHandle.Value.radius = prox.sphereRadius;
                                EditorGUI.BeginChangeCheck();
                                s_SphereHandle.Value.DrawHull(true);
                                s_SphereHandle.Value.DrawHandle();
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObjects(new Object[] {tr, comp}, "Update Proxy Volume Size");
                                    tr.position = tr.rotation * s_SphereHandle.Value.center;
                                    prox.sphereRadius = s_SphereHandle.Value.radius;
                                }

                                break;
                            case ProxyShape.Infinite:
                                break;
                        }
                    }
                }
            }
        }
    }
}
