using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class ScreenSpaceLightingEditor : VolumeComponentEditor
    {
        protected SerializedDataParameter m_RayLevel;
        protected SerializedDataParameter m_RayMaxLinearIterationsLevel;
        protected SerializedDataParameter m_RayMinLevel;
        protected SerializedDataParameter m_RayMaxLevel;
        protected SerializedDataParameter m_RayMaxIterations;
        protected SerializedDataParameter m_RayDepthSuccessBias;
        protected SerializedDataParameter m_ScreenWeightDistance;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceLighting>(serializedObject);

            m_RayLevel = Unpack(o.Find(x => x.rayLevel));
            m_RayMaxLinearIterationsLevel = Unpack(o.Find(x => x.rayMaxLinearIterationsLevel));
            m_RayMinLevel = Unpack(o.Find(x => x.rayMinLevel));
            m_RayMaxLevel = Unpack(o.Find(x => x.rayMaxLevel));
            m_RayMaxIterations = Unpack(o.Find(x => x.rayMaxIterations));
            m_RayDepthSuccessBias = Unpack(o.Find(x => x.rayDepthSuccessBias));
            m_ScreenWeightDistance = Unpack(o.Find(x => x.screenWeightDistance));
        }

        public override void OnInspectorGUI()
        {
            OnCommonInspectorGUI();
            EditorGUILayout.Separator();
            OnHiZInspectorGUI();
        }

        protected virtual void OnHiZInspectorGUI()
        {
            EditorGUILayout.LabelField(CoreEditorUtils.GetContent("HiZ Settings"));
            PropertyField(m_RayLevel, CoreEditorUtils.GetContent("Linear Ray Level"));
            PropertyField(m_RayMaxLinearIterationsLevel, CoreEditorUtils.GetContent("Linear Iterations"));
            PropertyField(m_RayMinLevel, CoreEditorUtils.GetContent("Ray Min Level"));
            PropertyField(m_RayMaxLevel, CoreEditorUtils.GetContent("Ray Max Level"));
            PropertyField(m_RayMaxIterations, CoreEditorUtils.GetContent("Ray Max Iterations"));
            PropertyField(m_RayDepthSuccessBias, CoreEditorUtils.GetContent("Ray Depth Success Bias"));
        }

        protected virtual void OnCommonInspectorGUI()
        {
            EditorGUILayout.LabelField(CoreEditorUtils.GetContent("Common Settings"));
            PropertyField(m_ScreenWeightDistance, CoreEditorUtils.GetContent("Screen Weight Distance"));
        }
    }
}