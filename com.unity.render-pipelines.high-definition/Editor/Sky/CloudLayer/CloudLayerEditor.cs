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
        struct CloudSettingsParameter
        {
            public SerializedDataParameter rotation;
            public SerializedDataParameter tint;
            public SerializedDataParameter intensityMultiplier;
            public SerializedDataParameter distortion;
            public SerializedDataParameter scrollDirection;
            public SerializedDataParameter scrollSpeed;
            public SerializedDataParameter flowmap;
        }

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
            public CloudSettingsParameter settings;
            public CloudLightingParameter lighting;
        }

        struct CloudCRTParameter
        {
            public SerializedDataParameter cloudCRT;
            public CloudSettingsParameter settings;
            public CloudLightingParameter lighting;
        }

        CloudSettingsParameter UnpackCloudSettings(SerializedProperty serializedProperty)
        {
            var p = new RelativePropertyFetcher<CloudLayer.CloudSettings>(serializedProperty);

            return new CloudSettingsParameter
            {
                rotation = Unpack(p.Find(x => x.rotation)),
                tint = Unpack(p.Find(x => x.tint)),
                intensityMultiplier = Unpack(p.Find(x => x.intensityMultiplier)),
                distortion = Unpack(p.Find(x => x.distortion)),
                scrollDirection = Unpack(p.Find(x => x.scrollDirection)),
                scrollSpeed = Unpack(p.Find(x => x.scrollSpeed)),
                flowmap = Unpack(p.Find(x => x.flowmap)),
            };
        }

        CloudLightingParameter UnpackCloudLighting(SerializedProperty serializedProperty)
        {
            var p = new RelativePropertyFetcher<CloudLayer.CloudLighting>(serializedProperty);

            return new CloudLightingParameter
            {
                mode = Unpack(p.Find(x => x.lighting)),
                steps = Unpack(p.Find(x => x.steps)),
                thickness = Unpack(p.Find(x => x.thickness)),
                castShadows = Unpack(p.Find(x => x.castShadows)),
            };
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
                settings = UnpackCloudSettings(p.Find(x => x.settings)),
                lighting = UnpackCloudLighting(p.Find(x => x.lighting)),
            };
        }

        CloudCRTParameter UnpackCloudCRT(SerializedProperty serializedProperty)
        {
            var p = new RelativePropertyFetcher<CloudLayer.CloudCRT>(serializedProperty);

            return new CloudCRTParameter
            {
                cloudCRT = Unpack(p.Find(x => x.cloudCRT)),
                settings = UnpackCloudSettings(p.Find(x => x.settings)),
                lighting = UnpackCloudLighting(p.Find(x => x.lighting))
            };
        }

        SerializedDataParameter m_Opacity, m_UpperHemisphereOnly;
        SerializedDataParameter m_Mode, m_LayerCount;
        SerializedDataParameter m_ShadowsOpacity, m_ShadowsTiling;
        CloudMapParameter[] m_Layers;
        CloudCRTParameter m_Crt;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<CloudLayer>(serializedObject);

            m_Opacity = Unpack(o.Find(x => x.opacity));
            m_UpperHemisphereOnly = Unpack(o.Find(x => x.upperHemisphereOnly));
            m_Mode = Unpack(o.Find(x => x.mode));
            m_LayerCount = Unpack(o.Find(x => x.layers));

            m_ShadowsOpacity = Unpack(o.Find(x => x.shadowsOpacity));
            m_ShadowsTiling = Unpack(o.Find(x => x.shadowsTiling));

            m_Layers = new CloudMapParameter[] {
                UnpackCloudMap(o.Find(x => x.layerA)),
                UnpackCloudMap(o.Find(x => x.layerB))
            };

            m_Crt = UnpackCloudCRT(o.Find(x => x.crt));
        }



        void PropertyField(CloudSettingsParameter settings)
        {
            PropertyField(settings.rotation);
            PropertyField(settings.tint);
            PropertyField(settings.intensityMultiplier);

            PropertyField(settings.distortion);
            if (settings.distortion.value.intValue != (int)CloudDistortionMode.None)
            {
                EditorGUI.indentLevel++;
                PropertyField(settings.scrollDirection);
                PropertyField(settings.scrollSpeed);
                if (settings.distortion.value.intValue == (int)CloudDistortionMode.Flowmap)
                {
                    PropertyField(settings.flowmap);
                }
                EditorGUI.indentLevel--;
            }
        }

        void PropertyField(CloudLightingParameter lighting, string label)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);

            PropertyField(lighting.mode);
            if (lighting.mode.value.intValue == (int)CloudLightingMode.Raymarching)
            {
                EditorGUI.indentLevel++;
                PropertyField(lighting.steps);
                PropertyField(lighting.thickness);
                EditorGUI.indentLevel--;
            }
            PropertyField(lighting.castShadows);
        }

        void PropertyField(CloudMapParameter map, string label)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);

            PropertyField(map.cloudMap);
            EditorGUI.indentLevel++;
            for (int i = 0; i < 4; i++)
                PropertyField(map.opacities[i]);
            EditorGUI.indentLevel--;

            PropertyField(map.settings);
            PropertyField(map.lighting, label + " Lighting");
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Opacity);
            PropertyField(m_UpperHemisphereOnly);

            PropertyField(m_Mode);
            if (m_Mode.value.intValue == (int)CloudLayerMode.CloudMap)
            {
                EditorGUI.indentLevel++;
                PropertyField(m_LayerCount);
                EditorGUI.indentLevel--;

                PropertyField(m_Layers[0], "Layer A");
                bool cloudShadows = m_Layers[0].lighting.castShadows.value.boolValue;

                if (m_LayerCount.value.intValue == (int)CloudMapMode.Double)
                {
                    PropertyField(m_Layers[1], "Layer B");
                    cloudShadows |= m_Layers[1].lighting.castShadows.value.boolValue;
                }

                if (cloudShadows)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Cloud Shadows", EditorStyles.miniLabel);

                    PropertyField(m_ShadowsOpacity);
                    PropertyField(m_ShadowsTiling);
                }
            }
            else if (m_Mode.value.intValue == (int)CloudLayerMode.RenderTexture)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Custom Render Texture", EditorStyles.miniLabel);

                PropertyField(m_Crt.cloudCRT);
                PropertyField(m_Crt.settings);
                PropertyField(m_Crt.lighting, "Lighting");
            }
        }
    }
}
