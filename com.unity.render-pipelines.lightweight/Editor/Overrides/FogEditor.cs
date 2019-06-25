using UnityEngine;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering.LWRP
{
    [VolumeComponentEditor(typeof(Fog))]
    sealed class FogEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Type;

        SerializedDataParameter m_NearFog;
        SerializedDataParameter m_FarFog;
        
        SerializedDataParameter m_Density;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Fog>(serializedObject);

            m_Type = Unpack(o.Find(x => x.type));
            // Linear Fog Settings
            m_NearFog = Unpack(o.Find(x => x.nearFog));
            m_FarFog = Unpack(o.Find(x => x.farFog));
            // Exponential 2 Settings
            m_Density = Unpack(o.Find(x => x.density));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Type);

            if (m_Type.value.intValue == (int)FogType.Linear)
            {
                PropertyField(m_NearFog);
                m_NearFog.value.floatValue = Mathf.Max(m_NearFog.value.floatValue, 0);
                PropertyField(m_FarFog);
                m_FarFog.value.floatValue = Mathf.Max(m_FarFog.value.floatValue, 0);

            }
            else if (m_Type.value.intValue == (int)FogType.Exp2)
            {
                PropertyField(m_Density);
                m_Density.value.floatValue = Mathf.Max(m_Density.value.floatValue, 0);
            }
        }
    }
}
