using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CloudLayer))]
    class CloudLayerEditor : VolumeComponentEditor
    {
        struct CloudMapParameter
        {
            public SerializedDataParameter cloudMap;
            public SerializedDataParameter[] opacities;

            public SerializedDataParameter altitude;
            public SerializedDataParameter rotation;
            public SerializedDataParameter tint;
            public SerializedDataParameter exposure;

            public SerializedDataParameter distortion;
            public SerializedDataParameter scrollOrientation;
            public SerializedDataParameter scrollSpeed;
            public SerializedDataParameter flowmap;

            public SerializedDataParameter raymarching;
            public SerializedDataParameter steps;
            public SerializedDataParameter thickness;
            public SerializedDataParameter probeDimmer;

            public SerializedDataParameter castShadows;
        }

        CloudMapParameter UnpackCloudMap(SerializedProperty serializedProperty)
        {
            var p = new RelativePropertyFetcher<CloudLayer.CloudMap>(serializedProperty);

            return new CloudMapParameter
            {
                cloudMap = Unpack(p.Find(x => x.cloudMap)),
                opacities = new SerializedDataParameter[]
                {
                    Unpack(p.Find(x => x.opacityR)),
                    Unpack(p.Find(x => x.opacityG)),
                    Unpack(p.Find(x => x.opacityB)),
                    Unpack(p.Find(x => x.opacityA))
                },

                altitude = Unpack(p.Find(x => x.altitude)),
                rotation = Unpack(p.Find(x => x.rotation)),
                tint = Unpack(p.Find(x => x.tint)),
                exposure = Unpack(p.Find(x => x.exposure)),

                distortion = Unpack(p.Find(x => x.distortionMode)),
                scrollOrientation = Unpack(p.Find(x => x.scrollOrientation)),
                scrollSpeed = Unpack(p.Find(x => x.scrollSpeed)),
                flowmap = Unpack(p.Find(x => x.flowmap)),

                raymarching = Unpack(p.Find(x => x.lighting)),
                steps = Unpack(p.Find(x => x.steps)),
                thickness = Unpack(p.Find(x => x.thickness)),
                probeDimmer = Unpack(p.Find(x => x.ambientProbeDimmer)),
                castShadows = Unpack(p.Find(x => x.castShadows)),
            };
        }

        SerializedDataParameter m_Opacity, m_UpperHemisphereOnly, m_LayerCount;
        SerializedDataParameter m_Resolution, m_ShadowResolution;
        SerializedDataParameter m_ShadowMultiplier, m_ShadowTint, m_ShadowSize;
        CloudMapParameter[] m_Layers;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<CloudLayer>(serializedObject);

            m_Opacity = Unpack(o.Find(x => x.opacity));
            m_UpperHemisphereOnly = Unpack(o.Find(x => x.upperHemisphereOnly));
            m_LayerCount = Unpack(o.Find(x => x.layers));
            m_Resolution = Unpack(o.Find(x => x.resolution));

            m_ShadowMultiplier = Unpack(o.Find(x => x.shadowMultiplier));
            m_ShadowTint = Unpack(o.Find(x => x.shadowTint));
            m_ShadowResolution = Unpack(o.Find(x => x.shadowResolution));
            m_ShadowSize = Unpack(o.Find(x => x.shadowSize));

            m_Layers = new CloudMapParameter[]
            {
                UnpackCloudMap(o.Find(x => x.layerA)),
                UnpackCloudMap(o.Find(x => x.layerB))
            };
        }

        void PropertyField(CloudMapParameter map, string label)
        {
            DrawHeader(label);

            PropertyField(map.cloudMap);
            using (new IndentLevelScope())
            {
                for (int i = 0; i < 4; i++)
                    PropertyField(map.opacities[i]);
            }

            PropertyField(map.altitude);
            PropertyField(map.rotation);
            PropertyField(map.tint);
            PropertyField(map.exposure);

            PropertyField(map.distortion);
            if (map.distortion.value.intValue != (int)CloudDistortionMode.None)
            {
                using (new IndentLevelScope())
                {
                    PropertyField(map.scrollOrientation);
                    PropertyField(map.scrollSpeed);
                    if (map.distortion.value.intValue == (int)CloudDistortionMode.Flowmap)
                    {
                        PropertyField(map.flowmap);
                    }

                }
            }

            PropertyField(map.raymarching);
            using (new IndentLevelScope())
            {
                PropertyField(map.steps);
                PropertyField(map.thickness);
                PropertyField(map.probeDimmer);
            }
            PropertyField(map.castShadows);
        }

        bool CastShadows => m_Layers[0].castShadows.value.boolValue || (m_LayerCount.value.intValue == (int)CloudMapMode.Double && m_Layers[1].castShadows.value.boolValue);

        public override void OnInspectorGUI()
        {
            PropertyField(m_Opacity);
            if (showAdditionalProperties)
                PropertyField(m_UpperHemisphereOnly);
            PropertyField(m_LayerCount);
            if (showAdditionalProperties)
                PropertyField(m_Resolution);

            PropertyField(m_Layers[0], "Layer A");
            if (m_LayerCount.value.intValue == (int)CloudMapMode.Double)
                PropertyField(m_Layers[1], "Layer B");

            PropertyField(m_ShadowMultiplier);
            PropertyField(m_ShadowTint);
            if (showAdditionalProperties)
                PropertyField(m_ShadowResolution);

            PropertyField(m_ShadowSize);
        }
    }
}
