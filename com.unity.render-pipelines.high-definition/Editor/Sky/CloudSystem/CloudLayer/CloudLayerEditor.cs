using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(CloudLayer))]
    class CloudLayerEditor : VolumeComponentEditor
    {
        struct CloudMapParameter
        {
            public SerializedDataParameter cloudMap;
            public SerializedDataParameter[] opacities;

            public SerializedDataParameter rotation;
            public SerializedDataParameter tint;
            public SerializedDataParameter exposure;

            public SerializedDataParameter distortion;
            public SerializedDataParameter scrollDirection;
            public SerializedDataParameter scrollSpeed;
            public SerializedDataParameter flowmap;

            public SerializedDataParameter lighting;
            public SerializedDataParameter steps;
            public SerializedDataParameter thickness;

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

                rotation = Unpack(p.Find(x => x.rotation)),
                tint = Unpack(p.Find(x => x.tint)),
                exposure = Unpack(p.Find(x => x.exposure)),
                distortion = Unpack(p.Find(x => x.distortionMode)),
                scrollDirection = Unpack(p.Find(x => x.scrollDirection)),
                scrollSpeed = Unpack(p.Find(x => x.scrollSpeed)),
                flowmap = Unpack(p.Find(x => x.flowmap)),

                lighting = Unpack(p.Find(x => x.lighting)),
                steps = Unpack(p.Find(x => x.steps)),
                thickness = Unpack(p.Find(x => x.thickness)),
                castShadows = Unpack(p.Find(x => x.castShadows)),
            };
        }

        SerializedDataParameter m_Opacity, m_UpperHemisphereOnly, m_LayerCount;
        SerializedDataParameter m_ShadowsOpacity, m_ShadowsTiling;
        CloudMapParameter[] m_Layers;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<CloudLayer>(serializedObject);

            m_Opacity = Unpack(o.Find(x => x.opacity));
            m_UpperHemisphereOnly = Unpack(o.Find(x => x.upperHemisphereOnly));
            m_LayerCount = Unpack(o.Find(x => x.layers));

            m_ShadowsOpacity = Unpack(o.Find(x => x.shadowsOpacity));
            m_ShadowsTiling = Unpack(o.Find(x => x.shadowsTiling));

            m_Layers = new CloudMapParameter[]
            {
                UnpackCloudMap(o.Find(x => x.layerA)),
                UnpackCloudMap(o.Find(x => x.layerB))
            };
        }

        void PropertyField(CloudMapParameter map, string label)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);

            PropertyField(map.cloudMap);
            using (new HDEditorUtils.IndentScope())
            {
                for (int i = 0; i < 4; i++)
                    PropertyField(map.opacities[i]);
            }

            PropertyField(map.rotation);
            PropertyField(map.tint);
            PropertyField(map.exposure);

            PropertyField(map.distortion);
            using (new HDEditorUtils.IndentScope())
            {
                PropertyField(map.scrollDirection);
                PropertyField(map.scrollSpeed);
                if (map.distortion.value.intValue == (int)CloudDistortionMode.Flowmap)
                {
                    PropertyField(map.flowmap);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(label + " Lighting", EditorStyles.miniLabel);

            PropertyField(map.lighting);
            using (new HDEditorUtils.IndentScope())
            {
                PropertyField(map.steps);
                PropertyField(map.thickness);
            }
            PropertyField(map.castShadows);
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Opacity);
            PropertyField(m_UpperHemisphereOnly);
            PropertyField(m_LayerCount);

            PropertyField(m_Layers[0], "Layer A");

            if (m_LayerCount.value.intValue == (int)CloudMapMode.Double)
                PropertyField(m_Layers[1], "Layer B");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Cloud Shadows", EditorStyles.miniLabel);

            PropertyField(m_ShadowsOpacity);
            PropertyField(m_ShadowsTiling);
        }
    }
}
