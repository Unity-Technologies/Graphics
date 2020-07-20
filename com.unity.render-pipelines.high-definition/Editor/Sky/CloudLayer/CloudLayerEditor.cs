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
        struct CloudLightingParameter
        {
            public SerializedDataParameter mode;
            public SerializedDataParameter steps;
            public SerializedDataParameter thickness;
            public SerializedDataParameter castShadows;
        }

        struct CloudMapParameter
        {
            public SerializedDataParameter cloudMap;
            public SerializedDataParameter[] opacities;
            public SerializedDataParameter rotation;
            public SerializedDataParameter tint;
            public SerializedDataParameter intensityMultiplier;
            public CloudLightingParameter lighting;
        }

        CloudMapParameter UnpackCloudMap(SerializedProperty serializedProperty)
        {
            var p = new RelativePropertyFetcher<CloudLayer.CloudMap>(serializedProperty);
            var l = new RelativePropertyFetcher<CloudLayer.CloudLighting>(p.Find(x => x.lighting));

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
                intensityMultiplier = Unpack(p.Find(x => x.intensityMultiplier)),
                lighting = new CloudLightingParameter
                {
                    mode = Unpack(l.Find(x => x.lighting)),
                    steps = Unpack(l.Find(x => x.steps)),
                    thickness = Unpack(l.Find(x => x.thickness)),
                    castShadows = Unpack(l.Find(x => x.castShadows)),
                }
            };
        }

        void PropertyField(CloudMapParameter map, int index)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Map {(index == 0 ? 'A' : 'B')}", EditorStyles.miniLabel);

            PropertyField(map.cloudMap);
            EditorGUI.indentLevel++;
            for (int i = 0; i < 4; i++)
                PropertyField(map.opacities[i]);
            EditorGUI.indentLevel--;
            PropertyField(map.rotation);
            PropertyField(map.tint);
            PropertyField(map.intensityMultiplier);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Map {(index == 0 ? 'A' : 'B')} Lighting", EditorStyles.miniLabel);
            PropertyField(map.lighting.mode);
            if (map.lighting.mode.value.intValue == (int)CloudLightingMode.Raymarching)
            {
                EditorGUI.indentLevel++;
                PropertyField(map.lighting.steps);
                PropertyField(map.lighting.thickness);
                EditorGUI.indentLevel--;
            }
            PropertyField(map.lighting.castShadows);
        }


        SerializedDataParameter m_Opacity, m_UpperHemisphereOnly;
        SerializedDataParameter m_Mode, m_Layers;
        CloudMapParameter[] m_Maps;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<CloudLayer>(serializedObject);

            m_Opacity = Unpack(o.Find(x => x.opacity));
            m_UpperHemisphereOnly = Unpack(o.Find(x => x.upperHemisphereOnly));
            m_Mode = Unpack(o.Find(x => x.mode));
            m_Layers = Unpack(o.Find(x => x.layers));

            m_Maps = new CloudMapParameter[] {
                UnpackCloudMap(o.Find(x => x.mapA)),
                UnpackCloudMap(o.Find(x => x.mapB))
            };
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Opacity);
            PropertyField(m_UpperHemisphereOnly);

            PropertyField(m_Mode);
            if (m_Mode.value.intValue == (int)CloudLayerMode.CloudMap)
            {
                EditorGUI.indentLevel++;
                PropertyField(m_Layers);
                EditorGUI.indentLevel--;
                
                PropertyField(m_Maps[0], 0);
                if (m_Layers.value.intValue == (int)CloudMapMode.Double)
                    PropertyField(m_Maps[1], 1);
            }
        }
    }
}
