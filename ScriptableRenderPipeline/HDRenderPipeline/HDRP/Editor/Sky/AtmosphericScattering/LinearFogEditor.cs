using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(LinearFog))]
    public class LinearFogEditor : AtmosphericScatteringEditor
    {
        private SerializedDataParameter m_FogStart;
        private SerializedDataParameter m_FogEnd;

        public override void OnEnable()
        {
            base.OnEnable();
            var o = new PropertyFetcher<LinearFog>(serializedObject);

            m_FogStart = Unpack(o.Find(x => x.fogStart));
            m_FogEnd = Unpack(o.Find(x => x.fogEnd));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            PropertyField(m_FogStart);
            PropertyField(m_FogEnd);
        }
    }
}