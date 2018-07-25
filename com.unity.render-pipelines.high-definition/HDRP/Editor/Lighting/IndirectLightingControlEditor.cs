using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(IndirectLightingControl))]
    public class IndirectLightingControlEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_IndirectSpecularIntensity;
        SerializedDataParameter m_IndirectDiffuseIntensity;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<IndirectLightingControl>(serializedObject);

            m_IndirectDiffuseIntensity = Unpack(o.Find(x => x.indirectDiffuseIntensity));
            m_IndirectSpecularIntensity = Unpack(o.Find(x => x.indirectSpecularIntensity));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_IndirectSpecularIntensity, CoreEditorUtils.GetContent("Indirect Specular Intensity|Multiplier for the reflected specular light."));
            PropertyField(m_IndirectDiffuseIntensity, CoreEditorUtils.GetContent("Indirect Diffuse Intensity|Multiplier for the baked diffuse lighting."));
        }
    }
}
