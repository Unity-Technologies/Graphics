using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(ExponentialFog))]
    class ExponentialFogEditor : AtmosphericScatteringEditor
    {
        SerializedDataParameter m_Density;
        SerializedDataParameter m_FogDistance;
        SerializedDataParameter m_FogBaseHeight;
        SerializedDataParameter m_FogHeightAttenuation;

        public override void OnEnable()
        {
            base.OnEnable();
            var o = new PropertyFetcher<ExponentialFog>(serializedObject);

            m_Density = Unpack(o.Find(x => x.density));
            m_FogDistance = Unpack(o.Find(x => x.fogDistance));
            m_FogBaseHeight = Unpack(o.Find(x => x.fogBaseHeight));
            m_FogHeightAttenuation = Unpack(o.Find(x => x.fogHeightAttenuation));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Density);
            PropertyField(m_FogDistance);
            PropertyField(m_FogBaseHeight);
            PropertyField(m_FogHeightAttenuation);
            PropertyField(m_MaxFogDistance);
            base.OnInspectorGUI(); // Color
        }
    }
}
