using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [CustomEditor(typeof(VolumeProfile))]
    sealed class VolumeProfileEditor : Editor
    {
        VolumeComponentListEditor m_ComponentList;

        void OnEnable()
        {
            m_ComponentList = new VolumeComponentListEditor(this);
            m_ComponentList.Init(target as VolumeProfile, serializedObject);
        }

        void OnDisable()
        {
            if (m_ComponentList != null)
                m_ComponentList.Clear();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_ComponentList.OnGUI();

            EditorGUILayout.Space();
            if (m_ComponentList.hasHiddenVolumeComponents)
                EditorGUILayout.HelpBox("There are Volume Components that are hidden in this asset because they are incompatible with the current active Render Pipeline. Change the active Render Pipeline to see them.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
