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

        SerializedDataParameter m_Height;
        SerializedDataParameter m_HeightFalloff;
        SerializedDataParameter m_DistanceOffset;
        SerializedDataParameter m_DistanceFalloff;

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

            // Height Fog Settings
            m_Height = Unpack(o.Find(x => x.height));
            m_HeightFalloff = Unpack(o.Find(x => x.heightFalloff));
            m_DistanceOffset = Unpack(o.Find(x => x.distanceOffset));
            m_DistanceFalloff = Unpack(o.Find(x => x.distanceFalloff));
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
                PropertyField(m_Height);
                PropertyField(m_HeightFalloff);
                m_HeightFalloff.value.floatValue = Mathf.Max(m_HeightFalloff.value.floatValue, 0);
                PropertyField(m_DistanceOffset);
                PropertyField(m_DistanceFalloff);
                m_DistanceFalloff.value.floatValue = Mathf.Max(m_DistanceFalloff.value.floatValue, 0);
            }

            PropertyField(m_ColorType);

            if (m_ColorType.value.intValue == (int) FogColorType.Color)
            {
                PropertyField(m_Color);
            }
            else if (m_ColorType.value.intValue == (int) FogColorType.Gradient)
            {
                // Do the things
            }
            else if (m_ColorType.value.intValue == (int) FogColorType.CubeMap)
            {
                PropertyField(m_Cubemap);
            }
        }
    }
}
