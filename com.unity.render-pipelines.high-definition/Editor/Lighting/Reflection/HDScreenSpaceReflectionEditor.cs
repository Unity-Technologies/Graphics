using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ScreenSpaceReflection))]
    public class HDScreenSpaceReflectionEditor : ScreenSpaceLightingEditor
    {
        SerializedDataParameter m_MinSmoothness;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<ScreenSpaceReflection>(serializedObject);
            m_MinSmoothness = Unpack(o.Find(x => x.minSmoothness));
        }

        public override void OnInspectorGUI()
        {
            OnCommonInspectorGUI();

            OnHiZInspectorGUI();
            PropertyField(m_MinSmoothness, CoreEditorUtils.GetContent("Min Smoothness|Minimal value of smoothness at which SSR is activated."));
        }
    }
}
