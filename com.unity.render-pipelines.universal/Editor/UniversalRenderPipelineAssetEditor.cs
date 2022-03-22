using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Styles = UnityEditor.Rendering.Universal.UniversalRenderPipelineAssetUI.Styles;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(UniversalRenderPipelineAsset)), CanEditMultipleObjects]
    public class UniversalRenderPipelineAssetEditor : Editor
    {
        private SerializedUniversalRenderPipelineAsset m_SerializedURPAsset;

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            m_SerializedURPAsset.Update();
            UniversalRenderPipelineAssetUI.Inspector.Draw(m_SerializedURPAsset, this);
            m_SerializedURPAsset.Apply();
        }

        void OnEnable()
        {
            m_SerializedURPAsset = new SerializedUniversalRenderPipelineAsset(serializedObject);
        }
    }
}
