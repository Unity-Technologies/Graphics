using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    public sealed class HDRenderPipelineEditor : Editor
    {
        SerializedHDRenderPipelineAsset m_SerializedHDRenderPipeline;

        void OnEnable()
        {
            m_SerializedHDRenderPipeline = new SerializedHDRenderPipelineAsset(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            var serialized = m_SerializedHDRenderPipeline;
            
            serialized.Update();

            HDRenderPipelineUI.Inspector.Draw(serialized, this);

            serialized.Apply();
        }
    }
}
