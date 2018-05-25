using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ScreenSpaceReflection))]
    public class HDScreenSpaceReflectionEditor : ScreenSpaceLightingEditor
    {
        SerializedDataParameter m_DeferredProjectionModel;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<ScreenSpaceReflection>(serializedObject);
            m_DeferredProjectionModel = Unpack(o.Find(x => x.deferredProjectionModel));
        }

        public override void OnInspectorGUI()
        {
            OnCommonInspectorGUI();
            var projectionModel = (Lit.ProjectionModel)m_DeferredProjectionModel.value.enumValueIndex;
            switch (projectionModel)
            {
                case Lit.ProjectionModel.HiZ:
                    EditorGUILayout.Separator();
                    OnHiZInspectorGUI();
                    break;
                case Lit.ProjectionModel.Proxy:
                    EditorGUILayout.Separator();
                    OnProxyInspectorGUI();
                    break;
            }
        }

        protected override void OnCommonInspectorGUI()
        {
            base.OnCommonInspectorGUI();
            PropertyField(m_DeferredProjectionModel, CoreEditorUtils.GetContent("Projection Model"));
        }
    }
}
