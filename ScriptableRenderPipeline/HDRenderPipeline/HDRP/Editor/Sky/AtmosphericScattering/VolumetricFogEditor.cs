using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(VolumetricFog))]
    public class VolumetricFogEditor : AtmosphericScatteringEditor
    {
        private SerializedDataParameter m_Albedo;
        private SerializedDataParameter m_MeanFreePath;
        private SerializedDataParameter m_Asymmetry;

        public override void OnEnable()
        {
            base.OnEnable();
            var o = new PropertyFetcher<VolumetricFog>(serializedObject);

            m_Albedo       = Unpack(o.Find(x => x.albedo));
            m_MeanFreePath = Unpack(o.Find(x => x.meanFreePath));
            m_Asymmetry    = Unpack(o.Find(x => x.asymmetry));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Albedo);
            PropertyField(m_MeanFreePath);
            PropertyField(m_Asymmetry);
        }
    }
}
