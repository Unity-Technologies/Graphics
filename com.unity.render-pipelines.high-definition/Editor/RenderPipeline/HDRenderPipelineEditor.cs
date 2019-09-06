using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    sealed class HDRenderPipelineEditor : Editor
    {
        SerializedHDRenderPipelineAsset m_SerializedHDRenderPipeline;

        internal bool largeLabelWidth = true;

        void OnEnable()
        {
            m_SerializedHDRenderPipeline = new SerializedHDRenderPipelineAsset(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            var serialized = m_SerializedHDRenderPipeline;

            serialized.Update();

            // In the quality window use more space for the labels
            if (!largeLabelWidth)
                EditorGUIUtility.labelWidth *= 2;
            HDRenderPipelineUI.Inspector.Draw(serialized, this);
            if (!largeLabelWidth)
                EditorGUIUtility.labelWidth *= 0.5f;

            serialized.Apply();
        }
    }
}
