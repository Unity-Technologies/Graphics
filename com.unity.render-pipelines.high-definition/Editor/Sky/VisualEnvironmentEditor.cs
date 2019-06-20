using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(VisualEnvironment))]
    public class VisualEnvironmentEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_SkyType;
        SerializedDataParameter m_SkyAmbientMode;
        SerializedDataParameter m_FogType;

        List<GUIContent> m_SkyClassNames = null;
        List<GUIContent> m_FogNames = null;
        List<int> m_SkyUniqueIDs = null;

        public static readonly string[] fogNames = Enum.GetNames(typeof(FogType));
        public static readonly int[] fogValues = Enum.GetValues(typeof(FogType)) as int[];

        public override void OnEnable()
        {
            base.OnEnable();
            var o = new PropertyFetcher<VisualEnvironment>(serializedObject);

            m_SkyType = Unpack(o.Find(x => x.skyType));
            m_SkyAmbientMode = Unpack(o.Find(x => x.skyAmbientMode));
            m_FogType = Unpack(o.Find(x => x.fogType));
        }

        void UpdateSkyAndFogIntPopupData()
        {
            if (m_SkyClassNames == null)
            {
                m_SkyClassNames = new List<GUIContent>();
                m_SkyUniqueIDs = new List<int>();

                // Add special "None" case.
                m_SkyClassNames.Add(new GUIContent("None"));
                m_SkyUniqueIDs.Add(0);

                var skyTypesDict = SkyManager.skyTypesDict;

                foreach (KeyValuePair<int, Type> kvp in skyTypesDict)
                {
                    m_SkyClassNames.Add(new GUIContent(ObjectNames.NicifyVariableName(kvp.Value.Name.ToString())));
                    m_SkyUniqueIDs.Add(kvp.Key);
                }
            }

            if (m_FogNames == null)
            {
                m_FogNames = new List<GUIContent>();

                foreach (string fogStr in fogNames)
                {
                    // Add Fog on each members of the enum except for None
                    m_FogNames.Add(new GUIContent(fogStr + (fogStr != "None" ? " Fog" : "")));
                }
            }
        }

        public override void OnInspectorGUI()
        {
            UpdateSkyAndFogIntPopupData();

            EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent("Sky"), EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {

                DrawOverrideCheckbox(m_SkyType);
                using (new EditorGUI.DisabledScope(!m_SkyType.overrideState.boolValue))
                {
                    EditorGUILayout.IntPopup(m_SkyType.value, m_SkyClassNames.ToArray(), m_SkyUniqueIDs.ToArray(), EditorGUIUtility.TrTextContent("Type", "Specifies the type of sky this Volume uses."));

                }
            }
            PropertyField(m_SkyAmbientMode, EditorGUIUtility.TrTextContent("Ambient Mode"));

            if ( ((SkyAmbientMode)m_SkyAmbientMode.value.enumValueIndex == SkyAmbientMode.Static) && SkyManager.GetStaticLightingSky() == null)
            {
                EditorGUILayout.HelpBox("A Static Lighting Sky Component is required for Static Ambient Mode.", MessageType.Info);
            }

            EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent("Fog"), EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawOverrideCheckbox(m_FogType);
                using (new EditorGUI.DisabledScope(!m_FogType.overrideState.boolValue))
                {
                    EditorGUILayout.IntPopup(m_FogType.value, m_FogNames.ToArray(), fogValues, EditorGUIUtility.TrTextContent("Type", "Specifies the type of fog this Volume uses."));
                }
            }
        }
    }
}
