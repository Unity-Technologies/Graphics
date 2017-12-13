using UnityEditor.Experimental.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor
{
    [CustomEditorForRenderPipeline(typeof(ReflectionProbe), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    partial class HDReflectionProbeEditor : Editor
    {
        SerializedReflectionProbe m_SerializedReflectionProbe;
        SerializedObject m_AdditionalDataSerializedObject;
        UIState m_UIState = new UIState();

        Matrix4x4 m_OldLocalSpace = Matrix4x4.identity;

        void OnEnable()
        {
            var additionalData = CoreEditorUtils.GetAdditionalData<HDAdditionalReflectionData>(targets);
            m_AdditionalDataSerializedObject = new SerializedObject(additionalData);
            m_SerializedReflectionProbe = new SerializedReflectionProbe(serializedObject, m_AdditionalDataSerializedObject);
            m_UIState.Reset(this, Repaint, m_SerializedReflectionProbe.mode.intValue);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_AdditionalDataSerializedObject.Update();

            var s = m_UIState;
            var p = m_SerializedReflectionProbe;

            Drawer_ReflectionProbeMode(s, p, this);
            Drawer_ModeSettings(s, p, this);
            EditorGUILayout.Space();
            Drawer_InfluenceShape(s, p, this);
            Drawer_IntensityMultiplier(s, p, this);

            Drawer_Toolbar(s, p, this);

            if (s.shouldUpdateOldLocalSpace)
            {
                s.shouldUpdateOldLocalSpace = false;
                UpdateOldLocalSpace();
            }

            m_AdditionalDataSerializedObject.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();
        }



        void UpdateOldLocalSpace()
        {
            m_OldLocalSpace = GetLocalSpace((ReflectionProbe)target);
        }

        static Matrix4x4 GetLocalSpace(ReflectionProbe probe)
        {
            Vector3 t = probe.transform.position;
            return Matrix4x4.TRS(t, GetLocalSpaceRotation(probe), Vector3.one);
        }

        static Quaternion GetLocalSpaceRotation(ReflectionProbe probe)
        {
            bool supportsRotation = (SupportedRenderingFeatures.active.reflectionProbeSupportFlags & SupportedRenderingFeatures.ReflectionProbeSupportFlags.Rotation) != 0;
            if (supportsRotation)
                return probe.transform.rotation;
            else
                return Quaternion.identity;
        }
    }
}
