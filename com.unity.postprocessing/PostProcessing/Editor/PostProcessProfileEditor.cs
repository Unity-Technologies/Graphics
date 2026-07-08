using UnityEngine.Rendering.PostProcessing;

#pragma warning disable CS0618 // Type or member is obsolete

namespace UnityEditor.Rendering.PostProcessing
{
    [CustomEditor(typeof(PostProcessProfile))]
    sealed class PostProcessProfileEditor : Editor
    {
        EffectListEditor m_EffectList;

        void OnEnable()
        {
            m_EffectList = new EffectListEditor(this);
            m_EffectList.Init(target as PostProcessProfile, serializedObject);
        }

        void OnDisable()
        {
            if (m_EffectList != null)
                m_EffectList.Clear();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_EffectList.OnGUI();
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
