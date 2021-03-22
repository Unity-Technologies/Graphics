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
        readonly GUIContent sunLabel        = new GUIContent("Sun light", "The main directional light, used for lighting and shadow casting.");
        readonly GUIContent shadowTiling    = new GUIContent("Shadow Tiling", "The tiling of the cloud shadows texture. Controlled by the cookie size parameter on the sun light.");
        readonly GUIContent scrollLabel     = new GUIContent("Scroll Orientation", "Sets the orientation of the distortion (in degrees).");

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
        SerializedDataParameter m_Resolution, m_ShadowResolution;
        SerializedDataParameter m_ShadowMultiplier, m_ShadowTint;
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
                PropertyField(map.scrollDirection, scrollLabel);
                PropertyField(map.scrollSpeed);
                if (map.distortion.value.intValue == (int)CloudDistortionMode.Flowmap)
                {
                    PropertyField(map.flowmap);
                }
            }

            PropertyField(map.lighting);
            using (new HDEditorUtils.IndentScope())
            {
                PropertyField(map.steps);
                PropertyField(map.thickness);
            }
            PropertyField(map.castShadows);
        }

        bool CastShadows => m_Layers[0].castShadows.value.boolValue || (m_LayerCount.value.intValue == (int)CloudMapMode.Double && m_Layers[1].castShadows.value.boolValue);

        public override void OnInspectorGUI()
        {
            bool prevShadows = CastShadows;

            PropertyField(m_Opacity);
            if (showAdditionalProperties)
                PropertyField(m_UpperHemisphereOnly);
            PropertyField(m_LayerCount);
            if (showAdditionalProperties)
                PropertyField(m_Resolution);

            PropertyField(m_Layers[0], "Layer A");
            if (m_LayerCount.value.intValue == (int)CloudMapMode.Double)
                PropertyField(m_Layers[1], "Layer B");

            Light sun = HDRenderPipeline.currentPipeline?.GetCurrentSunLight();
            if (sun != null && sun.TryGetComponent(out HDAdditionalLightData hdSun))
            {
                PropertyField(m_ShadowMultiplier);
                PropertyField(m_ShadowTint);
                if (showAdditionalProperties)
                    PropertyField(m_ShadowResolution);

                bool shadows = CastShadows;
                if (prevShadows && !shadows)
                    sun.cookie = null;
                else if (shadows && sun.cookie == null)
                {
                    Undo.RecordObject(hdSun, "Change cookie size");
                    hdSun.shapeHeight = 500;
                    hdSun.shapeWidth = 500;
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20);
                        EditorGUILayout.ObjectField(sunLabel, sun, typeof(Light), true);
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20);
                        var size = new Vector2(hdSun.shapeWidth, hdSun.shapeHeight);
                        EditorGUILayout.Vector2Field(shadowTiling, size);
                    }
                }
            }
        }
    }
}
