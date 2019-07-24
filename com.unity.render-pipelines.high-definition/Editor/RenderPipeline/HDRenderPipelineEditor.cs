using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    sealed class HDRenderPipelineEditor : Editor
    {
        SerializedHDRenderPipelineAsset m_SerializedHDRenderPipeline;

        internal bool showInspector = true;

        void OnEnable()
        {
            //showInspector = false;
            m_SerializedHDRenderPipeline = new SerializedHDRenderPipelineAsset(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            if (!showInspector)
                return;

            var serialized = m_SerializedHDRenderPipeline;

            serialized.Update();

            HDRenderPipelineUI.Inspector.Draw(serialized, this);

            serialized.Apply();
        }
    }
}
