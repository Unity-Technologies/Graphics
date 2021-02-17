using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(VisualEnvironment))]
    class VisualEnvironmentEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_SkyType;
        SerializedDataParameter m_CloudType;
        SerializedDataParameter m_SkyAmbientMode;

        static List<GUIContent> m_SkyClassNames = null;
        static List<int> m_SkyUniqueIDs = null;

        public static List<GUIContent> skyClassNames
        {
            get
            {
                UpdateSkyAndFogIntPopupData();
                return m_SkyClassNames;
            }
        }

        public static List<int> skyUniqueIDs
        {
            get
            {
                UpdateSkyAndFogIntPopupData();
                return m_SkyUniqueIDs;
            }
        }

        static List<GUIContent> m_CloudClassNames = null;
        static List<int> m_CloudUniqueIDs = null;

        public static List<GUIContent> cloudClassNames
        {
            get
            {
                UpdateSkyAndFogIntPopupData();
                return m_CloudClassNames;
            }
        }

        public static List<int> cloudUniqueIDs
        {
            get
            {
                UpdateSkyAndFogIntPopupData();
                return m_CloudUniqueIDs;
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            var o = new PropertyFetcher<VisualEnvironment>(serializedObject);

            m_SkyType = Unpack(o.Find(x => x.skyType));
            m_CloudType = Unpack(o.Find(x => x.cloudType));
            m_SkyAmbientMode = Unpack(o.Find(x => x.skyAmbientMode));
        }

        static void UpdateSkyAndFogIntPopupData()
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
                    string name = ObjectNames.NicifyVariableName(kvp.Value.Name.ToString());
                    name = name.Replace("Settings", ""); // remove Settings if it was in the class name
                    m_SkyClassNames.Add(new GUIContent(name));
                    m_SkyUniqueIDs.Add(kvp.Key);
                }
            }
            if (m_CloudClassNames == null)
            {
                m_CloudClassNames = new List<GUIContent>();
                m_CloudUniqueIDs = new List<int>();

                // Add special "None" case.
                m_CloudClassNames.Add(new GUIContent("None"));
                m_CloudUniqueIDs.Add(0);

                var typesDict = SkyManager.cloudTypesDict;

                foreach (KeyValuePair<int, Type> kvp in typesDict)
                {
                    string name = ObjectNames.NicifyVariableName(kvp.Value.Name.ToString());
                    name = name.Replace("Settings", ""); // remove Settings if it was in the class name
                    m_CloudClassNames.Add(new GUIContent(name));
                    m_CloudUniqueIDs.Add(kvp.Key);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            UpdateSkyAndFogIntPopupData();

            DrawHeader("Sky");
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawOverrideCheckbox(m_SkyType);
                using (new EditorGUI.DisabledScope(!m_SkyType.overrideState.boolValue))
                {
                    EditorGUILayout.IntPopup(m_SkyType.value, m_SkyClassNames.ToArray(), m_SkyUniqueIDs.ToArray(), EditorGUIUtility.TrTextContent("Sky type", "Specifies the type of sky this Volume uses."));
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawOverrideCheckbox(m_CloudType);
                using (new EditorGUI.DisabledScope(!m_CloudType.overrideState.boolValue))
                {
                    EditorGUILayout.IntPopup(m_CloudType.value, m_CloudClassNames.ToArray(), m_CloudUniqueIDs.ToArray(), EditorGUIUtility.TrTextContent("Background clouds", "Specifies the type of background cloud this Volume uses."));
                }
            }

            PropertyField(m_SkyAmbientMode, EditorGUIUtility.TrTextContent("Ambient Mode", "Specifies how the global ambient probe is computed. Dynamic will use the currently displayed sky and static will use the sky setup in the environment lighting panel."));

            var staticLightingSky = SkyManager.GetStaticLightingSky();
            if (m_SkyAmbientMode.value.GetEnumValue<SkyAmbientMode>() == SkyAmbientMode.Static)
            {
                if (staticLightingSky == null)
                    EditorGUILayout.HelpBox("Current Static Lighting Sky use None of profile None.", MessageType.Info);
                else
                {
                    var skyType = staticLightingSky.staticLightingSkyUniqueID == 0 ? "no Sky" : SkyManager.skyTypesDict[staticLightingSky.staticLightingSkyUniqueID].Name.ToString();
                    var cloudType = staticLightingSky.staticLightingCloudsUniqueID == 0 ? "no Clouds" : SkyManager.cloudTypesDict[staticLightingSky.staticLightingCloudsUniqueID].Name.ToString();
                    EditorGUILayout.HelpBox($"Current Static Lighting Sky uses {skyType} and {cloudType} of profile {staticLightingSky.profile?.name ?? "None"}.", MessageType.Info);
                }
            }
        }
    }
}
