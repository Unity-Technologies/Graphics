using UnityEngine;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering.LWRP
{
    [VolumeComponentEditor(typeof(Fog))]
    sealed class FogEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Type;
        // Linear Fog settings
        SerializedDataParameter m_NearFog;
        SerializedDataParameter m_FarFog;
        // Exponential Fog settings
        SerializedDataParameter m_Density;

        // Color settings
        SerializedDataParameter m_ColorType;
        SerializedDataParameter m_Color;
        SerializedDataParameter m_Cubemap;
        SerializedDataParameter m_Rotation;

        SerializedDataParameter m_HeightOffset;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Fog>(serializedObject);

            m_Type = Unpack(o.Find(x => x.type));
            // Linear Fog Settings
            m_NearFog = Unpack(o.Find(x => x.nearFog));
            m_FarFog = Unpack(o.Find(x => x.farFog));
            // Exponential 2 Settings
            m_Density = Unpack(o.Find(x => x.density));

            // Color Settings
            m_ColorType = Unpack(o.Find(x => x.colorType));
            m_Color = Unpack(o.Find(x => x.fogColor));
            m_Cubemap = Unpack(o.Find(x => x.cubemap));
            m_Rotation = Unpack(o.Find(x => x.rotation));

            // Height Fog Settings
            m_HeightOffset = Unpack(o.Find(x => x.heightOffset));
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
            else if (m_Type.value.intValue == (int)FogType.Height)
            {
                PropertyField(m_Density);
                m_Density.value.floatValue = Mathf.Max(m_Density.value.floatValue, 0);
                PropertyField(m_HeightOffset);
            }

            if (m_Type.value.intValue != (int) FogType.Off)
            {
                PropertyField(m_ColorType);

                if (m_ColorType.value.intValue == (int) FogColorType.Color)
                {
                    PropertyField(m_Color);
                }
                /*else if (m_ColorType.value.intValue == (int) FogColorType.Gradient)
                {
                    // Do the things
                }*/
                else if (m_ColorType.value.intValue == (int) FogColorType.CubeMap)
                {
                    PropertyField(m_Cubemap);
                    PropertyField(m_Rotation);
                }
            }
        }
    }
}
