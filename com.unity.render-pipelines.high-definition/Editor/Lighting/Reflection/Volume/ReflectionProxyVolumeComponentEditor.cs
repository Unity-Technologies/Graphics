using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(ReflectionProxyVolumeComponent))]
    [CanEditMultipleObjects]
    class ReflectionProxyVolumeComponentEditor : Editor
    {
        static readonly Color k_HandleColor = new Color(0 / 255f, 0xE5 / 255f, 0xFF / 255f, 8f / 255).gamma;

        HierarchicalSphere m_SphereHandle;
        HierarchicalBox m_BoxHandle;
        SerializedReflectionProxyVolumeComponent m_SerializedData;
        ReflectionProxyVolumeComponentUI m_UIState = new ReflectionProxyVolumeComponentUI();
        ReflectionProxyVolumeComponent[] m_TypedTargets;

        void OnEnable()
        {
            m_SerializedData = new SerializedReflectionProxyVolumeComponent(serializedObject);
            System.Array.Resize(ref m_TypedTargets, serializedObject.targetObjects.Length);
            for (int i = 0; i < serializedObject.targetObjects.Length; ++i)
                m_TypedTargets[i] = (ReflectionProxyVolumeComponent)serializedObject.targetObjects[i];

            m_UIState = m_UIState ?? new ReflectionProxyVolumeComponentUI();

            m_SphereHandle = new HierarchicalSphere(k_HandleColor);
            m_BoxHandle = new HierarchicalBox(k_HandleColor)
            {
                monoHandle = false
            };
        }

        public override void OnInspectorGUI()
        {
            var s = m_UIState;
            var d = m_SerializedData;
            var o = this;

            d.Update();
            s.Update(d);

            ReflectionProxyVolumeComponentUI.Inspector.Draw(s, d, o);

            d.Apply();
        }

        void OnSceneGUI()
        {
            
            for (int i = 0; i < m_TypedTargets.Length; ++i)
            {
                var comp = m_TypedTargets[i];
                var tr = comp.transform;
                var prox = comp.proxyVolume;

                using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, tr.rotation, Vector3.one)))
                {
                    switch (prox.shape)
                    {
                        case ProxyShape.Box:
                            m_BoxHandle.center = Quaternion.Inverse(tr.rotation) * tr.position;
                            m_BoxHandle.size = prox.boxSize;
                            EditorGUI.BeginChangeCheck();
                            m_BoxHandle.DrawHull(true);
                            m_BoxHandle.DrawHandle();
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObjects(new Object[] { tr, comp }, "Update Proxy Volume Size");
                                tr.position = tr.rotation * m_BoxHandle.center;
                                prox.boxSize = m_BoxHandle.size;
                            }
                            break;
                        case ProxyShape.Sphere:
                            m_SphereHandle.center = Quaternion.Inverse(tr.rotation) * tr.position;
                            m_SphereHandle.radius = prox.sphereRadius;
                            EditorGUI.BeginChangeCheck();
                            m_SphereHandle.DrawHull(true);
                            m_SphereHandle.DrawHandle();
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObjects(new Object[] { tr, comp }, "Update Proxy Volume Size");
                                tr.position = tr.rotation * m_SphereHandle.center;
                                prox.sphereRadius = m_SphereHandle.radius;
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
